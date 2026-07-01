using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Linq;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public IActionResult Login([FromBody] UserLoginRequestModel request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username နှင့် Password ဖြည့်ရန် လိုအပ်သည်။" });
        }

        var user = _db.Users.FirstOrDefault(x => x.UserName == request.UserName && x.IsDelete == false);
        if (user is null)
        {
            return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });
        }

        bool isPasswordValid = false;
        try
        {
            if (user.Password != null && user.Password.StartsWith("$2"))
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            }
            else
            {
                isPasswordValid = user.Password == request.Password;
            }
        }
        catch
        {
            isPasswordValid = user.Password == request.Password;
        }

        if (!isPasswordValid)
        {
            return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });
        }

        var role = _db.Roles.FirstOrDefault(r => r.RoleId == user.RoleId);
        var roleName = role?.RoleName ?? "Unknown";

        var permissions = _db.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == user.RoleId)
            .Select(rp => rp.Permission.PermissionName)
            .ToList();

        var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName),
            new System.Security.Claims.Claim("UserId", user.UserId.ToString()),
            new System.Security.Claims.Claim("FullName", user.FullName ?? string.Empty)
        };

        foreach (var perm in permissions)
        {
            claims.Add(new System.Security.Claims.Claim("Permission", perm));
        }

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

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: System.DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        var jwtToken = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            isSuccess = true,
            mustChangePassword = false,
            message = "Login ဝင်ရောက်ခြင်း အောင်မြင်ပါသည်။",
            userId = user.UserId,
            fullName = user.FullName,
            roleId = user.RoleId,
            role = roleName,
            token = jwtToken,
            permissions = permissions
        });
    }

    [HttpPost("change-password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequestModel request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest(new { message = "အချက်အလက်အားလုံး ဖြည့်သွင်းရန် လိုအပ်သည်။" });
        }

        var user = _db.Users.FirstOrDefault(x => x.UserName == request.UserName && x.IsDelete == false);
        if (user is null)
        {
            return BadRequest(new { message = "အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        bool isPasswordValid = false;
        try
        {
            if (user.Password != null && user.Password.StartsWith("$2"))
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password);
            }
            else
            {
                isPasswordValid = user.Password == request.CurrentPassword;
            }
        }
        catch
        {
            isPasswordValid = user.Password == request.CurrentPassword;
        }

        if (!isPasswordValid)
        {
            return BadRequest(new { message = "လက်ရှိ Password မှားယွင်းနေပါသည်။" });
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "Password သည် အနည်းဆုံး ၈ လုံး ရှိရမည်။" });
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
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
