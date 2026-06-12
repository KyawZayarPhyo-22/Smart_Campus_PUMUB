using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TutorController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public TutorController(SmartCampusDbContext db)
    {
        _db = db;
    }

    // ၁။ GET: Tutor အားလုံးစာရင်းယူရန် (Profile ဓာတ်ပုံလမ်းကြောင်း ပါဝင်သည်)
    [HttpGet]
    public IActionResult GetTutors()
    {
        var rawList = (from t in _db.Tutors
                       join d in _db.Departments on t.DepartmentId equals d.DepartmentId
                       join p in _db.Positions on t.PositionId equals p.PositionId
                       join u in _db.Users on t.UserId equals u.UserId
                       where t.IsDelete == false
                       orderby t.TutorId descending
                       select new
                       {
                           t.TutorId,
                           t.TutorName,
                           t.Email,
                           t.Phone,
                           t.Profile, // Profile Column ဓာတ်ပုံလမ်းကြောင်း
                           t.About,
                           d.DepartmentName,
                           p.PositionName,
                           u.UserName,
                           t.CreatedDateTime
                       }).ToList();

        var lst = rawList.Select(t => new
        {
            tutorId = t.TutorId,
            tutorName = t.TutorName,
            email = t.Email,
            phone = t.Phone,
            profile = t.Profile,
            about = t.About,
            departmentName = t.DepartmentName,
            positionName = t.PositionName,
            accountUserName = t.UserName,
            createdDateTime = t.CreatedDateTime.HasValue ? t.CreatedDateTime.Value.AddHours(6).AddMinutes(30) : (DateTime?)null
        }).ToList();

        return Ok(lst);
    }

    // ၂။ GET: Tutor တစ်ဦးတည်း Profile ရှာရန်
    [HttpGet("{id}")]
    public IActionResult GetTutor(int id)
    {
        var item = (from t in _db.Tutors
                    join d in _db.Departments on t.DepartmentId equals d.DepartmentId
                    join p in _db.Positions on t.PositionId equals p.PositionId
                    join u in _db.Users on t.UserId equals u.UserId
                    where t.TutorId == id && t.IsDelete == false
                    select new
                    {
                        t.TutorId,
                        t.TutorName,
                        t.Email,
                        t.Phone,
                        t.Profile,
                        t.About,
                        d.DepartmentName,
                        p.PositionName,
                        u.UserName,
                        t.CreatedDateTime
                    }).FirstOrDefault();

        if (item is null)
        {
            return NotFound(new { message = "ဆရာ/မ ကို ရှာမတွေ့ပါ။" });
        }

        var result = new
        {
            tutorId = item.TutorId,
            tutorName = item.TutorName,
            email = item.Email,
            phone = item.Phone,
            profile = item.Profile,
            about = item.About,
            departmentName = item.DepartmentName,
            positionName = item.PositionName,
            accountUserName = item.UserName,
            createdDateTime = item.CreatedDateTime.HasValue ? item.CreatedDateTime.Value.AddHours(6).AddMinutes(30) : (DateTime?)null
        };

        return Ok(result);
    }

    // ၃။ POST: Tutor Profile အသစ်ထည့်ရန် (ဓာတ်ပုံဖိုင် Upload Logic ပါဝင်သည်)
    [HttpPost]
    public IActionResult CreateTutor([FromBody] TutorCreateRequestModel request) // File ပါ၍ [FromForm] သုံးရပါမည်
    {
        // --- Validation စစ်ဆေးခြင်း ---
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Phone))
        {
            return BadRequest(new TutorCreateResponseModel { IsSuccess = false, Message = "Email နှင့် ဖုန်းနံပါတ် ဖြည့်ရန် လိုအပ်သည်။" });
        }

        // User Role စစ်ဆေးခြင်း (Tutor ဟုတ်မဟုတ်)
        var userAccount = _db.Users.FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);
        if (userAccount is null || userAccount.RoleId != 3) // မင်းရဲ့ Tutor Role ID ပြောင်းလဲနိုင်သည်
        {
            return BadRequest(new TutorCreateResponseModel { IsSuccess = false, Message = "ဤ User ID သည် ဆရာ/မ (Tutor) Role မဟုတ်သဖြင့် Profile ဆောက်ခွင့်မရှိပါ။" });
        }

        // Email / Phone Format Validations
        if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") || !Regex.IsMatch(request.Phone, @"^09\d{9}$"))
        {
            return BadRequest(new TutorCreateResponseModel { IsSuccess = false, Message = "Email သို့မဟုတ် ဖုန်းနံပါတ် ပုံစံမမှန်ကန်ပါ။" });
        }

        // ဖုန်းနံပါတ်အတုများ စစ်ဆေးခြင်း
        string lastNineDigits = request.Phone.Substring(2);
        if (Regex.IsMatch(lastNineDigits, @"^(\d)\1{8}$") || lastNineDigits == "123456789")
        {
            return BadRequest(new TutorCreateResponseModel { IsSuccess = false, Message = "ဖုန်းနံပါတ်အတုများ အသုံးပြုခွင့်မရှိပါ။" });
        }

        if (_db.Tutors.Any(x => (x.Email == request.Email || x.Phone == request.Phone) && x.IsDelete == false))
        {
            return BadRequest(new TutorCreateResponseModel { IsSuccess = false, Message = "Email သို့မဟုတ် ဖုန်းနံပါတ်သည် စနစ်ထဲတွင် ရှိပြီးသား ဖြစ်နေသည်။" });
        }

        // --- 📷 ဓာတ်ပုံဖိုင် သိမ်းဆည်းခြင်း Logic ---
        string? dbProfilePath = null;

        if (request.PhotoFile != null && request.PhotoFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(request.PhotoFile.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new TutorCreateResponseModel { IsSuccess = false, Message = "JPG, JPEG, PNG ပုံများသာ လက်ခံပါသည်။" });
            }

            string fileName = "tutor_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string fullPath = Path.Combine(uploadFolder, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                request.PhotoFile.CopyTo(stream);
            }

            dbProfilePath = "/uploads/" + fileName; // မင်းရဲ့ Profile Column ထဲဝင်မည့် လမ်းကြောင်း
        }

        // --- DB ထဲသို့ ဒေတာသွင်းခြင်း ---
        var newTutor = new Tutor
        {
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            UserId = request.UserId,
            TutorName = request.TutorName,
            Email = request.Email,
            Phone = request.Phone,
            Profile = dbProfilePath, // 📷 Profile ထဲ လမ်းကြောင်းသိမ်းသည်
            About = request.About,
            IsDelete = false
            // CreatedDateTime ကို DB ဘက်က DEFAULT (getdate()) နဲ့ Auto ထည့်ပေးပါမည်
        };

        _db.Tutors.Add(newTutor);
        int result = _db.SaveChanges();

        return StatusCode(201, new TutorCreateResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ဆရာ/မ Profile အသစ်ထည့်ခြင်း အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
        });
    }

    // ၄။ PUT: Tutor Profile ပြင်ရန် (ဓာတ်ပုံပါ လဲလှယ်နိုင်သည်)
    [HttpPut("{id}")]
    public IActionResult UpdateTutor(int id, [FromForm] TutorUpdateRequestModel request)
    {
        var item = _db.Tutors.FirstOrDefault(x => x.TutorId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new TutorUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ဆရာ/မ Profile ကို ရှာမတွေ့ပါ။" });
        }

        // Role Validation
        var userAccount = _db.Users.FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);
        if (userAccount == null || userAccount.RoleId != 3)
        {
            return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "သတ်မှတ်ပေးသော User အကောင့်သည် ဆရာ/မ Role မဟုတ်ပါ။" });
        }

        // ပုံအသစ် ထပ်တင်လာခဲ့လျှင်
        if (request.PhotoFile != null && request.PhotoFile.Length > 0)
        {
            var extension = Path.GetExtension(request.PhotoFile.FileName).ToLower();
            string fileName = "tutor_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            string fullPath = Path.Combine(uploadFolder, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                request.PhotoFile.CopyTo(stream);
            }

            item.Profile = "/uploads/" + fileName; // 📷 ပုံအသစ်လမ်းကြောင်းဖြင့် အစားထိုးသည်
        }

        item.DepartmentId = request.DepartmentId;
        item.PositionId = request.PositionId;
        item.UserId = request.UserId;
        item.TutorName = request.TutorName;
        item.Email = request.Email;
        item.Phone = request.Phone;
        item.About = request.About;

        int result = _db.SaveChanges();
        return Ok(new TutorUpdateResponseModel { IsSuccess = result > 0, Message = "ပြင်ဆင်မှု အောင်မြင်ပါသည်" });
    }
    // ၅။ PATCH: Tutor Profile တစ်စိတ်တစ်ပိုင်းစီ လိုက်ပြင်ရန် (ဓာတ်ပုံ သို့မဟုတ် အချက်အလက် သီးသန့်ပြင်နိုင်သည်)
    [HttpPatch("{id}")]
    public IActionResult PatchTutor(int id, [FromForm] TutorUpdateRequestModel request) // ဖိုင်ပါဝင်နိုင်၍ [FromForm] ကိုသုံးသည်
    {
        // အရင်ဦးဆုံး ပြင်မည့် Tutor ရှိမရှိ ရှာမည်
        var item = _db.Tutors.FirstOrDefault(x => x.TutorId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new TutorUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ဆရာ/မ Profile ကို ရှာမတွေ့ပါ။" });
        }

        int updateCount = 0; // ဘာတွေ ပြင်လိုက်လဲဆိုတာ မှတ်ရန် Count

        // --- (က) နိုင်ငံခြားကီး (Foreign Keys) များ ပါလာလျှင် စစ်ဆေးပြီး ပြင်မည် ---
        if (request.DepartmentId > 0)
        {
            item.DepartmentId = request.DepartmentId;
            updateCount++;
        }

        if (request.PositionId > 0)
        {
            item.PositionId = request.PositionId;
            updateCount++;
        }

        if (request.UserId > 0)
        {
            // UserId ပါလာခဲ့လျှင် အဲ့ဒီ User သည် တကယ်ရှိပြီး Tutor Role ဟုတ်မဟုတ် စစ်ဆေးမည်
            var userAccount = _db.Users.FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);
            if (userAccount == null || userAccount.RoleId != 3) // 3 = Tutor Role ID
            {
                return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "လွှဲပြောင်းပေးမည့် User ID သည် ဆရာ/မ (Tutor) Role မဟုတ်သဖြင့် ပြင်ဆင်ခွင့်မရှိပါ။" });
            }
            item.UserId = request.UserId;
            updateCount++;
        }

        // --- (ခ) စာသားအချက်အလက်များ ပါလာလျှင် ပြင်မည် ---
        if (!string.IsNullOrEmpty(request.TutorName))
        {
            item.TutorName = request.TutorName;
            updateCount++;
        }

        if (!string.IsNullOrEmpty(request.About))
        {
            item.About = request.About;
            updateCount++;
        }

        // --- (ဂ) Email ပါလာလျှင် Format နှင့် Duplicate စစ်ဆေးပြီးမှ ပြင်မည် ---
        if (!string.IsNullOrEmpty(request.Email))
        {
            if (!Regex.IsMatch(request.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "Email ပုံစံ (Format) မမှန်ကန်ပါ။" });
            }

            // ကိုယ့် ID မဟုတ်တဲ့ အခြားသူတွေထဲမှာ ဒီ Email သုံးထားတာ ရှိလား စစ်ခြင်း
            var isEmailExist = _db.Tutors.Any(x => x.Email == request.Email && x.TutorId != id && x.IsDelete == false);
            if (isEmailExist)
            {
                return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "ဤ Email ကို အခြားသူတစ်ယောက် အသုံးပြုထားပြီးသား ဖြစ်သည်။" });
            }

            item.Email = request.Email;
            updateCount++;
        }

        // --- (ဃ) Phone ပါလာလျှင် မြန်မာဖုန်း Format နှင့် အတုအယောင် စစ်ဆေးပြီးမှ ပြင်မည် ---
        if (!string.IsNullOrEmpty(request.Phone))
        {
            if (!Regex.IsMatch(request.Phone, @"^09\d{9}$"))
            {
                return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "ဖုန်းနံပါတ်သည် 09 ဖြင့်စပြီး ဂဏန်းစုစုပေါင်း ၁၁ လုံး ဖြစ်ရပါမည်။" });
            }

            string lastNineDigits = request.Phone.Substring(2);
            if (Regex.IsMatch(lastNineDigits, @"^(\d)\1{8}$") || lastNineDigits == "123456789")
            {
                return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "ဖုန်းနံပါတ်အတုများ အသုံးပြုခွင့်မရှိပါ။" });
            }

            var isPhoneExist = _db.Tutors.Any(x => x.Phone == request.Phone && x.TutorId != id && x.IsDelete == false);
            if (isPhoneExist)
            {
                return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "ဤ ဖုန်းနံပါတ်ကို အခြားသူတစ်ယောက် အသုံးပြုထားပြီးသား ဖြစ်သည်။" });
            }

            item.Phone = request.Phone;
            updateCount++;
        }

        // --- (င) 📷 ဓာတ်ပုံဖိုင်အသစ် သီးသန့် တင်လာခဲ့လျှင် ၎င်းတစ်ခုတည်းကို ပြင်ဆင်မည် ---
        if (request.PhotoFile != null && request.PhotoFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(request.PhotoFile.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "JPG, JPEG, PNG ဓာတ်ပုံများသာ လက်ခံပါသည်။" });
            }

            string fileName = "tutor_patch_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string fullPath = Path.Combine(uploadFolder, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                request.PhotoFile.CopyTo(stream);
            }

            item.Profile = "/uploads/" + fileName; // ပုံလမ်းကြောင်းကို အသစ်လဲလိုက်သည်
            updateCount++;
        }

        // ပြင်စရာ ဘာမှ ပို့မလာခဲ့ရင် ပယ်ချမည်
        if (updateCount == 0)
        {
            return BadRequest(new TutorUpdateResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် အချက်အလက် လုံးဝ ပို့မလာခဲ့ပါ။" });
        }

        // ပြောင်းလဲမှုများကို သိမ်းဆည်းခြင်း
        int result = _db.SaveChanges();

        return Ok(new TutorUpdateResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ဆရာ/မ Profile ကို ကွက်တိ တစ်စိတ်တစ်ပိုင်း ပြင်ဆင်ပြီးပါပြီ။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ သို့မဟုတ် ပြောင်းလဲမှုမရှိပါ။"
        });
    }


    // ၅။ DELETE: Tutor Profile ဖြုတ်ရန် (Soft Delete)
    [HttpDelete("{id}")]
    public IActionResult DeleteTutor(int id)
    {
        var item = _db.Tutors.FirstOrDefault(x => x.TutorId == id && x.IsDelete == false);
        if (item is null)
        {
            return NotFound(new TutorDeleteResponseModel { IsSuccess = false, Message = "ဖျက်မည့် ဆရာ/မ Profile ကို ရှာမတွေ့ပါ။" });
        }

        item.IsDelete = true;
        int result = _db.SaveChanges();

        return Ok(new TutorDeleteResponseModel { IsSuccess = result > 0, Message = "စနစ်ထဲမှ ဖယ်ရှားပြီးပါပြီ။" });
    }
}