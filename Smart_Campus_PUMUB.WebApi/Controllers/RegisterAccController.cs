using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Net;
using System.Net.Mail;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCode(200)]
public class RegisterAccController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly IConfiguration _config;

    public RegisterAccController(SmartCampusDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/registeracc/test-email — SMTP Diagnosis
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("test-email")]
    public async Task<IActionResult> TestEmail([FromQuery] string to)
    {
        try
        {
            string html = "<h1>Test Email</h1><p>Smart Campus PUMUB Brevo Test is working!</p>";
            string text = "Test Email - Smart Campus PUMUB Brevo Test is working!";
            await SendEmailAsync(to, "Test Email from Smart Campus", html, text);
            return Ok(new { success = true, message = "Test email sent successfully! Please check your Inbox and Spam folders." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message, details = ex.ToString() });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/registeracc — Student submits registration request
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] RegisterAccCreateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new RegisterAccActionResponse { IsSuccess = false, Message = "ဖြည့်စွက်ထားသော အချက်အလက်တွေ မကြည့်ရပါ။" });

        // Check duplicate FormNo or ExamSeatNo
        bool duplicate = await _db.RegisterAccounts.AnyAsync(r =>
            r.FormNo == request.FormNo && r.ExamSeatNo == request.ExamSeatNo);

        if (duplicate)
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "ဤ Form No နှင့် ခုံနံပါတ်ဖြင့် Request တင်ပြီးသားရှိပါသည်။"
            });

        var entity = new RegisterAccount
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            FormNo = request.FormNo,
            ExamSeatNo = request.ExamSeatNo,
            Status = "Pending",
            CreatedDateTime = DateTime.Now
        };

        _db.RegisterAccounts.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new RegisterAccActionResponse
        {
            IsSuccess = true,
            Message = "Account Request တင်ပြမှု အောင်မြင်ပါသည်။ Admin မှ စစ်ဆေးပြီးနောက် Email ဖြင့် အကြောင်းကြားပါမည်။"
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/registeracc — Admin: paged list with optional status filter
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null)
    {
        var query = _db.RegisterAccounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            query = query.Where(r => r.Status == status);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(r =>
                r.FullName.Contains(searchTerm) ||
                r.Email.Contains(searchTerm) ||
                (r.FormNo != null && r.FormNo.Contains(searchTerm)) ||
                (r.ExamSeatNo != null && r.ExamSeatNo.Contains(searchTerm)));

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(r => r.CreatedDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RegisterAccListItem
            {
                RegisterAccId = r.RegisterAccId,
                FullName = r.FullName,
                Email = r.Email,
                Phone = r.Phone,
                FormNo = r.FormNo,
                ExamSeatNo = r.ExamSeatNo,
                Status = r.Status,
                RejectionReason = r.RejectionReason,
                CreatedDateTime = r.CreatedDateTime,
                ReviewedDateTime = r.ReviewedDateTime,
                ReviewedBy = r.ReviewedBy
            })
            .ToListAsync();

        return Ok(new RegisterAccPagedResponse
        {
            IsSuccess = true,
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = pageNumber
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/registeracc/{id}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var entity = await _db.RegisterAccounts.FindAsync(id);
        if (entity == null)
            return NotFound(new RegisterAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        return Ok(new RegisterAccListItem
        {
            RegisterAccId = entity.RegisterAccId,
            FullName = entity.FullName,
            Email = entity.Email,
            Phone = entity.Phone,
            FormNo = entity.FormNo,
            ExamSeatNo = entity.ExamSeatNo,
            Status = entity.Status,
            RejectionReason = entity.RejectionReason,
            CreatedDateTime = entity.CreatedDateTime,
            ReviewedDateTime = entity.ReviewedDateTime,
            ReviewedBy = entity.ReviewedBy
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/registeracc/{id}/approve — Admin approves & auto-creates user
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] RegisterAccActionRequest request)
    {
        var entity = await _db.RegisterAccounts.FindAsync(id);
        if (entity == null)
            return NotFound(new RegisterAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        if (entity.Status != "Pending")
            return BadRequest(new RegisterAccActionResponse { IsSuccess = false, Message = "ဤ Request ကို ဆောင်ရွက်ပြီးသားဖြစ်ပါသည်။" });

        // --- Find Student role ---
        var studentRole = await _db.Roles.FirstOrDefaultAsync(r =>
            r.RoleName == "Student" || r.RoleName == "student");

        if (studentRole == null)
            return StatusCode(500, new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "Student Role ကို ဒေတာဘေ့စ်မှာ ရှာမတွေ့ပါ။ Admin မှ Role ထပ်ဆောင်းပေးပါ။"
            });

        // --- Auto-generate username from FullName + seat no ---
        string baseUsername = GenerateUsername(entity.FullName, entity.ExamSeatNo ?? entity.RegisterAccId.ToString());
        string finalUsername = await EnsureUniqueUsername(baseUsername);

        // --- Auto-generate random password ---
        string plainPassword = GeneratePassword();
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        // --- Create User account ---
        var newUser = new User
        {
            RoleId = studentRole.RoleId,
            FullName = entity.FullName,
            UserName = finalUsername,
            Password = hashedPassword,
            MustChangePassword = true,
            IsDelete = false,
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.ReviewedBy ?? "System"
        };

        _db.Users.Add(newUser);

        // --- Mark RegisterAcc as Approved ---
        entity.Status = "Approved";
        entity.ReviewedDateTime = DateTime.Now;
        entity.ReviewedBy = request.ReviewedBy ?? "Admin";

        await _db.SaveChangesAsync();

        // --- Send approval email ---
        try
        {
            string subject = "Polytechnic University Maubin - Account Approved";
            string htmlBody = BuildApprovalEmail(entity.FullName, finalUsername, plainPassword);
            string plainTextBody = BuildApprovalEmailText(entity.FullName, finalUsername, plainPassword);
            await SendEmailAsync(entity.Email, subject, htmlBody, plainTextBody);
        }
        catch (Exception ex)
        {
            // Email failure should not roll back the approval
            Console.WriteLine($"[Email Error] Approval email failed: {ex.Message}");
        }

        return Ok(new RegisterAccActionResponse
        {
            IsSuccess = true,
            Message = $"Account Approve လုပ်ပြီးပါပြီ။ Username '{finalUsername}' ကို Email ဖြင့် ပေးပို့ပြီးပါပြီ။"
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/registeracc/{id}/reject — Admin rejects with optional reason
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RegisterAccActionRequest request)
    {
        var entity = await _db.RegisterAccounts.FindAsync(id);
        if (entity == null)
            return NotFound(new RegisterAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        if (entity.Status != "Pending")
            return BadRequest(new RegisterAccActionResponse { IsSuccess = false, Message = "ဤ Request ကို ဆောင်ရွက်ပြီးသားဖြစ်ပါသည်။" });

        entity.Status = "Rejected";
        entity.RejectionReason = request.RejectionReason;
        entity.ReviewedDateTime = DateTime.Now;
        entity.ReviewedBy = request.ReviewedBy ?? "Admin";

        await _db.SaveChangesAsync();

        // --- Send rejection email ---
        try
        {
            string subject = "Polytechnic University Maubin - Account Registration Update";
            string htmlBody = BuildRejectionEmail(entity.FullName, request.RejectionReason);
            string plainTextBody = BuildRejectionEmailText(entity.FullName, request.RejectionReason);
            await SendEmailAsync(entity.Email, subject, htmlBody, plainTextBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Email Error] Rejection email failed: {ex.Message}");
        }

        return Ok(new RegisterAccActionResponse
        {
            IsSuccess = true,
            Message = "Request Reject လုပ်ပြီးပါပြီ။ Email ဖြင့် အကြောင်းကြားပြီးပါပြီ။"
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: Generate username from name + seat no
    // ─────────────────────────────────────────────────────────────────────────
    private static string GenerateUsername(string fullName, string seatNo)
    {
        // Take first word of name + seat number, lowercase, remove special chars
        string firstWord = fullName.Split(' ')[0].ToLower();
        string cleaned = new string(firstWord.Where(char.IsLetterOrDigit).ToArray());
        string seatCleaned = new string(seatNo.Where(char.IsLetterOrDigit).ToArray());
        return $"{cleaned}.{seatCleaned}".ToLower();
    }

    private async Task<string> EnsureUniqueUsername(string baseUsername)
    {
        string candidate = baseUsername;
        int suffix = 1;
        while (await _db.Users.AnyAsync(u => u.UserName == candidate))
        {
            candidate = $"{baseUsername}{suffix}";
            suffix++;
        }
        return candidate;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: Random strong password
    // ─────────────────────────────────────────────────────────────────────────
    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var rng = new Random();
        var password = "SC@" + new string(Enumerable.Range(0, 8).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        return password;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: Email sender via SMTP (Multipart Text/HTML to avoid spam folder)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string plainTextBody)
    {
        var host = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var enableSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true");
        var senderEmail = _config["Email:SenderEmail"] ?? "";
        var senderName = _config["Email:SenderName"] ?? "Smart Campus PUMUB";
        var senderPassword = _config["Email:SenderPassword"] ?? "";

        using var client = new SmtpClient(host, port)
        {
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(senderEmail, senderPassword),
            EnableSsl = enableSsl
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject
        };
        mailMessage.To.Add(toEmail);

        // Add Plain Text view
        var plainView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
        mailMessage.AlternateViews.Add(plainView);

        // Add HTML view
        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
        mailMessage.AlternateViews.Add(htmlView);

        await client.SendMailAsync(mailMessage);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Email Templates
    // ─────────────────────────────────────────────────────────────────────────
    private static string BuildApprovalEmail(string name, string username, string password)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f0f4ff; margin: 0; padding: 20px; }}
    .card {{ background: #fff; border-radius: 16px; max-width: 520px; margin: auto; padding: 36px; box-shadow: 0 4px 24px rgba(37,99,235,0.1); }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .badge {{ display:inline-block; background: #10b981; color: white; border-radius: 50px; padding: 6px 18px; font-size: 0.85rem; font-weight:700; letter-spacing:0.05em; }}
    h2 {{ color: #1e3a8a; font-size: 1.4rem; margin: 12px 0 4px; }}
    p {{ color: #475569; line-height: 1.7; }}
    .cred-box {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 20px; margin: 20px 0; }}
    .cred-row {{ display: flex; justify-content: space-between; margin-bottom: 10px; }}
    .cred-label {{ color: #94a3b8; font-size: 0.85rem; font-weight: 600; }}
    .cred-val {{ color: #0f172a; font-weight: 700; font-family: monospace; font-size: 1rem; }}
    .footer {{ text-align: center; margin-top: 24px; color: #94a3b8; font-size: 0.8rem; }}
    .warn {{ background: #fef3c7; border-radius: 8px; padding: 12px 16px; color: #92400e; font-size: 0.85rem; margin-top: 16px; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <span class='badge'>✓ APPROVED</span>
      <h2>Account Registration Approved</h2>
      <p>Smart Campus · Polytechnic University Maubin</p>
    </div>
    <p>Dear <strong>{name}</strong>,</p>
    <p>သင်၏ Semester I Account Registration Request ကို အတည်ပြုပြီးပါပြီ။ အောက်ပါ Login Credentials များဖြင့် Smart Campus System ထဲ ဝင်ရောက်နိုင်ပါသည်။</p>
    <div class='cred-box'>
      <div class='cred-row'>
        <span class='cred-label'>Username</span>
        <span class='cred-val'>{username}</span>
      </div>
      <div class='cred-row'>
        <span class='cred-label'>Password</span>
        <span class='cred-val'>{password}</span>
      </div>
    </div>
    <div class='warn'>
      ⚠️ ကျေးဇူးပြု၍ ပထမဆုံးဝင်ချိန်တွင် Password ကို ချက်ချင်း ပြောင်းလဲပေးပါ။ ဤ Temporary Password ကို သိမ်းဆည်းထားခြင်း မပြုပါနှင့်။
    </div>
    <div class='footer'>
      © {DateTime.Now.Year} Smart Campus PUMUB &nbsp;·&nbsp; Polytechnic University Maubin
    </div>
  </div>
</body>
</html>";
    }

    private static string BuildRejectionEmail(string name, string? reason)
    {
        string reasonHtml = string.IsNullOrWhiteSpace(reason)
            ? ""
            : $"<p><strong>အကြောင်းရင်း:</strong> {reason}</p>";

        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f0f4ff; margin: 0; padding: 20px; }}
    .card {{ background: #fff; border-radius: 16px; max-width: 520px; margin: auto; padding: 36px; box-shadow: 0 4px 24px rgba(239,68,68,0.1); }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .badge {{ display:inline-block; background: #ef4444; color: white; border-radius: 50px; padding: 6px 18px; font-size: 0.85rem; font-weight:700; letter-spacing:0.05em; }}
    h2 {{ color: #7f1d1d; font-size: 1.4rem; margin: 12px 0 4px; }}
    p {{ color: #475569; line-height: 1.7; }}
    .reason-box {{ background: #fef2f2; border: 1px solid #fca5a5; border-radius: 12px; padding: 16px 20px; margin: 20px 0; color: #991b1b; }}
    .footer {{ text-align: center; margin-top: 24px; color: #94a3b8; font-size: 0.8rem; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <span class='badge'>✗ NOT APPROVED</span>
      <h2>Account Registration Update</h2>
      <p>Smart Campus · Polytechnic University Maubin</p>
    </div>
    <p>Dear <strong>{name}</strong>,</p>
    <p>သင်၏ Semester I Account Registration Request ကို ဤအကြိမ်တွင် လက်ခံနိုင်ခြင်း မရှိပါ။</p>
    {(string.IsNullOrWhiteSpace(reason) ? "" : $"<div class='reason-box'><strong>အကြောင်းရင်း:</strong><br/>{reason}</div>")}
    <p>ပြဿနာရှိပါက ကျောင်း Admin Office သို့ တိုက်ရိုက် ဆက်သွယ်နိုင်ပါသည်။</p>
    <div class='footer'>
      © {DateTime.Now.Year} Smart Campus PUMUB &nbsp;·&nbsp; Polytechnic University Maubin
    </div>
  </div>
</body>
</html>";
    }

    private static string BuildApprovalEmailText(string name, string username, string password)
    {
        return $@"Dear {name},

သင်၏ Semester I Account Registration Request ကို အတည်ပြုပြီးပါပြီ။ အောက်ပါ Login Credentials များဖြင့် Smart Campus System ထဲ ဝင်ရောက်နိုင်ပါသည်။

Username: {username}
Password: {password}

ကျေးဇူးပြု၍ ပထမဆုံးဝင်ချိန်တွင် Password ကို ချက်ချင်း ပြောင်းလဲပေးပါ။

Smart Campus PUMUB
Polytechnic University Maubin";
    }

    private static string BuildRejectionEmailText(string name, string? reason)
    {
        string reasonStr = string.IsNullOrWhiteSpace(reason) ? "" : $"\nအကြောင်းရင်း: {reason}\n";
        return $@"Dear {name},

သင်၏ Semester I Account Registration Request ကို ဤအကြိမ်တွင် လက်ခံနိုင်ခြင်း မရှိပါ။
{reasonStr}
ပြဿနာရှိပါက ကျောင်း Admin Office သို့ တိုက်ရိုက် ဆက်သွယ်နိုင်ပါသည်။

Smart Campus PUMUB
Polytechnic University Maubin";
    }
}
