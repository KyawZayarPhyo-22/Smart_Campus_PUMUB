using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Mvc.Infrastructure.DefaultStatusCode(200)]
public class NewStudentAccController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public NewStudentAccController(SmartCampusDbContext db, JwtSettings jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/newstudentagc/login — NewStudentAcc Login (separate from User login)
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] NewStudentAccLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username နှင့် Password ဖြည့်ရန် လိုအပ်သည်။" });

        var acc = await _db.NewStudentAccs
            .FirstOrDefaultAsync(a => a.Username == request.Username);

        if (acc == null)
            return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });

        // ── Status check: Inactive account cannot login ──
        if (acc.AccountStatus == "Inactive")
            return Unauthorized(new
            {
                message = "ဤ Account ကို ဆိုင်းငံ့ (Inactive) ထားပါသည်။ Admin ထံ ဆက်သွယ်ပေးပါ။",
                isInactive = true
            });

        // ── Password verify ──
        bool isPasswordValid = false;
        try
        {
            if (acc.PasswordHash.StartsWith("$2"))
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, acc.PasswordHash);
            else
                isPasswordValid = acc.PasswordHash == request.Password;
        }
        catch
        {
            isPasswordValid = acc.PasswordHash == request.Password;
        }

        if (!isPasswordValid)
            return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });

        // ── MustChangePassword: return without token so client can redirect ──
        if (acc.MustChangePassword)
        {
            return Ok(new
            {
                isSuccess = true,
                mustChangePassword = true,
                message = "ကျေးဇူးပြု၍ Password အသစ် ပြောင်းလဲပေးပါ။",
                newStudentAccId = acc.NewStudentAccId,
                username = acc.Username,
                fullName = acc.FullName,
                token = (string?)null
            });
        }

        // ── Generate JWT ──
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.Name, acc.Username),
            new(System.Security.Claims.ClaimTypes.Role, "NewStudent"),
            new("NewStudentAccId", acc.NewStudentAccId.ToString()),
            new("FullName", acc.FullName)
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        var jwtToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            isSuccess = true,
            mustChangePassword = false,
            message = "Login ဝင်ရောက်ခြင်း အောင်မြင်ပါသည်။",
            newStudentAccId = acc.NewStudentAccId,
            username = acc.Username,
            fullName = acc.FullName,
            token = jwtToken
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/newstudentagc/change-password
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("change-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ChangePassword([FromBody] NewStudentAccChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "အချက်အလက်အားလုံး ဖြည့်သွင်းရန် လိုအပ်သည်။" });

        var acc = await _db.NewStudentAccs.FirstOrDefaultAsync(a => a.Username == request.Username);
        if (acc == null)
            return NotFound(new { message = "Account ရှာမတွေ့ပါ။" });

        if (acc.AccountStatus == "Inactive")
            return Unauthorized(new { message = "Account ကို Inactive ထားပါသည်။" });

        bool valid = false;
        try
        {
            valid = acc.PasswordHash.StartsWith("$2")
                ? BCrypt.Net.BCrypt.Verify(request.CurrentPassword, acc.PasswordHash)
                : acc.PasswordHash == request.CurrentPassword;
        }
        catch { valid = acc.PasswordHash == request.CurrentPassword; }

        if (request.NewPassword.Length < 8)
            return BadRequest(new { message = "Password သည် အနည်းဆုံး ၈ လုံး ရှိရမည်။" });

        if (request.NewPassword == request.CurrentPassword)
            return BadRequest(new { message = "စကားဝှက်အသစ်သည် လက်ရှိ (Email မှ ပို့ပေးထားသော) စကားဝှက်ဟောင်းနှင့် မတူညီရပါ။" });

        acc.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        acc.MustChangePassword = false;
        acc.ModifiedDateTime = DateTime.Now;
        acc.ModifiedBy = request.Username;

        await _db.SaveChangesAsync();

        return Ok(new { isSuccess = true, message = "Password ပြောင်းလဲမှု အောင်မြင်ပါသည်။ ပြန်ဝင်ပေးပါ။" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/newstudentagc — paged list with optional status filter & search
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    [Permission("NewStudentAcc.View")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? searchTerm = null)
    {
        var query = _db.NewStudentAccs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
            query = query.Where(a => a.AccountStatus == status);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(a =>
                a.FullName.Contains(searchTerm) ||
                a.Username.Contains(searchTerm) ||
                a.Email.Contains(searchTerm) ||
                (a.Phone != null && a.Phone.Contains(searchTerm)));

        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderByDescending(a => a.CreatedDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new NewStudentAccResponse
            {
                NewStudentAccId = a.NewStudentAccId,
                RegisterAccId = a.RegisterAccId,
                FullName = a.FullName,
                Email = a.Email,
                Phone = a.Phone,
                Username = a.Username,
                AccountStatus = a.AccountStatus,
                MustChangePassword = a.MustChangePassword,
                CreatedDateTime = a.CreatedDateTime,
                CreatedBy = a.CreatedBy,
                ModifiedDateTime = a.ModifiedDateTime,
                ModifiedBy = a.ModifiedBy
            })
            .ToListAsync();

        return Ok(new NewStudentAccPagedResponse
        {
            IsSuccess = true,
            Items = items,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CurrentPage = pageNumber
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/newstudentagc/{id}
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("{id}")]
    [Permission("NewStudentAcc.View")]
    public async Task<IActionResult> GetById(int id)
    {
        var a = await _db.NewStudentAccs.FindAsync(id);
        if (a == null)
            return NotFound(new NewStudentAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        return Ok(new NewStudentAccResponse
        {
            NewStudentAccId = a.NewStudentAccId,
            RegisterAccId = a.RegisterAccId,
            FullName = a.FullName,
            Email = a.Email,
            Phone = a.Phone,
            Username = a.Username,
            AccountStatus = a.AccountStatus,
            MustChangePassword = a.MustChangePassword,
            CreatedDateTime = a.CreatedDateTime,
            CreatedBy = a.CreatedBy,
            ModifiedDateTime = a.ModifiedDateTime,
            ModifiedBy = a.ModifiedBy
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/newstudentagc — Admin manual create
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [Permission("NewStudentAcc.Create")]
    public async Task<IActionResult> Create([FromBody] NewStudentAccCreateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new NewStudentAccActionResponse { IsSuccess = false, Message = "ဖြည့်ထားသောအချက်အလက် မမှန်ကန်ပါ။" });

        string baseUsername = GenerateUsername(request.FullName, request.RegisterAccId?.ToString() ?? DateTime.Now.Ticks.ToString());
        string finalUsername = await EnsureUniqueUsername(baseUsername);
        string plainPassword = GeneratePassword();
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        var entity = new NewStudentAcc
        {
            RegisterAccId = request.RegisterAccId,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Username = finalUsername,
            PasswordHash = hashedPassword,
            AccountStatus = "Active",
            MustChangePassword = true,
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.CreatedBy ?? "Admin"
        };

        _db.NewStudentAccs.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            IsSuccess = true,
            Message = $"Account ဖန်တီးမှု အောင်မြင်ပါသည်။",
            Username = finalUsername,
            Password = plainPassword,
            NewStudentAccId = entity.NewStudentAccId
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT api/newstudentagc/{id} — Update basic info
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPut("{id}")]
    [Permission("NewStudentAcc.Edit")]
    public async Task<IActionResult> Update(int id, [FromBody] NewStudentAccCreateRequest request)
    {
        var entity = await _db.NewStudentAccs.FindAsync(id);
        if (entity == null)
            return NotFound(new NewStudentAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        entity.FullName = request.FullName;
        entity.Email = request.Email;
        entity.Phone = request.Phone;
        entity.ModifiedDateTime = DateTime.Now;
        entity.ModifiedBy = request.CreatedBy ?? "Admin";

        await _db.SaveChangesAsync();

        return Ok(new NewStudentAccActionResponse { IsSuccess = true, Message = "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUT api/newstudentagc/{id}/status — Toggle Active / Inactive
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPut("{id}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] NewStudentAccUpdateStatusRequest request)
    {
        var entity = await _db.NewStudentAccs.FindAsync(id);
        if (entity == null)
            return NotFound(new NewStudentAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        if (request == null || string.IsNullOrWhiteSpace(request.AccountStatus))
            return BadRequest(new NewStudentAccActionResponse { IsSuccess = false, Message = "Request body သို့မဟုတ် AccountStatus မပြည့်စုံပါ။" });

        var trimmedStatus = request.AccountStatus.Trim();
        if (!string.Equals(trimmedStatus, "Active", StringComparison.OrdinalIgnoreCase) && 
            !string.Equals(trimmedStatus, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new NewStudentAccActionResponse
            {
                IsSuccess = false,
                Message = "AccountStatus သည် 'Active' သို့မဟုတ် 'Inactive' သာ ဖြစ်ရမည်။"
            });
        }

        entity.AccountStatus = string.Equals(trimmedStatus, "Active", StringComparison.OrdinalIgnoreCase) ? "Active" : "Inactive";
        entity.ModifiedDateTime = DateTime.Now;
        entity.ModifiedBy = request.ModifiedBy ?? "Admin";

        await _db.SaveChangesAsync();

        string statusMm = request.AccountStatus == "Active" ? "ဖွင့်" : "ပိတ်";
        return Ok(new NewStudentAccActionResponse
        {
            IsSuccess = true,
            Message = $"Account ကို {statusMm}လိုက်ပါပြီ။ (Status: {request.AccountStatus})"
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE api/newstudentagc/{id} — Hard delete
    // ─────────────────────────────────────────────────────────────────────────
    [HttpDelete("{id}")]
    [Permission("NewStudentAcc.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.NewStudentAccs.FindAsync(id);
        if (entity == null)
            return NotFound(new NewStudentAccActionResponse { IsSuccess = false, Message = "ရှာမတွေ့ပါ။" });

        _db.NewStudentAccs.Remove(entity);
        await _db.SaveChangesAsync();

        return Ok(new NewStudentAccActionResponse { IsSuccess = true, Message = "Account ဖျက်ပြီးပါပြီ။" });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────
    private static string GenerateUsername(string fullName, string seed)
    {
        string firstWord = fullName.Split(' ')[0].ToLower();
        string cleaned = new string(firstWord.Where(char.IsLetterOrDigit).ToArray());
        string seedCleaned = new string(seed.Where(char.IsLetterOrDigit).ToArray());
        // Limit seed to last 6 chars to keep username short
        if (seedCleaned.Length > 6) seedCleaned = seedCleaned[^6..];
        return $"{cleaned}.{seedCleaned}".ToLower();
    }

    private async Task<string> EnsureUniqueUsername(string baseUsername)
    {
        string candidate = baseUsername;
        int suffix = 1;
        while (await _db.NewStudentAccs.AnyAsync(a => a.Username == candidate))
        {
            candidate = $"{baseUsername}{suffix}";
            suffix++;
        }
        return candidate;
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        var rng = new Random();
        return "SC@" + new string(Enumerable.Range(0, 8).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Change-password request model
// ─────────────────────────────────────────────────────────────────────────────
public class NewStudentAccChangePasswordRequest
{
    public string? Username { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
