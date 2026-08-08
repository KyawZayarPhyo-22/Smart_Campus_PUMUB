using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;

using Smart_Campus_PUMUB.WebApi.Services;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly JwtSettings _jwtSettings;
    private readonly IFacultyDataScopeService _scopeService;

    public UserController(SmartCampusDbContext db, JwtSettings jwtSettings, IFacultyDataScopeService scopeService)
    {
        _db = db;
        _jwtSettings = jwtSettings;
        _scopeService = scopeService;
    }

    // =========================================================================
    // 🎯 [NEW] ကျောင်းသားများ Website မှ ကိုယ်တိုင်အကောင့်ဖွင့်ရန် (Student Register)
    // =========================================================================
    [HttpPost("register")]
    public IActionResult Register(UserRegisterRequestModel request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.FullName))
        {
            return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = "အချက်အလက်များကို ပြည့်စုံစွာ ဖြည့်သွင်းပေးပါ။" });
        }

        string formattedUserName = request.UserName.Replace(" ", "_");

        if (!Regex.IsMatch(formattedUserName, "^[a-zA-Z0-9_]+$"))
        {
            return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = "Username တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
        }

        // Username ထပ်မထပ် စစ်ဆေးခြင်း
        var isUsernameExist = _db.Users.Any(x => x.UserName == formattedUserName && x.IsDelete == false);
        if (isUsernameExist)
        {
            return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = "ဤ Username သည် စနစ်ထဲတွင် ရှိနှင့်ပြီးသား ဖြစ်နေသည်။" });
        }

        // Password Validation (၈ လုံး၊ စာလုံးကြီး၊ စာလုံးသေး၊ သင်္ကေတ)
        var passwordError = ValidatePasswordPolicy(request.Password);
        if (passwordError != null) return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = passwordError });

        // 🔒 Password ကို လုံခြုံအောင် ဟက်ရှ် (Hash) ပြုလုပ်ခြင်း
        string hashedPass = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            RoleId = 3, // 💡 မင်းရဲ့ Role Table သတ်မှတ်ချက်အရ 3 သည် 'Student' ဖြစ်သည် (Default Assigned)
            FullName = request.FullName,
            UserName = formattedUserName,
            Password = hashedPass,
            IsDelete = false,
            MustChangePassword = false,
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            Status = "Active"
        };

        _db.Users.Add(newUser);
        int result = _db.SaveChanges();

        return StatusCode(201, new UserCreateResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "အကောင့်ဖွင့်ခြင်း အောင်မြင်ပါသည်။" : "အကောင့်ဖွင့်ခြင်း မအောင်မြင်ပါ။"
        });
    }

    // =========================================================================
    // 🎯 [NEW] အကောင့်ထဲသို့ Login ဝင်ရောက်ရန် API
    // =========================================================================
    [HttpPost("login")]
    public IActionResult Login(UserLoginRequestModel request)
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

        // 🔒 Hashed Password ကို ကိုက်ညီမှု ရှိမရှိ စစ်ဆေးခြင်း
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
            new System.Security.Claims.Claim("FullName", user.FullName ?? string.Empty),
            new System.Security.Claims.Claim("FacultyId", user.FacultyId?.ToString() ?? ""),
            new System.Security.Claims.Claim("RoleId", user.RoleId.ToString())
        };

        foreach (var perm in permissions)
        {
            claims.Add(new System.Security.Claims.Claim("Permission", perm));
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
            message = "Login ဝင်ရောက်ခြင်း အောင်မြင်ပါသည်။",
            userId = user.UserId,
            fullName = user.FullName,
            roleId = user.RoleId,
            role = roleName,
            token = jwtToken,
            permissions = permissions
        });
    }

    private static bool _userEmailColumnChecked = false;
    private void EnsureUserEmailColumn()
    {
        if (_userEmailColumnChecked) return;
        try
        {
            _db.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'Email')
                BEGIN
                    ALTER TABLE [dbo].[User] ADD [Email] NVARCHAR(150) NULL;
                END

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND name = 'Faculty_Id')
                BEGIN
                    ALTER TABLE [dbo].[User] ADD [Faculty_Id] INT NULL;
                END
            ");
            _userEmailColumnChecked = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"User Email/Faculty column check error: {ex.Message}");
        }
    }

    // ၁။ GET: User အားလုံးစာရင်းယူရန်
    [HttpGet]
    public IActionResult GetUsers()
    {
        EnsureUserEmailColumn();

        var query = from u in _db.Users
                    join r in _db.Roles on u.RoleId equals r.RoleId
                    join f in _db.Faculties on u.FacultyId equals f.FacultyId into facultyGroup
                    from f in facultyGroup.DefaultIfEmpty()
                    where u.IsDelete == false || u.IsDelete == null
                    select new { u, r, f };

        // Hierarchical RBAC Faculty Scoping:
        if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
        {
            var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
            if (scopedFacultyId.HasValue)
            {
                query = query.Where(x => x.u.FacultyId == scopedFacultyId.Value);
            }
        }

        var lst = query.OrderByDescending(x => x.u.UserId)
                       .Select(x => new UserModel
                       {
                           UserId = x.u.UserId,
                           RoleId = x.u.RoleId,
                           RoleName = x.r.RoleName,
                           FacultyId = x.u.FacultyId,
                           FacultyName = (x.r.RoleName == "Super Admin" || x.u.RoleId == 4) 
                                ? string.Join(" & ", _db.Faculties.Where(k => k.IsDelete == false || k.IsDelete == null).Select(k => k.FacultyName))
                                : (x.f != null ? x.f.FacultyName : null),
                           FullName = x.u.FullName,
                           UserName = x.u.UserName,
                           RoleNo = x.u.RoleNo,
                           Email = x.u.Email,
                           Password = "********",
                           CreatedDateTime = x.u.CreatedDateTime,
                           Status = x.u.Status
                       }).ToList();

        return Ok(lst);
    }

    // 🎯 [NEW] GET: api/user/roleno/{roleNo} - RoleNo ဖြင့် User ရှာဖွေပြီး Tutor Profile စစ်ဆေးရန်
    [HttpGet("roleno/{roleNo}")]
    public IActionResult GetUserByRoleNo(string roleNo)
    {
        var user = _db.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.RoleNo == roleNo && u.IsDelete == false);
            
        if (user == null)
        {
            return NotFound(new { IsSuccess = false, Message = "အသုံးပြုသူ ရှာမတွေ့ပါ။" });
        }

        // Check if tutor profile already exists in Tutor table
        var isTutorExist = _db.Tutors.Any(t => t.UserId == user.UserId && t.IsDelete == false);

        return Ok(new
        {
            IsSuccess = true,
            UserId = user.UserId,
            RoleId = user.RoleId,
            RoleName = user.Role?.RoleName,
            FullName = user.FullName,
            IsTutorExist = isTutorExist
        });
    }

    // ၂။ GET: User တစ်ဦးတည်း Profile ယူရန်
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var item = _db.Users
            .Include(x => x.Faculty)
            .FirstOrDefault(x => x.UserId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new { message = "အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        var userModel = new UserModel
        {
            UserId = item.UserId,
            RoleId = item.RoleId,
            FacultyId = item.FacultyId,
            FacultyName = item.Faculty?.FacultyName,
            FullName = item.FullName,
            UserName = item.UserName,
            RoleNo = item.RoleNo,
            Email = item.Email,
            Password = "********",
            CreatedDateTime = item.CreatedDateTime,
            Status = item.Status
        };

        return Ok(userModel);
    }

    // ၃။ POST: Admin မှ User အကောင့်အသစ်ဆောက်ပေးရန်
    [HttpPost]
    public IActionResult CreateUser(UserCreateRequestModel request)
    {
        EnsureUserEmailColumn();
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = "Username နှင့် Password ဖြည့်ရန် လိုအပ်သည်။" });
        }

        string formattedUserName = request.UserName.Replace(" ", "_");

        if (!Regex.IsMatch(formattedUserName, "^[a-zA-Z0-9_]+$"))
        {
            return BadRequest(new UserCreateResponseModel { Message = "Username တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
        }

        var isUsernameExist = _db.Users.Any(x => x.UserName == formattedUserName && x.IsDelete == false);
        if (isUsernameExist)
        {
            return BadRequest(new UserCreateResponseModel { Message = "ဤ Username သည် စနစ်ထဲတွင် ရှိပြီးသား ဖြစ်နေသည်။" });
        }

        var passwordError = ValidatePasswordPolicy(request.Password);
        if (passwordError != null) return BadRequest(new UserCreateResponseModel { Message = passwordError });

        // 🔒 Password အား Hash လုပ်ခြင်း
        string hashedPass = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            RoleId = request.RoleId,
            FacultyId = (request.FacultyId == null || request.FacultyId <= 0) ? null : request.FacultyId,
            FullName = request.FullName,
            UserName = formattedUserName,
            RoleNo = request.RoleNo,
            Email = request.Email,
            Password = hashedPass,
            IsDelete = false,
            MustChangePassword = true,
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            Status = string.IsNullOrEmpty(request.Status) ? "Active" : request.Status
        };

        _db.Users.Add(newUser);
        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "User Added",
            Description = $"User '{request.UserName}' was added from the system.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });
        _db.SaveChanges();

        return StatusCode(201, new UserCreateResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "အကောင့်အသစ် ဆောက်ခြင်း အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
        });
    }

    // ၄။ PUT: User အကောင့် Profile ပြင်ရန်
    [HttpPut("{id}")]
    public IActionResult UpdateUser(int id, UserUpdateRequestModel request)
    {
        EnsureUserEmailColumn();
        var item = _db.Users.FirstOrDefault(x => x.UserId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new UserUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        if (string.IsNullOrEmpty(request.UserName))
        {
            return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "Username ဖြည့်ရန် လိုအပ်သည်။" });
        }

        string formattedUserName = request.UserName.Replace(" ", "_");

        if (!Regex.IsMatch(formattedUserName, "^[a-zA-Z0-9_]+$"))
        {
            return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "Username တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
        }

        var isUsernameExist = _db.Users.Any(x => x.UserName == formattedUserName && x.UserId != id && (x.IsDelete == false || x.IsDelete == null));
        if (isUsernameExist)
        {
            return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "ဤ Username သည် အခြားသူတစ်ယောက် သုံးထားပြီးသား ဖြစ်သည်။" });
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordError = ValidatePasswordPolicy(request.Password);
            if (passwordError != null) return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = passwordError });
            item.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        item.RoleId = request.RoleId;
        item.FacultyId = (request.FacultyId == null || request.FacultyId <= 0) ? null : request.FacultyId;
        item.FullName = request.FullName;
        item.UserName = formattedUserName;
        item.RoleNo = request.RoleNo;
        item.Email = request.Email;
        if (!string.IsNullOrEmpty(request.Status))
        {
            item.Status = request.Status;
        }

        int result = _db.SaveChanges();
        _db.Activities.Add(new Activity
        {
            ActivityTitle = "User Updated",
            Description = $"User '{request.UserName}' was Updated from the system.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });
        _db.SaveChanges();

        return Ok(new UserUpdateResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "အချက်အလက် ပြင်ဆင်ခြင်း အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
            Data = new UserModel
            {
                UserId = item.UserId,
                RoleId = item.RoleId,
                FullName = item.FullName,
                UserName = item.UserName,
                RoleNo = item.RoleNo,
                Password = "********",
                CreatedDateTime = item.CreatedDateTime,
                Status = item.Status
            }
        });
    }

    // ၅။ PATCH: User အချက်အလက်များကို တစ်စိတ်တစ်ပိုင်းစီ လိုက်ပြင်ရန်
    [HttpPatch("{id}")]
    public IActionResult PatchUser(int id, UserUpdateRequestModel request)
    {
        var item = _db.Users.FirstOrDefault(x => x.UserId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new UserUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        int updateCount = 0;

        if (request.RoleId > 0)
        {
            item.RoleId = request.RoleId;
            updateCount++;
        }



        if (!string.IsNullOrEmpty(request.FullName))
        {
            item.FullName = request.FullName;
            updateCount++;
        }

        if (!string.IsNullOrEmpty(request.UserName))
        {
            string formattedUserName = request.UserName.Replace(" ", "_");

            if (!Regex.IsMatch(formattedUserName, "^[a-zA-Z0-9_]+$"))
            {
                return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "Username တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
            }

            var isUsernameExist = _db.Users.Any(x => x.UserName == formattedUserName && x.UserId != id && x.IsDelete == false);
            if (isUsernameExist)
            {
                return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "ဤ Username သည် အခြားသူတစ်ယောက် သုံးထားပြီးသား ဖြစ်သည်။" });
            }

            item.UserName = formattedUserName;
            updateCount++;
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            var passwordError = ValidatePasswordPolicy(request.Password);
            if (passwordError != null) return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = passwordError });

            item.Password = BCrypt.Net.BCrypt.HashPassword(request.Password); //🔒 Patch တွင်လည်း Hash ပြုလုပ်သိမ်းဆည်းခြင်း
            updateCount++;
        }

        if (request.RoleNo != null) // Allow empty string if user wants to clear it
        {
            if (!string.IsNullOrEmpty(request.RoleNo))
            {
                var isRoleNoExist = _db.Users.Any(x => x.RoleNo == request.RoleNo && x.UserId != id && x.IsDelete == false);
                if (isRoleNoExist)
                {
                    return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "ဤ Roll Number သည် အခြားသူတစ်ယောက် သုံးထားပြီးသား ဖြစ်သည်။" });
                }
            }
            item.RoleNo = request.RoleNo;
            updateCount++;
        }

        if (updateCount == 0)
        {
            return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် အချက်အလက်များ လိုအပ်ပါသည်။" });
        }

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "User Updated",
            Description = $"User '{request.UserName}' was Updated from the system.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });
        _db.SaveChanges();

        return Ok(new UserUpdateResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "အချက်အလက်များကို တစ်စိတ်တစ်ပိုင်း ပြင်ဆင်ခြင်း အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
            Data = new UserModel
            {
                UserId = item.UserId,
                RoleId = item.RoleId,
                FullName = item.FullName,
                UserName = item.UserName,
                RoleNo = item.RoleNo,
                Password = "********",
                CreatedDateTime = item.CreatedDateTime
            }
        });
    }

    // ၆။ DELETE: User ကို ပိတ်ပစ်ရန် (Soft Delete)
    [HttpDelete("{id}")]
    public IActionResult DeleteUser(int id)
    {
        var item = _db.Users.FirstOrDefault(x => x.UserId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new UserDeleteResponseModel { IsSuccess = false, Message = "ဖျက်မည့် အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        item.IsDelete = true;
        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "User Deleted",
            Description = $"User '{item.UserName}' was Deleted from the system.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });
        _db.SaveChanges();

        return Ok(new UserDeleteResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "အကောင့်ကို ပိတ်သိမ်း (Delete) ခြင်း အောင်မြင်ပါသည်။" : "အကောင့်ပိတ်ခြင်း မအောင်မြင်ပါ။"
        });
    }

    [HttpPatch("toggle-status/{id}")]
    [Smart_Campus_PUMUB.WebApi.Filters.Permission("User.Edit")]
    public IActionResult ToggleStatus(int id)
    {
        var item = _db.Users.FirstOrDefault(x => x.UserId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new { isSuccess = false, message = "အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        string newStatus = (item.Status == "Inactive") ? "Active" : "Inactive";
        item.Status = newStatus;
        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "User Status Toggled",
            Description = $"User '{item.UserName}' status was changed to '{newStatus}'.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });
        _db.SaveChanges();

        return Ok(new { isSuccess = result > 0, message = $"အသုံးပြုသူအဆင့်အတန်းအား {newStatus} သို့ ပြောင်းလဲပြီးပါပြီ။", status = newStatus });
    }
    //[HttpGet("count/by-role")]
    //public IActionResult GetCountByRole()
    //{
    //    var data = _db.Users // သင်၏ User သို့မဟုတ် Student/Staff စားပွဲနာမည်
    //        .GroupBy(u => u.Role)
    //        .Select(g => new { Name = g.Key, Y = g.Count() })
    //        .ToList();

    //    return Ok(data);
    //}

    [HttpGet("count/by-role")]
    [AllowAnonymous]
    public IActionResult GetCountByRole()
    {
        var result = _db.Users
            .Include(u => u.Role)
            .Where(u => u.IsDelete == false)
            .GroupBy(u => u.Role != null ? u.Role.RoleName : "Unknown")
            .Select(g => new
            {
                name = g.Key,
                y = (double)g.Count(),
                color = (g.Key == "Admin" || g.Key == "ADMIN") ? "#22d3ee" : ((g.Key == "Student" || g.Key == "STUDENT") ? "#10b981" : "#8b5cf6")
            }).ToList();

        return Ok(result);
    }

    [HttpGet("count/by-faculty")]
    [AllowAnonymous]
    public IActionResult GetCountByFaculty()
    {
        EnsureUserEmailColumn();

        var query = from u in _db.Users
                    join f in _db.Faculties on u.FacultyId equals f.FacultyId into facultyGroup
                    from f in facultyGroup.DefaultIfEmpty()
                    where u.IsDelete == false || u.IsDelete == null
                    select new { u, fName = f != null ? f.FacultyName : "Other/Unassigned" };

        var result = query.ToList()
            .GroupBy(x => x.fName)
            .Select(g => new
            {
                name = g.Key,
                y = (double)g.Count(),
                color = (g.Key.Contains("Computing", StringComparison.OrdinalIgnoreCase) || g.Key.Contains("Computer", StringComparison.OrdinalIgnoreCase)) ? "#38bdf8" :
                        (g.Key.Contains("Engineering", StringComparison.OrdinalIgnoreCase) ? "#8b5cf6" : "#10b981")
            }).ToList();

        return Ok(result);
    }

    [HttpGet("count/by-faculty-year")]
    [AllowAnonymous]
    public IActionResult GetCountByFacultyYear()
    {
        EnsureUserEmailColumn();

        var years = new List<string> { "2022-2023", "2023-2024", "2024-2025", "2025-2026", "2026-2027" };

        var usersWithFaculty = (from u in _db.Users
                                join f in _db.Faculties on u.FacultyId equals f.FacultyId into facultyGroup
                                from f in facultyGroup.DefaultIfEmpty()
                                join s in _db.Students on u.UserId equals s.UserId into studentGroup
                                from s in studentGroup.DefaultIfEmpty()
                                where (u.IsDelete == false || u.IsDelete == null)
                                select new
                                {
                                    u.UserId,
                                    u.CreatedDateTime,
                                    FacultyName = f != null ? f.FacultyName : "",
                                    ClassYear = s != null && !string.IsNullOrEmpty(s.CurrentClassYear) ? s.CurrentClassYear : null
                                }).ToList();

        int[] fcCounts = new int[5];
        int[] feCounts = new int[5];

        foreach (var item in usersWithFaculty)
        {
            int yearIdx = 4;
            
            if (item.CreatedDateTime.HasValue)
            {
                int y = item.CreatedDateTime.Value.Year;
                if (y == 2022) yearIdx = 0;
                else if (y == 2023) yearIdx = 1;
                else if (y == 2024) yearIdx = 2;
                else if (y == 2025) yearIdx = 3;
                else if (y >= 2026) yearIdx = 4;
                else yearIdx = Math.Abs(item.UserId) % 5;
            }
            else
            {
                yearIdx = Math.Abs(item.UserId) % 5;
            }

            if (item.FacultyName.Contains("Computing", StringComparison.OrdinalIgnoreCase) || item.FacultyName.Contains("Computer", StringComparison.OrdinalIgnoreCase))
            {
                fcCounts[yearIdx]++;
            }
            else
            {
                feCounts[yearIdx]++;
            }
        }

        var result = new
        {
            categories = years,
            series = new[]
            {
                new { name = "Faculty of Computing (FC)", color = "#38bdf8", data = fcCounts },
                new { name = "Faculty of Engineering (FE)", color = "#8b5cf6", data = feCounts }
            }
        };

        return Ok(result);
    }

    [HttpGet("paginate")]
    public IActionResult GetUsersPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? roleName = null,
        [FromQuery] string? facultyName = null)
    {
        EnsureUserEmailColumn();

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var query = from u in _db.Users
                    join r in _db.Roles on u.RoleId equals r.RoleId
                    join f in _db.Faculties on u.FacultyId equals f.FacultyId into facultyGroup
                    from f in facultyGroup.DefaultIfEmpty()
                    where u.IsDelete == false || u.IsDelete == null
                    select new { user = u, role = r, faculty = f };

        // Hierarchical RBAC Faculty Scoping:
        if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
        {
            var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
            if (scopedFacultyId.HasValue)
            {
                query = query.Where(x => x.user.FacultyId == scopedFacultyId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => (x.user.FullName != null && x.user.FullName.Contains(searchTerm)) ||
                                     (x.user.UserName != null && x.user.UserName.Contains(searchTerm)) ||
                                     (x.user.RoleNo != null && x.user.RoleNo.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(roleName) && !roleName.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.role.RoleName == roleName);
        }

        if (!string.IsNullOrWhiteSpace(facultyName) && !facultyName.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.faculty != null && x.faculty.FacultyName == facultyName);
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x.user.UserId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserModel
            {
                UserId = x.user.UserId,
                RoleId = x.user.RoleId,
                RoleName = x.role.RoleName,
                FacultyId = x.user.FacultyId,
                FacultyName = x.faculty != null ? x.faculty.FacultyName : null,
                FullName = x.user.FullName,
                UserName = x.user.UserName,
                RoleNo = x.user.RoleNo,
                Email = x.user.Email,
                Password = "********",
                CreatedDateTime = x.user.CreatedDateTime,
                Status = x.user.Status
            })
            .ToList();

        var result = new PagedResult<UserModel>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(result);
    }

    // 💡 ကုဒ်များ ထပ်ခါတလဲလဲ မဖြစ်စေရန် Password Policy ကို ခွဲထုတ်ထားသော သီးသန့် Private Method
    private string? ValidatePasswordPolicy(string password)
    {
        if (password.Length < 8) return "Password သည် အနည်းဆုံး ၈ လုံး ရှိရမည်။";
        if (!Regex.IsMatch(password, "[A-Z]")) return "Password တွင် အနည်းဆုံး အင်္ဂလိပ်စာလုံးကြီး (Capital Letter) တစ်လုံး ပါရမည်။";
        if (!Regex.IsMatch(password, "[a-z]")) return "Password တွင် အနည်းဆုံး အင်္ဂလိပ်စာလုံးသေး (Small Letter) တစ်လုံး ပါရမည်။";
        if (!Regex.IsMatch(password, "[^a-zA-Z0-9]")) return "Password တွင် အနည်းဆုံး သင်္ကေတ (Special Character) တစ်လုံး ပါရမည်။";
        return null;
    }
}