using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Net;
using System.Net.Mail;

using Smart_Campus_PUMUB.WebApi.Services;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterAccController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly IConfiguration _config;
    private readonly IFacultyDataScopeService _scopeService;

    public RegisterAccController(SmartCampusDbContext db, IConfiguration config, IFacultyDataScopeService scopeService)
    {
        _db = db;
        _config = config;
        _scopeService = scopeService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/registeracc/test-email — SMTP Diagnosis
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("test-email")]
    //[AllowAnonymous]
    [Permission("RegisterAcc.View")]

    public async Task<IActionResult> TestEmail([FromQuery] string to)
    {
        try
        {
            string html = "<h1>Test Email</h1><p>Academic University Registration Assistant (AURA) Brevo Test is working!</p>";
            string text = "Test Email - Academic University Registration Assistant (AURA) Brevo Test is working!";
            await SendEmailAsync(to, "Test Email from Academic University Registration Assistant (AURA)", html, text);
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
    [AllowAnonymous]

    public async Task<IActionResult> Submit([FromBody] RegisterAccCreateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new RegisterAccActionResponse { IsSuccess = false, Message = "ဖြည့်စွက်ထားသော အချက်အလက်တွေ မပြည့်စုံပါ။" });

        var nonMmRegex = new System.Text.RegularExpressions.Regex(@"[^\u1000-\u1049\u104E\u103F\-\/\s]");
        if (nonMmRegex.IsMatch(request.FormNo ?? "") || nonMmRegex.IsMatch(request.ExamSeatNo ?? ""))
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "တက္ကသိုလ်ဝင် Form No နှင့် ဆယ်တန်းခုံနံပါတ် တွင် မြန်မာအက္ခရာနှင့် မြန်မာဂဏန်းများသာ ထည့်သွင်းခွင့်ရှိပါသည် (ဥပမာ - မအူ-၁၂၃၊ သရ-၄၅၆)။"
            });
        }

        string phone = (request.Phone ?? "").Trim();
        if (string.IsNullOrWhiteSpace(phone) || !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^09[2-9]\d{6,8}$"))
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "မှန်ကန်သော မြန်မာနိုင်ငံ မိုဘိုင်းဖုန်းနံပါတ် (၀၉xxxxxxxxx) ကို ထည့်သွင်းပေးပါ (ဂဏန်း ၉ လုံးမှ ၁၁ လုံး)။"
            });
        }

        // Check repeated dummy digits like 09111111111, 09999999999, etc.
        if (phone.Length > 2 && phone.Substring(2).Distinct().Count() <= 1)
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "တရားမဝင်သော သို့မဟုတ် ထပ်ခါတလဲလဲ ဂဏန်းများဖြင့် ဖွဲ့စည်းထားသော ဖုန်းနံပါတ် ဖြစ်နေပါသည်။"
            });
        }

        string[] dummySequences = { "09123456789", "09987654321", "09012345678", "0912345678", "0987654321" };
        if (dummySequences.Contains(phone))
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "စမ်းသပ်ဂဏန်းအစဉ်လိုက် ဖုန်းနံပါတ်များကို ထည့်သွင်းခွင့်မပြုပါ။"
            });
        }

        string email = (request.Email ?? "").Trim();
        // 1. Check duplicate Email in Users, NewStudentAccs, or active RegisterAccounts
        bool emailExists = await _db.Users.AnyAsync(u => u.Email == email && u.IsDelete == false) ||
                           await _db.NewStudentAccs.AnyAsync(a => a.Email == email) ||
                           await _db.RegisterAccounts.AnyAsync(r => r.Email == email && r.Status != "Rejected");

        if (emailExists)
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "ဤ Email ဖြင့် အကောင့်ဖွင့်ထားပြီး သို့မဟုတ် Request တင်ထားပြီး ဖြစ်ပါသည်။ ကျေးဇူးပြု၍ Login ဝင်ပါ (သို့မဟုတ်) Forgot Password ပြုလုပ်ပါ။"
            });
        }

        // 2. Check duplicate Phone in NewStudentAccs or active RegisterAccounts
        bool phoneExists = await _db.NewStudentAccs.AnyAsync(a => a.Phone == phone) ||
                           await _db.RegisterAccounts.AnyAsync(r => r.Phone == phone && r.Status != "Rejected");

        if (phoneExists)
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "ဤဖုန်းနံပါတ်ဖြင့် အကောင့်ဖွင့်ထားပြီး သို့မဟုတ် Request တင်ထားပြီး ဖြစ်ပါသည်။"
            });
        }

        string formNo = (request.FormNo ?? "").Trim();
        string examSeatNo = (request.ExamSeatNo ?? "").Trim();

        // 3. Check duplicate FormNo
        bool formNoExists = await _db.RegisterAccounts.AnyAsync(r => r.FormNo == formNo && r.Status != "Rejected");
        if (formNoExists)
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "ဤ Form No ဖြင့် Request တင်ထားပြီး ဖြစ်ပါသည်။"
            });
        }

        // 4. Check duplicate ExamSeatNo in Users (RoleNo) or active RegisterAccounts
        bool examSeatExists = await _db.Users.AnyAsync(u => u.RoleNo != null && u.RoleNo != "" && u.RoleNo == examSeatNo && u.IsDelete == false) ||
                              await _db.RegisterAccounts.AnyAsync(r => r.ExamSeatNo == examSeatNo && r.Status != "Rejected");

        if (examSeatExists)
        {
            return BadRequest(new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = "ဤဆယ်တန်းခုံနံပါတ်ဖြင့် အကောင့်ဖွင့်ထားပြီး သို့မဟုတ် Request တင်ထားပြီး ဖြစ်ပါသည်။"
            });
        }

        var entity = new RegisterAccount
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = phone,
            FormNo = formNo,
            ExamSeatNo = examSeatNo,
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
    //[Authorize]
    [Permission("RegisterAcc.View")]

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

        var rawList = await query
            .OrderByDescending(r => r.CreatedDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var regIds = rawList.Select(r => r.RegisterAccId).ToList();

        var newAccs = await _db.NewStudentAccs
            .Where(n => n.RegisterAccId.HasValue && regIds.Contains(n.RegisterAccId.Value))
            .ToListAsync();

        var items = rawList.Select(r =>
        {
            var matchedAcc = newAccs.FirstOrDefault(n => n.RegisterAccId == r.RegisterAccId);

            int? newStudentAccId = matchedAcc?.NewStudentAccId;
            string? accStatus = null;

            if (r.Status == "Approved")
            {
                accStatus = matchedAcc?.AccountStatus ?? "Active";
            }

            return new RegisterAccListItem
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
                ReviewedBy = r.ReviewedBy,
                NewStudentAccId = newStudentAccId,
                AccountStatus = accStatus
            };
        }).ToList();

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
    [Permission("RegisterAcc.Edit")]

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
    [Permission("RegisterAcc.Edit")]

    public async Task<IActionResult> Approve(int id, [FromBody] RegisterAccActionRequest request)
    {
        try
        {
            var entity = await _db.RegisterAccounts.FindAsync(id);
            if (entity == null)
                return NotFound(new RegisterAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

            if (entity.Status != "Pending")
                return BadRequest(new RegisterAccActionResponse { IsSuccess = false, Message = "ဤ Request ကို ဆောင်ရွက်ပြီးသားဖြစ်ပါသည်။" });

            // --- Auto-generate username from FullName (all lowercase with underscores) ---
            string baseUsername = GenerateUsername(entity.FullName);
            string finalUsername = await EnsureUniqueUsername(baseUsername);

            // --- Auto-generate random password ---
            string plainPassword = GeneratePassword();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            // 1. --- Create or update User record in User table with Student Role (RoleId = 3) ---
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == entity.Email && u.IsDelete == false);
            int userId;

            if (existingUser != null)
            {
                existingUser.Password = hashedPassword;
                existingUser.Status = "Active";
                existingUser.MustChangePassword = true;
                existingUser.RoleNo = entity.ExamSeatNo ?? entity.FormNo;
                existingUser.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
                existingUser.ModifiedBy = request.ReviewedBy ?? "Admin";
                userId = existingUser.UserId;
                finalUsername = existingUser.UserName;
            }
            else
            {
                var newUser = new User
                {
                    RoleId = 3, // 3 = Student
                    FullName = entity.FullName,
                    UserName = finalUsername,
                    RoleNo = entity.ExamSeatNo ?? entity.FormNo,
                    Email = entity.Email,
                    Password = hashedPassword,
                    MustChangePassword = true,
                    Status = "Active",
                    IsDelete = false,
                    CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                    CreatedBy = request.ReviewedBy ?? "Admin"
                };

                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();
                userId = newUser.UserId;
            }

            // 2. --- Create or link Student record in Student table ---
            var existingStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (existingStudent == null)
            {
                var student = new Student
                {
                    UserId = userId,
                    CurrentRollNo = entity.ExamSeatNo ?? entity.FormNo,
                    CurrentClassYear = "First Year",
                    CurrentMajor = "N/A",
                    Status = "Active",
                    IsDelete = false,
                    CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                    CreatedBy = request.ReviewedBy ?? "Admin"
                };
                _db.Students.Add(student);
            }
            else
            {
                existingStudent.CurrentRollNo = entity.ExamSeatNo ?? entity.FormNo;
                existingStudent.Status = "Active";
            }

            // 3. --- Create or update NewStudentAcc record for compatibility ---
            var newStudentAcc = await _db.NewStudentAccs.FirstOrDefaultAsync(a => a.RegisterAccId == entity.RegisterAccId || a.Email == entity.Email);
            if (newStudentAcc != null)
            {
                newStudentAcc.Username = finalUsername;
                newStudentAcc.PasswordHash = hashedPassword;
                newStudentAcc.AccountStatus = "Active";
                newStudentAcc.MustChangePassword = true;
                newStudentAcc.FullName = entity.FullName;
                newStudentAcc.Phone = entity.Phone;
                newStudentAcc.ModifiedDateTime = DateTime.Now;
                newStudentAcc.ModifiedBy = request.ReviewedBy ?? "Admin";
            }
            else
            {
                newStudentAcc = new NewStudentAcc
                {
                    RegisterAccId = entity.RegisterAccId,
                    FullName = entity.FullName,
                    Email = entity.Email,
                    Phone = entity.Phone,
                    Username = finalUsername,
                    PasswordHash = hashedPassword,
                    AccountStatus = "Active",
                    MustChangePassword = true,
                    CreatedDateTime = DateTime.Now,
                    CreatedBy = request.ReviewedBy ?? "System"
                };
                _db.NewStudentAccs.Add(newStudentAcc);
            }

            // 4. --- Mark RegisterAcc as Approved ---
            entity.Status = "Approved";
            entity.ReviewedDateTime = DateTime.Now;
            entity.ReviewedBy = request.ReviewedBy ?? "Admin";

            await _db.SaveChangesAsync();

            // 5. --- Send approval email with credentials ---
            try
            {
                string subject = "Polytechnic University (Maubin) - Account Approved & Login Details";
                string htmlBody = BuildApprovalEmail(entity.FullName, finalUsername, plainPassword);
                string plainTextBody = BuildApprovalEmailText(entity.FullName, finalUsername, plainPassword);
                await SendEmailAsync(entity.Email, subject, htmlBody, plainTextBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email Error] Approval email failed: {ex.Message}");
            }

            return Ok(new RegisterAccActionResponse
            {
                IsSuccess = true,
                Message = $"Account Approve လုပ်ပြီးပါပြီ။ Username '{finalUsername}' နှင့် Password ကို Email ({entity.Email}) သို့ ပေးပို့ပြီးပါပြီ။"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new RegisterAccActionResponse
            {
                IsSuccess = false,
                Message = $"Approve လုပ်ဆောင်ရာတွင် အမှားဖြစ်ပါသည်: {ex.Message}"
            });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/registeracc/{id}/reject — Admin rejects with optional reason
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("{id}/reject")]
    [Permission("RegisterAcc.Edit")]

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
            string subject = "Polytechnic University (Maubin) - Account Registration Update";
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
    // PUT /api/registeracc/{id}/status — Admin toggles Active / Inactive
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPut("{id}/status")]
    [Permission("RegisterAcc.Edit")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] NewStudentAccUpdateStatusRequest request)
    {
        var entity = await _db.RegisterAccounts.FindAsync(id);
        if (entity == null)
            return NotFound(new RegisterAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        var trimmedStatus = (request?.AccountStatus ?? "Active").Trim();
        bool isTargetActive = string.Equals(trimmedStatus, "Active", StringComparison.OrdinalIgnoreCase);
        string newStatus = isTargetActive ? "Active" : "Inactive";

        // Find NewStudentAcc specifically linked to this RegisterAccId
        var newAcc = await _db.NewStudentAccs.FirstOrDefaultAsync(n => n.RegisterAccId == entity.RegisterAccId);

        if (newAcc != null)
        {
            newAcc.AccountStatus = newStatus;
            newAcc.ModifiedDateTime = DateTime.Now;
            newAcc.ModifiedBy = request?.ModifiedBy ?? "Admin";
        }
        else if (entity.Status == "Approved")
        {
            // Auto create NewStudentAcc if missing for approved record
            string baseUsername = GenerateUsername(entity.FullName);
            string finalUsername = await EnsureUniqueUsernameInNewStudentAcc(baseUsername);
            string plainPassword = GeneratePassword();
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

            var createdNewAcc = new NewStudentAcc
            {
                RegisterAccId = entity.RegisterAccId,
                FullName = entity.FullName,
                Email = entity.Email,
                Phone = entity.Phone,
                Username = finalUsername,
                PasswordHash = hashedPassword,
                AccountStatus = newStatus,
                MustChangePassword = true,
                CreatedDateTime = DateTime.Now,
                CreatedBy = request?.ModifiedBy ?? "Admin"
            };
            _db.NewStudentAccs.Add(createdNewAcc);
        }

        await _db.SaveChangesAsync();

        return Ok(new RegisterAccActionResponse
        {
            IsSuccess = true,
            Message = $"Account Status ကို {newStatus} သို့ အောင်မြင်စွာ ပြောင်းလဲပြီးပါပြီ။"
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper: Generate username from full name (all lowercase, spaces replaced by underscore)
    // ─────────────────────────────────────────────────────────────────────────
    private static string GenerateUsername(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "student";

        // Full Name အပြည့်အစုံကို အသေးစာလုံးပြောင်းပြီး space နေရာတွင် '_' ထည့်သွင်းခြင်း
        string lower = fullName.Trim().ToLowerInvariant();
        string replaced = System.Text.RegularExpressions.Regex.Replace(lower, @"\s+", "_");
        string cleaned = new string(replaced.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"_+", "_").Trim('_');

        return string.IsNullOrWhiteSpace(cleaned) ? "student" : cleaned;
    }

    private async Task<string> EnsureUniqueUsername(string baseUsername)
    {
        string candidate = baseUsername;
        int suffix = 1;
        while (await _db.Users.AnyAsync(u => u.UserName == candidate && u.IsDelete == false) ||
               await _db.NewStudentAccs.AnyAsync(a => a.Username == candidate))
        {
            candidate = $"{baseUsername}_{suffix}";
            suffix++;
        }
        return candidate;
    }

    /// <summary>NewStudentAcc table တွင် username ထပ်မနေရ</summary>
    private async Task<string> EnsureUniqueUsernameInNewStudentAcc(string baseUsername)
    {
        string candidate = baseUsername;
        int suffix = 1;
        while (await _db.NewStudentAccs.AnyAsync(a => a.Username == candidate))
        {
            candidate = $"{baseUsername}_{suffix}";
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
        var senderName = _config["Email:SenderName"] ?? "Polytechnic University (Maubin) · Academic University Registration Assistant (AURA)";
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

        // Add HTML view with inline logo linked resource
        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");

        string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "pumub_logo.png");
        if (!System.IO.File.Exists(logoPath))
        {
            logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "pumub_logo.png");
        }

        if (System.IO.File.Exists(logoPath))
        {
            var logoResource = new LinkedResource(logoPath, "image/png")
            {
                ContentId = "pumub_logo",
                TransferEncoding = System.Net.Mime.TransferEncoding.Base64
            };
            htmlView.LinkedResources.Add(logoResource);
        }

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
    .badge {{ display:inline-block; background: #10b981; color: white; border-radius: 50px; padding: 6px 18px; font-size: 0.85rem; font-weight:700; letter-spacing:0.05em; margin-top: 12px; }}
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
      <div style='margin-bottom: 14px; text-align: center;'>
        <img src='cid:pumub_logo' alt='Polytechnic University (Maubin)' width='80' height='80' style='display: inline-block; width: 80px; height: 80px; border-radius: 50%; box-shadow: 0 4px 12px rgba(0,0,0,0.08);' />
      </div>
      <span class='badge'>✓ APPROVED</span>
      <h2>Account Registration Approved</h2>
      <p style='margin: 6px 0 0; font-size: 0.9rem; color: #64748b;'>Academic University Registration Assistant (AURA) · Polytechnic University (Maubin)</p>
    </div>
    <p>Dear <strong>{name}</strong>,</p>
    <p>သင်၏ Semester I Account Registration Request ကို အတည်ပြုပြီးပါပြီ။ အောက်ပါ Login Credentials များဖြင့် Academic University Registration Assistant (AURA) System ထဲ ဝင်ရောက်နိုင်ပါသည်။</p>
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
      © {DateTime.Now.Year} Academic University Registration Assistant (AURA) &nbsp;·&nbsp; Polytechnic University (Maubin)
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
    .badge {{ display:inline-block; background: #ef4444; color: white; border-radius: 50px; padding: 6px 18px; font-size: 0.85rem; font-weight:700; letter-spacing:0.05em; margin-top: 12px; }}
    h2 {{ color: #7f1d1d; font-size: 1.4rem; margin: 12px 0 4px; }}
    p {{ color: #475569; line-height: 1.7; }}
    .reason-box {{ background: #fef2f2; border: 1px solid #fca5a5; border-radius: 12px; padding: 16px 20px; margin: 20px 0; color: #991b1b; }}
    .footer {{ text-align: center; margin-top: 24px; color: #94a3b8; font-size: 0.8rem; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <div style='margin-bottom: 14px; text-align: center;'>
        <img src='cid:pumub_logo' alt='Polytechnic University (Maubin)' width='80' height='80' style='display: inline-block; width: 80px; height: 80px; border-radius: 50%; box-shadow: 0 4px 12px rgba(0,0,0,0.08);' />
      </div>
      <span class='badge'>✗ NOT APPROVED</span>
      <h2>Account Registration Update</h2>
      <p style='margin: 6px 0 0; font-size: 0.9rem; color: #64748b;'>Academic University Registration Assistant (AURA) · Polytechnic University (Maubin)</p>
    </div>
    <p>Dear <strong>{name}</strong>,</p>
    <p>သင်၏ Semester I Account Registration Request ကို ဤအကြိမ်တွင် လက်ခံနိုင်ခြင်း မရှိပါ။</p>
    {(string.IsNullOrWhiteSpace(reason) ? "" : $"<div class='reason-box'><strong>အကြောင်းရင်း:</strong><br/>{reason}</div>")}
    <p>ပြဿနာရှိပါက ကျောင်း Admin Office သို့ တိုက်ရိုက် ဆက်သွယ်နိုင်ပါသည်။</p>
    <div class='footer'>
      © {DateTime.Now.Year} Academic University Registration Assistant (AURA) &nbsp;·&nbsp; Polytechnic University (Maubin)
    </div>
  </div>
</body>
</html>";
    }

    private static string BuildApprovalEmailText(string name, string username, string password)
    {
        return $@"Dear {name},

သင်၏ Semester I Account Registration Request ကို အတည်ပြုပြီးပါပြီ။ အောက်ပါ Login Credentials များဖြင့် Academic University Registration Assistant (AURA) System ထဲ ဝင်ရောက်နိုင်ပါသည်။

Username: {username}
Password: {password}

ကျေးဇူးပြု၍ ပထမဆုံးဝင်ချိန်တွင် Password ကို ချက်ချင်း ပြောင်းလဲပေးပါ။

Academic University Registration Assistant (AURA)
Polytechnic University (Maubin)";
    }

    private static string BuildRejectionEmailText(string name, string? reason)
    {
        string reasonStr = string.IsNullOrWhiteSpace(reason) ? "" : $"\nအကြောင်းရင်း: {reason}\n";
        return $@"Dear {name},

သင်၏ Semester I Account Registration Request ကို ဤအကြိမ်တွင် လက်ခံနိုင်ခြင်း မရှိပါ။
{reasonStr}
ပြဿနာရှိပါက ကျောင်း Admin Office သို့ တိုက်ရိုက် ဆက်သွယ်နိုင်ပါသည်။

Academic University Registration Assistant (AURA)
Polytechnic University (Maubin)";
    }
}


