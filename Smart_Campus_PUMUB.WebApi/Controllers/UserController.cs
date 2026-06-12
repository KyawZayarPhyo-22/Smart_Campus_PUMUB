using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public UserController(SmartCampusDbContext db)
    {
        _db = db;
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
            RoleId = 4, // 💡 မင်းရဲ့ Role Table သတ်မှတ်ချက်အရ 4 သည် 'Student' ဖြစ်သည် (Default Assigned)
            FullName = request.FullName,
            UserName = formattedUserName,
            Password = hashedPass,
            IsDelete = false,
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
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
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        if (!isPasswordValid)
        {
            return Unauthorized(new { message = "Username သို့မဟုတ် Password မှားယွင်းနေပါသည်။" });
        }

        return Ok(new
        {
            isSuccess = true,
            message = "Login ဝင်ရောက်ခြင်း အောင်မြင်ပါသည်။",
            userId = user.UserId,
            fullName = user.FullName,
            roleId = user.RoleId
        });
    }

    // ၁။ GET: User အားလုံးစာရင်းယူရန်
    [HttpGet]
    public IActionResult GetUsers()
    {
        var lst = _db.Users
            .Where(x => x.IsDelete == false)
            // Join လုပ်ရန် .Join ကို သုံးပါ
            .Join(_db.Roles,
                  user => user.RoleId,
                  role => role.RoleId,
                  (user, role) => new { user, role })
            .OrderByDescending(x => x.user.UserId)
            .Select(x => new UserModel
            {
                UserId = x.user.UserId,
                RoleId = x.user.RoleId,
                FullName = x.user.FullName,
                UserName = x.user.UserName,
                // RoleName ကို Role table ထဲမှ ဆွဲထုတ်လိုက်ခြင်း
                RoleName = x.role.RoleName,
                Password = "********",
                CreatedDateTime = x.user.CreatedDateTime
            })
            .ToList();

        return Ok(lst);
    }

    // ၂။ GET: User တစ်ဦးတည်း Profile ယူရန်
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        var item = _db.Users.FirstOrDefault(x => x.UserId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new { message = "အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        var userModel = new UserModel
        {
            UserId = item.UserId,
            RoleId = item.RoleId,
            FullName = item.FullName,
            UserName = item.UserName,
            Password = "********",
            CreatedDateTime = item.CreatedDateTime
        };

        return Ok(userModel);
    }

    // ၃။ POST: Admin မှ User အကောင့်အသစ်ဆောက်ပေးရန်
    [HttpPost]
    public IActionResult CreateUser(UserCreateRequestModel request)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = "Username နှင့် Password ဖြည့်ရန် လိုအပ်သည်။" });
        }

        string formattedUserName = request.UserName.Replace(" ", "_");

        if (!Regex.IsMatch(formattedUserName, "^[a-zA-Z0-9_]+$"))
        {
            return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = "Username တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
        }

        var isUsernameExist = _db.Users.Any(x => x.UserName == formattedUserName && x.IsDelete == false);
        if (isUsernameExist)
        {
            return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = "ဤ Username သည် စနစ်ထဲတွင် ရှိပြီးသား ဖြစ်နေသည်။" });
        }

        var passwordError = ValidatePasswordPolicy(request.Password);
        if (passwordError != null) return BadRequest(new UserCreateResponseModel { IsSuccess = false, Message = passwordError });

        // 🔒 Password အား Hash လုပ်ခြင်း
        string hashedPass = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            RoleId = request.RoleId,
            FullName = request.FullName,
            UserName = formattedUserName,
            Password = hashedPass,
            IsDelete = false,
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        };

        _db.Users.Add(newUser);
        int result = _db.SaveChanges();

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
        var item = _db.Users.FirstOrDefault(x => x.UserId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new UserUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် အသုံးပြုသူကို ရှာမတွေ့ပါ။" });
        }

        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "Username နှင့် Password ဖြည့်ရန် လိုအပ်သည်။" });
        }

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

        var passwordError = ValidatePasswordPolicy(request.Password);
        if (passwordError != null) return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = passwordError });

        item.RoleId = request.RoleId;
        item.FullName = request.FullName;
        item.UserName = formattedUserName;
        item.Password = BCrypt.Net.BCrypt.HashPassword(request.Password); //🔒 Update တွင်လည်း Hash ပြုလုပ်သိမ်းဆည်းခြင်း

        int result = _db.SaveChanges();

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
                Password = "********",
                CreatedDateTime = item.CreatedDateTime
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

        if (updateCount == 0)
        {
            return BadRequest(new UserUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် အချက်အလက်များ လိုအပ်ပါသည်။" });
        }

        int result = _db.SaveChanges();

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

        return Ok(new UserDeleteResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "အကောင့်ကို ပိတ်သိမ်း (Delete) ခြင်း အောင်မြင်ပါသည်။" : "အကောင့်ပိတ်ခြင်း မအောင်မြင်ပါ။"
        });
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