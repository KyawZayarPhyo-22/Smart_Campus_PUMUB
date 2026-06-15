using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Smart_Campus_PUMUB.Database.AppDbContext; // မင်းရဲ့ AppDbContext တည်နေရာ
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public StudentController(SmartCampusDbContext db)
    {
        _db = db;
    }

    // 🎯 ၁။ GET: api/student (ကျောင်းသားအားလုံးစာရင်း - Soft Delete မဖြစ်သေးတာပဲပြမည်)
    [HttpGet]
    public IActionResult GetStudents()
    {
        var lst = _db.Students
        .Include(x => x.User)
            .Where(x => x.IsDelete == false || x.IsDelete == null)
            .OrderByDescending(s => s.StudentId)
            .Select(s => new StudentModel
            {
                StudentId = s.StudentId,
                UserId = s.UserId,
                CurrentClassYear = s.CurrentClassYear,
                CurrentMajor = s.CurrentMajor,
                CurrentRollNo = s.CurrentRollNo,
                UserName = s.User.UserName,
                Status = s.Status ?? "Active"
            })
            .ToList();

        return Ok(lst);
    }

    // 🎯 ၂။ GET: api/student/{id} (ကျောင်းသားတစ်ဦးချင်းစီ၏ အသေးစိတ်ကြည့်ရန်)
    [HttpGet("{id}")]
    public IActionResult GetStudent(int id)
    {
        var item = _db.Students.FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ကျောင်းသားကို ရှာမတွေ့ပါ။" });
        }

        var data = new StudentModel
        {
            StudentId = item.StudentId,
            UserId = item.UserId,
            CurrentClassYear = item.CurrentClassYear,
            CurrentMajor = item.CurrentMajor,
            CurrentRollNo = item.CurrentRollNo,
            Status = item.Status ?? "Active"
        };

        return Ok(data);
    }

    // 🎯 ၃။ POST: api/student (ကျောင်းသားအသစ် စာရင်းသွင်းရန် - Validation ပါဝင်သည်)
    [HttpPost]
    public IActionResult CreateStudent(StudentCreateRequestModel request)
    {
        // Validation: မဖြစ်မနေလိုအပ်သော အချက်အလက်များ ရှိမရှိ စစ်ဆေးခြင်း
        if (request.UserId <= 0 || string.IsNullOrEmpty(request.CurrentClassYear) || string.IsNullOrEmpty(request.CurrentMajor))
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "လိုအပ်သော ဒေတာများကို ပြည့်စုံစွာ ဖြည့်သွင်းပါ။" });
        }

        // Validation: Roll No ပုံစံစစ်ဆေးခြင်း (MUB-1098 ကဲ့သို့ စာလုံး၊ ဂဏန်း နှင့် ဒက်ရှ် '-' သာ ခွင့်ပြုမည်)
        if (!string.IsNullOrEmpty(request.CurrentRollNo))
        {
            // Regex ရှင်းလင်းချက်: ^[a-zA-Z0-9-]+$ ဆိုသည်မှာ အင်္ဂလိပ်စာလုံး၊ ဂဏန်း နှင့် ဒက်ရှ်(-) မှလွဲ၍ အခြား သင်္ကေတများ မပါရဟု တားမြစ်ခြင်းဖြစ်သည်
            if (!Regex.IsMatch(request.CurrentRollNo, "^[a-zA-Z0-9-]+$"))
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ခုံအမှတ် (Roll No) တွင် '-' မှလွဲ၍ အခြား အထူးသင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
            }

            // ခုံအမှတ် ထပ်နေခြင်း ရှိမရှိ စစ်ဆေးခြင်း
            var isRollNoExist = _db.Students.Any(x => x.CurrentRollNo == request.CurrentRollNo && (x.IsDelete == false || x.IsDelete == null));
            if (isRollNoExist)
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤ ခုံအမှတ် (Roll No) သည် စနစ်ထဲတွင် ရှိနှင့်ပြီးသား ဖြစ်ပါသည်။" });
            }
        }

        // ကျောင်းသားအကောင့် (User_Id) အစစ်အမှန် ရှိမရှိ နှင့် ၎င်းသည် Student Role (Role_id = 4) ဟုတ်မဟုတ် စစ်ဆေးခြင်း
        var userCheck = _db.Users.FirstOrDefault(u => u.UserId == request.UserId && u.IsDelete == false);
        if (userCheck is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ဤအသုံးပြုသူအကောင့် (UserId) ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
        }
        if (userCheck.RoleId != 4)
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ကျောင်းသားအကောင့် (Student Role) ဖြစ်မှသာ ကျောင်းသားစာရင်း သွင်းခွင့်ရှိသည်။" });
        }

        // ဤ User သည် ကျောင်းသားစာရင်းထဲတွင် အသက်ဝင်လျက် ရှိပြီးသားဖြစ်နေပါက ထပ်မံ ထည့်ခွင့်မပြုရန် တားဆီးခြင်း
        var isAlreadyStudent = _db.Students.Any(x => x.UserId == request.UserId && (x.IsDelete == false || x.IsDelete == null));
        if (isAlreadyStudent)
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤအသုံးပြုသူသည် ကျောင်းသားစာရင်းထဲတွင် ရှိပြီးသား ဖြစ်နေပါသည်။" });
        }

        // DB ထဲသို့ အသစ်ထည့်သွင်းခြင်း
        var newStudent = new Student
        {
            UserId = request.UserId,
            CurrentClassYear = request.CurrentClassYear,
            CurrentMajor = request.CurrentMajor,
            CurrentRollNo = request.CurrentRollNo?.ToUpper(), // စာလုံးအကြီးဖြင့် သိမ်းဆည်းရန်
            Status = "Active",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            IsDelete = false
        };

        // Note: If entity class name is TblStudent, change here
        _db.Students.Add(newStudent);
        int result = _db.SaveChanges();

        return StatusCode(201, new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားအဖြစ် စာရင်းသွင်းခြင်း အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
        });
    }

    // 🎯 ၄။ PUT: api/student/{id} (ကျောင်းသား အတန်းတက်ခြင်း/ခုံအမှတ် ပြောင်းခြင်း ပြင်ရန်)
    [HttpPut("{id}")]
    public IActionResult UpdateStudent(int id, StudentUpdateRequestModel request)
    {
        var item = _db.Students.FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ကျောင်းသားမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        if (string.IsNullOrEmpty(request.CurrentClassYear) || string.IsNullOrEmpty(request.CurrentMajor))
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "အတန်း နှင့် Major ဖြည့်သွင်းရန် လိုအပ်သည်။" });
        }

        // Roll No ရိုက်ထားလျှင် Special Character ပါမပါ ထပ်မံစစ်ဆေးခြင်း
        if (!string.IsNullOrEmpty(request.CurrentRollNo))
        {
            if (!Regex.IsMatch(request.CurrentRollNo, "^[a-zA-Z0-9-]+$"))
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ခုံအမှတ် (Roll No) တွင် '-' မှလွဲ၍ အခြား အထူးသင်္ကေတများ မသုံးရပါ။" });
            }

            // မိမိမှအပ အခြားသူတစ်ယောက်က ဤခုံအမှတ်ကို သုံးထားခြင်း ရှိမရှိ စစ်ဆေးခြင်း
            var isRollNoDuplicate = _db.Students.Any(x => x.CurrentRollNo == request.CurrentRollNo && x.StudentId != id && (x.IsDelete == false || x.IsDelete == null));
            if (isRollNoDuplicate)
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤ ခုံအမှတ် (Roll No) ကို အခြားကျောင်းသားတစ်ဦး အသုံးပြုထားပြီး ဖြစ်ပါသည်။" });
            }
        }

        // Data များ အစားထိုး ပြင်ဆင်ခြင်း
        item.CurrentClassYear = request.CurrentClassYear;
        item.CurrentMajor = request.CurrentMajor;
        item.CurrentRollNo = request.CurrentRollNo?.ToUpper();
        item.Status = request.Status;

        // item.UserName = request.User.UserName,
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        return Ok(new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားအချက်အလက် ပြင်ဆင်ခြင်း အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
            Data = new StudentModel
            {
                StudentId = item.StudentId,
                UserId = item.UserId,
                CurrentClassYear = item.CurrentClassYear,
                CurrentMajor = item.CurrentMajor,
                CurrentRollNo = item.CurrentRollNo,
                Status = item.Status ?? "Active"
            }
        });
    }
    [HttpPatch("{id}")]
    public IActionResult PatchStudent(int id, StudentPatchRequestModel request)
    {
        // ၁။ ပြင်ဆင်မည့် ကျောင်းသား ရှိမရှိ အရင်စစ်မည်
        var item = _db.Students.FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ကျောင်းသားမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        int updateCount = 0;

        // ၂။ အတန်း (Class Year) ပါလာလျှင် ပြင်မည်
        if (!string.IsNullOrEmpty(request.CurrentClassYear))
        {
            item.CurrentClassYear = request.CurrentClassYear;
            updateCount++;
        }

        // ၃။ မေဂျာ (Major) ပါလာလျှင် ပြင်မည်
        if (!string.IsNullOrEmpty(request.CurrentMajor))
        {
            item.CurrentMajor = request.CurrentMajor;
            updateCount++;
        }

        // ၄။ ခုံအမှတ် (Roll No) ပါလာလျှင် Validation စစ်ပြီးမှ ပြင်မည်
        if (!string.IsNullOrEmpty(request.CurrentRollNo))
        {
            // Regex စစ်ဆေးခြင်း: ဒက်ရှ် (-) မှလွဲ၍ အခြား အထူးသင်္ကေတများ မပါရ
            if (!Regex.IsMatch(request.CurrentRollNo, "^[a-zA-Z0-9-]+$"))
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ခုံအမှတ် (Roll No) တွင် '-' မှလွဲ၍ အခြား အထူးသင်္ကေတများ မသုံးရပါ။" });
            }

            // မိမိမှအပ အခြားကျောင်းသားတစ်ဦးဦးက ဤခုံအမှတ်ကို သုံးထားခြင်း ရှိမရှိ စစ်ဆေးခြင်း
            var isRollNoDuplicate = _db.Students.Any(x => x.CurrentRollNo == request.CurrentRollNo && x.StudentId != id && (x.IsDelete == false || x.IsDelete == null));
            if (isRollNoDuplicate)
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤ ခုံအမှတ် (Roll No) ကို အခြားကျောင်းသားတစ်ဦး အသုံးပြုထားပြီး ဖြစ်ပါသည်။" });
            }

            item.CurrentRollNo = request.CurrentRollNo.ToUpper(); // စာလုံးအကြီးပြောင်းသိမ်းမည်
            updateCount++;
        }

        // ၅။ အခြေအနေ (Status) ပါလာလျှင် ပြင်မည် (ဥပမာ - Active, Absent, Dropped)
        if (!string.IsNullOrEmpty(request.Status))
        {
            item.Status = request.Status;
            updateCount++;
        }

        // ၆။ ဘာအချက်အလက်မှ ပြောင်းလဲလာခြင်း မရှိလျှင် BadRequest ပြန်မည်
        if (updateCount == 0)
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် အချက်အလက်များ လိုအပ်ပါသည်။" });
        }

        // ပြင်ဆင်သည့် အချိန်ကို မှတ်သားမည်
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        return Ok(new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားအချက်အလက်များကို တစ်စိတ်တစ်ပိုင်း ပြင်ဆင်ခြင်း အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
            Data = new StudentModel
            {
                StudentId = item.StudentId,
                UserId = item.UserId,
                CurrentClassYear = item.CurrentClassYear,
                CurrentMajor = item.CurrentMajor,
                CurrentRollNo = item.CurrentRollNo,
                Status = item.Status ?? "Active"
            }
        });
    }

    // 🎯 ၅။ DELETE: api/student/{id} (ကျောင်းသားအဖြစ်မှ ရပ်စဲရန် - Soft Delete)
    [HttpDelete("{id}")]
    public IActionResult DeleteStudent(int id)
    {
        var item = _db.Students.FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ဖျက်သိမ်းမည့် ကျောင်းသားမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        // ဒေတာအပြီးမဖျက်ဘဲ Soft Delete ပြုလုပ်ပြီး Status ကိုပါ Expired/Dropped သတ်မှတ်ခြင်း
        item.IsDelete = true;
        item.Status = "Deleted";
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        return Ok(new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားမှတ်တမ်းကို ပယ်ဖျက်ခြင်း အောင်မြင်ပါသည်။" : "ပယ်ဖျက်ခြင်း မအောင်မြင်ပါ။"
        });
    }
}