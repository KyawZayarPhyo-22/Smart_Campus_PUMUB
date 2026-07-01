using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext; // မင်းရဲ့ AppDbContext တည်နေရာ
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public StudentController(SmartCampusDbContext db)
    {
        _db = db;
    }

    // 🎯 ၁။ GET: api/student (ကျောင်းသားအားလုံးစာရင်း - Soft Delete မဖြစ်သေးတာပဲပြမည်)
    [HttpGet]
    [Authorize(Roles = "Admin,admin")]
    public IActionResult GetStudents()
    {
        // 1. Get all active users who are students
        var studentUsers = _db.Users
            .Where(u => u.RoleId == 3 && (u.IsDelete == false || u.IsDelete == null))
            .ToList();

        // 2. Check if they have a Student record. If not, create one.
        bool hasNewStudents = false;
        foreach (var user in studentUsers)
        {
            var studentExists = _db.Students.Any(s => s.UserId == user.UserId);
            if (!studentExists)
            {
                var newStudent = new Student
                {
                    UserId = user.UserId,
                    CurrentClassYear = "First Year", // default
                    CurrentMajor = "N/A", // default
                    Status = "Active",
                    CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                    IsDelete = false
                };
                _db.Students.Add(newStudent);
                hasNewStudents = true;
            }
        }

        if (hasNewStudents)
        {
            _db.SaveChanges();
        }

        var lst = _db.Students
            .Include(s => s.User)
            .Where(x => (x.IsDelete == false || x.IsDelete == null) && x.User.RoleId == 3 && (x.User.IsDelete == false || x.User.IsDelete == null))
            .OrderByDescending(s => s.StudentId)
            .Select(s => new StudentModel
            {
                StudentId = s.StudentId,
                UserId = s.UserId,
                FullName = s.User.FullName,
                CurrentClassYear = s.CurrentClassYear,
                CurrentMajor = s.CurrentMajor,
                CurrentRollNo = s.User.RoleNo,
                Status = s.Status ?? "Active",
                Sem1_Result = s.Sem1_Result,
                Sem2_Result = s.Sem2_Result,
                Sem3_Result = s.Sem3_Result,
                Sem4_Result = s.Sem4_Result,
                Sem5_Result = s.Sem5_Result,
                Sem6_Result = s.Sem6_Result,
                Sem7_Result = s.Sem7_Result,
                Sem8_Result = s.Sem8_Result,
                Sem9_Result = s.Sem9_Result
            })
            .ToList();

        return Ok(lst);
    }

    // 🎯 ၂။ GET: api/student/{id} (ကျောင်းသားတစ်ဦးချင်းစီ၏ အသေးစိတ်ကြည့်ရန်)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,admin")]
    public IActionResult GetStudent(int id)
    {
        var item = _db.Students.Include(s => s.User).FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ကျောင်းသားကို ရှာမတွေ့ပါ။" });
        }

        var data = new StudentModel
        {
            StudentId = item.StudentId,
            UserId = item.UserId,
            FullName = item.User?.FullName,
            CurrentClassYear = item.CurrentClassYear,
            CurrentMajor = item.CurrentMajor,
            CurrentRollNo = item.User != null ? item.User.RoleNo : item.CurrentRollNo,
            Status = item.Status ?? "Active",
            Sem1_Result = item.Sem1_Result,
            Sem2_Result = item.Sem2_Result,
            Sem3_Result = item.Sem3_Result,
            Sem4_Result = item.Sem4_Result,
            Sem5_Result = item.Sem5_Result,
            Sem6_Result = item.Sem6_Result,
            Sem7_Result = item.Sem7_Result,
            Sem8_Result = item.Sem8_Result,
            Sem9_Result = item.Sem9_Result
        };

        return Ok(data);
    }

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "Admin,admin,Student,student")]
    public IActionResult GetStudentByUserId(int userId)
    {
        var userCheck = _db.Users.FirstOrDefault(u => u.UserId == userId && u.RoleId == 3 && (u.IsDelete == false || u.IsDelete == null));
        if (userCheck is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ကျောင်းသားအကောင့်ကို ရှာမတွေ့ပါ။" });
        }

        var item = _db.Students.Include(s => s.User).FirstOrDefault(x => x.UserId == userId && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            // Create on the fly
            var newStudent = new Student
            {
                UserId = userId,
                CurrentClassYear = "First Year",
                CurrentMajor = "N/A",
                Status = "Active",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                IsDelete = false
            };
            _db.Students.Add(newStudent);
            _db.SaveChanges();

            item = _db.Students.Include(s => s.User).FirstOrDefault(x => x.UserId == userId && (x.IsDelete == false || x.IsDelete == null));
        }

        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ကျောင်းသားမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        var data = new StudentModel
        {
            StudentId = item.StudentId,
            UserId = item.UserId,
            FullName = item.User?.FullName,
            CurrentClassYear = item.CurrentClassYear,
            CurrentMajor = item.CurrentMajor,
            CurrentRollNo = item.User != null ? item.User.RoleNo : item.CurrentRollNo,
            Status = item.Status ?? "Active",
            Sem1_Result = item.Sem1_Result,
            Sem2_Result = item.Sem2_Result,
            Sem3_Result = item.Sem3_Result,
            Sem4_Result = item.Sem4_Result,
            Sem5_Result = item.Sem5_Result,
            Sem6_Result = item.Sem6_Result,
            Sem7_Result = item.Sem7_Result,
            Sem8_Result = item.Sem8_Result,
            Sem9_Result = item.Sem9_Result
        };

        return Ok(data);
    }

    // 🎯 ၃။ POST: api/student (ကျောင်းသားအသစ် စာရင်းသွင်းရန် - Validation ပါဝင်သည်)
    [HttpPost]
    [Authorize(Roles = "Admin,admin")]
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
        if (userCheck.RoleId != 3)
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

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "New Student Registered",
            Description = $"{request.Name} was added to the System.",
            CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
        });
        _db.SaveChanges();

        return StatusCode(201, new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားအဖြစ် စာရင်းသွင်းခြင်း အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
        });
    }

    // 🎯 ၄။ PUT: api/student/{id} (ကျောင်းသား အတန်းတက်ခြင်း/ခုံအမှတ် ပြောင်းခြင်း ပြင်ရန်)
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,admin")]
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
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = " Student Updated",
            Description = $"{request.CurrentClassYear} {request.CurrentMajor} was updated to the System.",
            CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
        });
        _db.SaveChanges();

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
    [Authorize(Roles = "Admin,admin")]
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

        // ၆။ Semester Results
        if (request.Sem1_Result != null) { item.Sem1_Result = request.Sem1_Result == "None" ? null : request.Sem1_Result; updateCount++; }
        if (request.Sem2_Result != null) { item.Sem2_Result = request.Sem2_Result == "None" ? null : request.Sem2_Result; updateCount++; }
        if (request.Sem3_Result != null) { item.Sem3_Result = request.Sem3_Result == "None" ? null : request.Sem3_Result; updateCount++; }
        if (request.Sem4_Result != null) { item.Sem4_Result = request.Sem4_Result == "None" ? null : request.Sem4_Result; updateCount++; }
        if (request.Sem5_Result != null) { item.Sem5_Result = request.Sem5_Result == "None" ? null : request.Sem5_Result; updateCount++; }
        if (request.Sem6_Result != null) { item.Sem6_Result = request.Sem6_Result == "None" ? null : request.Sem6_Result; updateCount++; }
        if (request.Sem7_Result != null) { item.Sem7_Result = request.Sem7_Result == "None" ? null : request.Sem7_Result; updateCount++; }
        if (request.Sem8_Result != null) { item.Sem8_Result = request.Sem8_Result == "None" ? null : request.Sem8_Result; updateCount++; }
        if (request.Sem9_Result != null) { item.Sem9_Result = request.Sem9_Result == "None" ? null : request.Sem9_Result; updateCount++; }

        // ၇။ ဘာအချက်အလက်မှ ပြောင်းလဲလာခြင်း မရှိလျှင် BadRequest ပြန်မည်
        if (updateCount == 0)
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် အချက်အလက်များ လိုအပ်ပါသည်။" });
        }

        // ပြင်ဆင်သည့် အချိန်ကို မှတ်သားမည်
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = " Student Updated",
            Description = $"{request.CurrentClassYear} {request.CurrentMajor} was updated to the System.",
            CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
        });
        _db.SaveChanges();

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
                Status = item.Status ?? "Active",
                Sem1_Result = item.Sem1_Result,
                Sem2_Result = item.Sem2_Result,
                Sem3_Result = item.Sem3_Result,
                Sem4_Result = item.Sem4_Result,
                Sem5_Result = item.Sem5_Result,
                Sem6_Result = item.Sem6_Result,
                Sem7_Result = item.Sem7_Result,
                Sem8_Result = item.Sem8_Result,
                Sem9_Result = item.Sem9_Result
            }
        });
    }

    // 🎯 ၅။ DELETE: api/student/{id} (ကျောင်းသားအဖြစ်မှ ရပ်စဲရန် - Soft Delete)
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,admin")]
    public IActionResult DeleteStudent(int id)
    {
        var item = _db.Students.FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ဖျက်သိမ်းမည့် ကျောင်းသားမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        item.IsDelete = true;
        item.Status = "Dropped";
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "Student deleted",
            Description = $"{item.CurrentClassYear} {item.CurrentMajor} was deleted to the System.",
            CreatedDateTime = DateTime.UtcNow
        });
        _db.SaveChanges();

        return Ok(new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားမှတ်တမ်းကို ပယ်ဖျက်ခြင်း အောင်မြင်ပါသည်။" : "ပယ်ဖျက်ခြင်း မအောင်မြင်ပါ။"
        });
    }

    // 🎯 ၆။ GET: api/student/count/active (တက်ကြွဆဲ ကျောင်းသားအရေအတွက်)
    [HttpGet("count/active")]
    [Authorize(Roles = "Admin,admin")]
    public IActionResult GetActiveStudentCount()
    {
        var count = _db.Users.Count(u => u.RoleId == 3 && (u.IsDelete == false || u.IsDelete == null));
        return Ok(new { Count = count });
    }

    // 🎯 ၇။ GET: api/student/profile/{userId} - ကျောင်းသား Profile ကြည့်ရန်
    [HttpGet("profile/{userId}")]
    [Authorize(Roles = "Admin,admin,Student,student")]
    public IActionResult GetStudentProfile(int userId)
    {
        var student = _db.Students
            .Include(s => s.User)
            .FirstOrDefault(s => s.UserId == userId && (s.IsDelete == false || s.IsDelete == null));

        if (student == null)
            return NotFound(new { IsSuccess = false, Message = "ကျောင်းသားကို ရှာမတွေ့ပါ။" });

        var reg = _db.StudentRegistrations
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null))
            .OrderByDescending(r => r.RegistrationId)
            .FirstOrDefault();

        var notifications = _db.StudentRegistrations
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null))
            .Select(r => new
            {
                r.RegistrationId,
                r.Status,
                r.AcademicYearRange,
                r.AcademicYearLevel,
                r.CreatedDatetime,
                Payments = r.RegistrationPayments
                    .Where(p => p.IsDelete == false || p.IsDelete == null)
                    .Select(p => new { p.PaymentId, p.Status, p.CreatedDateTime, p.AmountPaid })
                    .ToList()
            })
            .ToList();

        return Ok(new
        {
            StudentId     = student.StudentId,
            UserId        = student.UserId,
            FullName      = student.User?.FullName,
            UserName      = student.User?.UserName,
            RollNo        = student.User?.RoleNo ?? student.CurrentRollNo,
            CurrentClassYear = string.IsNullOrWhiteSpace(student.CurrentClassYear) || student.CurrentClassYear == "N/A" ? reg?.AcademicYearLevel ?? "N/A" : student.CurrentClassYear,
            CurrentSemester = reg?.AcademicYearLevel,
            CurrentMajor  = string.IsNullOrWhiteSpace(student.CurrentMajor) || student.CurrentMajor == "N/A" ? reg?.Major ?? "N/A" : student.CurrentMajor,
            Status        = student.Status,
            Dob           = reg?.Dob,
            Email         = reg?.Email,
            Phone         = (string?)null,
            StudentImage  = reg?.StudentImage,
            Sem1_Result   = student.Sem1_Result,
            Sem2_Result   = student.Sem2_Result,
            Sem3_Result   = student.Sem3_Result,
            Sem4_Result   = student.Sem4_Result,
            Sem5_Result   = student.Sem5_Result,
            Sem6_Result   = student.Sem6_Result,
            Sem7_Result   = student.Sem7_Result,
            Sem8_Result   = student.Sem8_Result,
            Sem9_Result   = student.Sem9_Result,
            Registrations = notifications
        });
    }

    // 🎯 ၈။ PUT: api/student/profile/{userId}/image - Profile ဓာတ်ပုံ ပြောင်းရန်
    [HttpPut("profile/{userId}/image")]
    [Authorize(Roles = "Admin,admin,Student,student")]
    public IActionResult UpdateStudentProfileImage(int userId, [FromBody] StudentProfileImageRequest request)
    {
        var reg = _db.StudentRegistrations
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null))
            .OrderByDescending(r => r.RegistrationId)
            .FirstOrDefault();

        if (reg == null)
            return NotFound(new { IsSuccess = false, Message = "ကျောင်းအပ်နှံမှု မှတ်တမ်းကို ရှာမတွေ့ပါ။" });

        reg.StudentImage = request.ImageBase64;
        reg.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
        _db.SaveChanges();

        return Ok(new { IsSuccess = true, Message = "Profile ဓာတ်ပုံ ပြောင်းလဲခြင်း အောင်မြင်ပါသည်။" });
    }
}

public class StudentProfileImageRequest
{
    public string? ImageBase64 { get; set; }
}
