using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Linq;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly JwtSettings _jwtSettings;

    public AuthController(SmartCampusDbContext db, JwtSettings jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] UserLoginRequestModel request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username နှင့် Password ဖြည့်ရန် လိုအပ်သည်။" });
        }

        // ── Step 1: Check User table (Admin, Staff, Tutor...) ──────────────
        var user = _db.Users.FirstOrDefault(x => x.UserName == request.UserName && x.IsDelete == false);

        if (user != null)
        {
            if (user.Status != "Active")
            {
                return Unauthorized(new { message = "သင်၏အကောင့်သည် Inactive ဖြစ်နေသဖြင့် Login ဝင်ရောက်ခွင့်မရှိပါ။" });
            }

            bool isPasswordValid = false;
            try
            {
                if (user.Password == request.Password)
                {
                    isPasswordValid = true;
                }
                else if (user.Password != null && user.Password.StartsWith("$2"))
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
                }
            }
            catch
            {
                isPasswordValid = user.Password == request.Password;
            }

            if (!isPasswordValid)
                return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });

            var role = _db.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
            var roleName = role?.RoleName ?? "Unknown";

            var permissions = _db.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == user.RoleId)
                .Select(rp => rp.Permission.PermissionName)
                .ToList();

            bool canAccessAllFaculties = string.Equals(roleName, "Super Admin", System.StringComparison.OrdinalIgnoreCase) 
                || user.RoleId == 4 
                || _db.RoleHierarchies.Any(rh => rh.ParentRoleId == user.RoleId && rh.CanAccessAllFaculties);

            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName),
                new System.Security.Claims.Claim("UserId", user.UserId.ToString()),
                new System.Security.Claims.Claim("FullName", user.FullName ?? string.Empty),
                new System.Security.Claims.Claim("FacultyId", user.FacultyId?.ToString() ?? ""),
                new System.Security.Claims.Claim("RoleId", user.RoleId.ToString()),
                new System.Security.Claims.Claim("CanAccessAllFaculties", canAccessAllFaculties ? "true" : "false")
            };

            foreach (var perm in permissions)
                claims.Add(new System.Security.Claims.Claim("Permission", perm));

            bool mustChangePassword = user.MustChangePassword ?? false;
            if (mustChangePassword)
            {
                return Ok(new
                {
                    isSuccess = true,
                    mustChangePassword = true,
                    message = "ကျေးဇူးပြု၍ Password အသစ် ပြောင်းလဲပေးပါ။",
                    userId = user.UserId,
                    fullName = user.FullName,
                    roleId = user.RoleId,
                    role = roleName,
                    token = (string?)null,
                    permissions = new System.Collections.Generic.List<string>()
                });
            }

            var userKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var userCreds = new Microsoft.IdentityModel.Tokens.SigningCredentials(userKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var userToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: System.DateTime.UtcNow.AddDays(7),
                signingCredentials: userCreds);
            var userJwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(userToken);

            return Ok(new
            {
                isSuccess = true,
                mustChangePassword = false,
                message = "Login ဝင်ရောက်ခြင်း အောင်မြင်ပါသည်။",
                userId = user.UserId,
                fullName = user.FullName,
                roleId = user.RoleId,
                role = roleName,
                token = userJwt,
                permissions = permissions
            });
        }

        // ── Step 2: Check NewStudentAcc table (New Students sent via email) ──
        var newStudent = _db.NewStudentAccs.FirstOrDefault(a => a.Username == request.UserName);

        if (newStudent == null)
            return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });

        // ── Active / Inactive check ──
        if (!string.Equals(newStudent.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
            return Unauthorized(new
            {
                message = "ဤ Account ကို ဆိုင်းငံ့ (Inactive) ထားပါသည်။ Admin ထံ ဆက်သွယ်ပေးပါ။",
                isInactive = true
            });

        bool nsPasswordValid = false;
        try
        {
            if (newStudent.PasswordHash.StartsWith("$2"))
                nsPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, newStudent.PasswordHash);
            else
                nsPasswordValid = newStudent.PasswordHash == request.Password;
        }
        catch
        {
            nsPasswordValid = newStudent.PasswordHash == request.Password;
        }

        if (!nsPasswordValid)
            return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });

        // ── MustChangePassword ──
        if (newStudent.MustChangePassword)
        {
            return Ok(new
            {
                isSuccess = true,
                mustChangePassword = true,
                message = "ကျေးဇူးပြု၍ Password အသစ် ပြောင်းလဲပေးပါ။",
                userId = newStudent.NewStudentAccId,
                fullName = newStudent.FullName,
                roleId = 0,
                role = "NewStudent",
                token = (string?)null,
                permissions = new System.Collections.Generic.List<string>()
            });
        }

        // ── Generate JWT for NewStudent ──
        var nsClaims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, newStudent.Username),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "NewStudent"),
            new System.Security.Claims.Claim("NewStudentAccId", newStudent.NewStudentAccId.ToString()),
            new System.Security.Claims.Claim("FullName", newStudent.FullName)
        };

        var nsKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var nsCreds = new Microsoft.IdentityModel.Tokens.SigningCredentials(nsKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var nsTokenObj = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: nsClaims,
            expires: System.DateTime.UtcNow.AddDays(7),
            signingCredentials: nsCreds);
        var nsJwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(nsTokenObj);

        return Ok(new
        {
            isSuccess = true,
            mustChangePassword = false,
            message = "Login ဝင်ရောက်ခြင်း အောင်မြင်ပါသည်။",
            userId = newStudent.NewStudentAccId,
            fullName = newStudent.FullName,
            roleId = 0,
            role = "NewStudent",
            token = nsJwt,
            permissions = new System.Collections.Generic.List<string>()
        });
    }

    [HttpPost("change-password")]
    [AllowAnonymous]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequestModel request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest(new { isSuccess = false, message = "အချက်အလက်အားလုံး ဖြည့်သွင်းရန် လိုအပ်သည်။" });
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { isSuccess = false, message = "Password သည် အနည်းဆုံး ၈ လုံး ရှိရမည်။" });
        }

        // ── Step 1: Check User table ──
        var user = _db.Users.FirstOrDefault(x => x.UserName == request.UserName && x.IsDelete == false);
        if (user != null)
        {
            bool isPasswordValid = false;
            try
            {
                if (user.Password != null && user.Password.StartsWith("$2"))
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password);
                else
                    isPasswordValid = user.Password == request.CurrentPassword;
            }
            catch { isPasswordValid = user.Password == request.CurrentPassword; }

            if (!isPasswordValid)
                return BadRequest(new { isSuccess = false, message = "လက်ရှိ Password မှားယွင်းနေပါသည်။" });

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.MustChangePassword = false;
            _db.SaveChanges();

            return Ok(new { isSuccess = true, message = "Password အသစ်ပြောင်းလဲခြင်း အောင်မြင်ပါသည်။ ကျေးဇူးပြု၍ Login ပြန်ဝင်ပေးပါ။" });
        }

        // ── Step 2: Check NewStudentAcc table ──
        var newStudent = _db.NewStudentAccs.FirstOrDefault(a => a.Username == request.UserName);
        if (newStudent == null)
            return BadRequest(new { isSuccess = false, message = "အသုံးပြုသူကို ရှာမတွေ့ပါ။" });

        bool nsPasswordValid = false;
        try
        {
            if (newStudent.PasswordHash.StartsWith("$2"))
                nsPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, newStudent.PasswordHash);
            else
                nsPasswordValid = newStudent.PasswordHash == request.CurrentPassword;
        }
        catch { nsPasswordValid = newStudent.PasswordHash == request.CurrentPassword; }

        if (!nsPasswordValid)
            return BadRequest(new { isSuccess = false, message = "လက်ရှိ Password မှားယွင်းနေပါသည်။" });

        newStudent.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        newStudent.MustChangePassword = false;
        newStudent.ModifiedDateTime = DateTime.Now;
        newStudent.ModifiedBy = "Self";
        _db.SaveChanges();

        return Ok(new { isSuccess = true, message = "Password အသစ်ပြောင်းလဲခြင်း အောင်မြင်ပါသည်။ ကျေးဇူးပြု၍ Login ပြန်ဝင်ပေးပါ။" });
    }
}

public class ChangePasswordRequestModel
{
    public string? UserName { get; set; }
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}


