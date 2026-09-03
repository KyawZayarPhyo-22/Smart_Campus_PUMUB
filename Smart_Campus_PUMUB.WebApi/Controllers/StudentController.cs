using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext; // AppDbContext namespace
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Smart_Campus_PUMUB.WebApi.Services;

namespace Smart_Campus_PUMUB.WebApi.Controllers;


[ApiController]
[Route("api/[controller]")]
public class StudentController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly IFacultyDataScopeService _scopeService;
    private readonly IEnrollmentService _enrollmentService;

    public StudentController(SmartCampusDbContext db, IFacultyDataScopeService scopeService, IEnrollmentService enrollmentService)
    {
        _db = db;
        _scopeService = scopeService;
        _enrollmentService = enrollmentService;
    }

    // GET: api/student (Get active students)
    [HttpGet]
    [Permission("Student.View")]
    public IActionResult GetStudents([FromQuery] int? facultyId = null)
    {
        // Hierarchical RBAC Data Scoping:
        if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
        {
            var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
            if (scopedFacultyId.HasValue)
            {
                facultyId = scopedFacultyId.Value;
            }
        }

        // 1. Get all active users who are students
          var studentUsersQuery = _db.Users
            .Where(u => u.RoleId == 3 && (u.IsDelete == false || u.IsDelete == null));

        // Faculty-based scoping: if facultyId provided, filter users by faculty
        if (facultyId.HasValue && facultyId.Value > 0)
        {
            studentUsersQuery = studentUsersQuery.Where(u => u.FacultyId == facultyId.Value);
        }

        var studentUsers = studentUsersQuery.ToList();

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
                    CurrentClassYear = "", // default
                    CurrentMajor = "", // default
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

        // 3. Get all active Majors for the Faculty join lookup
        var majors = _db.Majors
            .Include(m => m.Faculty)
            .Where(m => m.IsDelete == false || m.IsDelete == null)
            .ToList();

        var studentsQuery = _db.Students
            .Include(s => s.User)
                .ThenInclude(u => u.Faculty)
            .Where(x => (x.IsDelete == false || x.IsDelete == null) && x.User.RoleId == 3 && (x.User.IsDelete == false || x.User.IsDelete == null));

        // Apply faculty filter at the DB query level
        if (facultyId.HasValue && facultyId.Value > 0)
        {
            studentsQuery = studentsQuery.Where(x => x.User.FacultyId == facultyId.Value);
        }

        var studentsList = studentsQuery
            .OrderByDescending(s => s.StudentId)
            .ToList();

        var userIds = studentsList.Select(s => s.UserId).Distinct().ToList();

        var latestRegs = _db.StudentRegistrations
            .Where(r => r.UserId != null && userIds.Contains(r.UserId.Value) && (r.IsDelete == false || r.IsDelete == null))
            .OrderByDescending(r => r.RegistrationId)
            .ToList()
            .GroupBy(r => r.UserId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var latestInfos = _db.StudentPersonalInfos
            .Where(p => userIds.Contains(p.UserId))
            .OrderByDescending(p => p.Id)
            .ToList()
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        bool updatedAny = false;
        var lst = studentsList.Select(s =>
        {
            latestRegs.TryGetValue(s.UserId, out var reg);
            latestInfos.TryGetValue(s.UserId, out var info);

            var majorName = !string.IsNullOrWhiteSpace(info?.major) && info.major != "N/A"
                ? info.major.Trim()
                : (!string.IsNullOrWhiteSpace(reg?.Major) && reg.Major != "N/A"
                    ? reg.Major.Trim()
                    : (!string.IsNullOrWhiteSpace(s.CurrentMajor) && s.CurrentMajor != "N/A" ? s.CurrentMajor.Trim() : ""));

            var classYear = !string.IsNullOrWhiteSpace(info?.academic_year_level) && info.academic_year_level != "N/A"
                ? info.academic_year_level.Trim()
                : (!string.IsNullOrWhiteSpace(reg?.AcademicYearLevel) && reg.AcademicYearLevel != "N/A"
                    ? reg.AcademicYearLevel.Trim()
                    : (!string.IsNullOrWhiteSpace(s.CurrentClassYear) && s.CurrentClassYear != "N/A" ? s.CurrentClassYear.Trim() : ""));

            var rollNo = !string.IsNullOrWhiteSpace(s.User?.RoleNo)
                ? s.User.RoleNo.Trim()
                : (!string.IsNullOrWhiteSpace(info?.roll_no)
                    ? info.roll_no.Trim()
                    : (!string.IsNullOrWhiteSpace(s.CurrentRollNo) ? s.CurrentRollNo.Trim() : ""));

            // Sync back to student entity so DB stays up-to-date
            if (s.CurrentMajor != majorName && !string.IsNullOrWhiteSpace(majorName))
            {
                s.CurrentMajor = majorName;
                updatedAny = true;
            }
            if (s.CurrentClassYear != classYear && !string.IsNullOrWhiteSpace(classYear))
            {
                s.CurrentClassYear = classYear;
                updatedAny = true;
            }
            if (s.CurrentRollNo != rollNo && !string.IsNullOrWhiteSpace(rollNo))
            {
                s.CurrentRollNo = rollNo;
                updatedAny = true;
            }

            var currentMajorText = (majorName ?? "").Trim();
            // Student.CurrentMajor (string) နဲ့ Major.MajorName တူတာ သို့မဟုတ် ပါဝင်တာ ရှာ → Faculty ရမည်
            var matchedMajor = majors.FirstOrDefault(m =>
                !string.IsNullOrEmpty(currentMajorText) && (
                    string.Equals(m.MajorName.Trim(), currentMajorText, StringComparison.OrdinalIgnoreCase) ||
                    m.MajorName.Trim().ToLower().Contains(currentMajorText.ToLower()) ||
                    currentMajorText.ToLower().Contains(m.MajorName.Trim().ToLower())
                )
            );

            if (matchedMajor != null && s.FacultyId != matchedMajor.FacultyId)
            {
                s.FacultyId = matchedMajor.FacultyId;
                updatedAny = true;
            }

            return new StudentModel
            {
                StudentId = s.StudentId,
                UserId = s.UserId,
                FullName = s.User.FullName,
                CurrentClassYear = classYear,
                CurrentMajor = majorName,
                FacultyName = s.User?.Faculty?.FacultyName ?? matchedMajor?.Faculty?.FacultyName,
                CurrentRollNo = rollNo,
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
            };
        }).ToList();

        if (updatedAny)
        {
            _db.SaveChanges();
        }

        return Ok(lst);
    }

    // GET: api/student/{id} (Get student details)
    [HttpGet("{id}")]
    [Permission("Student.View")]
    public IActionResult GetStudent(int id)
    {
        var item = _db.Students
            .Include(s => s.User)
                .ThenInclude(u => u.Faculty)
            .FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ကျောင်းသားကို ရှာမတွေ့ပါ။" });
        }

        var latestInfo = _db.StudentPersonalInfos
            .Where(p => p.UserId == item.UserId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        var latestReg = _db.StudentRegistrations
            .Where(r => r.UserId == item.UserId && (r.IsDelete == false || r.IsDelete == null))
            .OrderByDescending(r => r.RegistrationId)
            .FirstOrDefault();

        var majorName = !string.IsNullOrWhiteSpace(latestInfo?.major) && latestInfo.major != "N/A"
            ? latestInfo.major.Trim()
            : (!string.IsNullOrWhiteSpace(latestReg?.Major) && latestReg.Major != "N/A"
                ? latestReg.Major.Trim()
                : (!string.IsNullOrWhiteSpace(item.CurrentMajor) && item.CurrentMajor != "N/A" ? item.CurrentMajor.Trim() : ""));

        var classYear = !string.IsNullOrWhiteSpace(latestInfo?.academic_year_level) && latestInfo.academic_year_level != "N/A"
            ? latestInfo.academic_year_level.Trim()
            : (!string.IsNullOrWhiteSpace(latestReg?.AcademicYearLevel) && latestReg.AcademicYearLevel != "N/A"
                ? latestReg.AcademicYearLevel.Trim()
                : (!string.IsNullOrWhiteSpace(item.CurrentClassYear) && item.CurrentClassYear != "N/A" ? item.CurrentClassYear.Trim() : ""));

        var rollNo = !string.IsNullOrWhiteSpace(item.User?.RoleNo)
            ? item.User.RoleNo.Trim()
            : (!string.IsNullOrWhiteSpace(latestInfo?.roll_no)
                ? latestInfo.roll_no.Trim()
                : item.CurrentRollNo);

        var data = new StudentModel
        {
            StudentId = item.StudentId,
            UserId = item.UserId,
            FullName = item.User?.FullName,
            CurrentClassYear = classYear,
            CurrentMajor = majorName,
            FacultyName = item.User?.Faculty?.FacultyName,
            CurrentRollNo = rollNo,
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
    [AllowAnonymous]
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
                CurrentRollNo = userCheck.RoleNo ?? string.Empty,
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

        var latestInfo = _db.StudentPersonalInfos
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefault();

        var latestReg = _db.StudentRegistrations
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null))
            .OrderByDescending(r => r.RegistrationId)
            .FirstOrDefault();

        var majorName = !string.IsNullOrWhiteSpace(latestInfo?.major) && latestInfo.major != "N/A"
            ? latestInfo.major.Trim()
            : (!string.IsNullOrWhiteSpace(latestReg?.Major) && latestReg.Major != "N/A"
                ? latestReg.Major.Trim()
                : (!string.IsNullOrWhiteSpace(item.CurrentMajor) && item.CurrentMajor != "N/A" ? item.CurrentMajor.Trim() : ""));

        var classYear = !string.IsNullOrWhiteSpace(latestInfo?.academic_year_level) && latestInfo.academic_year_level != "N/A"
            ? latestInfo.academic_year_level.Trim()
            : (!string.IsNullOrWhiteSpace(latestReg?.AcademicYearLevel) && latestReg.AcademicYearLevel != "N/A"
                ? latestReg.AcademicYearLevel.Trim()
                : (!string.IsNullOrWhiteSpace(item.CurrentClassYear) && item.CurrentClassYear != "N/A" ? item.CurrentClassYear.Trim() : ""));

        var rollNo = !string.IsNullOrWhiteSpace(item.User?.RoleNo)
            ? item.User.RoleNo.Trim()
            : (!string.IsNullOrWhiteSpace(latestInfo?.roll_no)
                ? latestInfo.roll_no.Trim()
                : item.CurrentRollNo);

        var data = new StudentModel
        {
            StudentId = item.StudentId,
            UserId = item.UserId,
            FullName = item.User?.FullName,
            CurrentClassYear = classYear,
            CurrentMajor = majorName,
            CurrentRollNo = rollNo,
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

    [HttpGet("by-roll/{rollNo}")]
    [AllowAnonymous]
    public IActionResult GetStudentByRollNo(string rollNo)
    {
        if (string.IsNullOrWhiteSpace(rollNo))
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "Roll No ဖြည့်ပါ" });
        }

        string cleanRoll = rollNo.Trim();
        var item = _db.Students
            .Include(s => s.User)
            .FirstOrDefault(x => (x.IsDelete == false || x.IsDelete == null) &&
                                 ((x.User != null && x.User.RoleNo != null && x.User.RoleNo.ToLower() == cleanRoll.ToLower()) ||
                                  (x.CurrentRollNo != null && x.CurrentRollNo.ToLower() == cleanRoll.ToLower())));

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

    // POST: api/student (Create new student with validation)
    [HttpPost]
    [Permission("Student.Create")]
    public IActionResult CreateStudent(StudentCreateRequestModel request)
    {
        // Validate required fields
        if (request.UserId <= 0 || string.IsNullOrEmpty(request.CurrentClassYear) || string.IsNullOrEmpty(request.CurrentMajor))
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "လိုအပ်သော ဒေတာများကို ပြည့်စုံစွာ ဖြည့်သွင်းပါ။" });
        }

        // Validate Roll No format (alphanumeric & hyphen)
        if (!string.IsNullOrEmpty(request.CurrentRollNo))
        {
            // Regex format check for alphanumeric and hyphens
            if (!Regex.IsMatch(request.CurrentRollNo, "^[a-zA-Z0-9-]+$"))
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ခုံအမှတ် (Roll No) တွင် '-' မှလွဲ၍ အခြား အထူးသင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
            }

            // Check for duplicate Roll No
            var isRollNoExist = _db.Students.Any(x => x.CurrentRollNo == request.CurrentRollNo && (x.IsDelete == false || x.IsDelete == null));
            if (isRollNoExist)
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤ ခုံအမှတ် (Roll No) သည် စနစ်ထဲတွင် ရှိနှင့်ပြီးသား ဖြစ်ပါသည်။" });
            }
        }

        // Verify User existence & Student role (RoleId = 3)
        var userCheck = _db.Users.FirstOrDefault(u => u.UserId == request.UserId && u.IsDelete == false);
        if (userCheck is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ဤအသုံးပြုသူအကောင့် (UserId) ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
        }
        if (userCheck.RoleId != 3)
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ကျောင်းသားအကောင့် (Student Role) ဖြစ်မှသာ ကျောင်းသားစာရင်း သွင်းခွင့်ရှိသည်။" });
        }

        // Prevent duplicate active student record
        var isAlreadyStudent = _db.Students.Any(x => x.UserId == request.UserId && (x.IsDelete == false || x.IsDelete == null));
        if (isAlreadyStudent)
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤအသုံးပြုသူသည် ကျောင်းသားစာရင်းထဲတွင် ရှိပြီးသား ဖြစ်နေပါသည်။" });
        }

        // Insert into DB
        var newStudent = new Student
        {
            UserId = request.UserId,
            CurrentClassYear = request.CurrentClassYear,
            CurrentMajor = request.CurrentMajor,
            CurrentRollNo = request.CurrentRollNo?.ToUpper(), // Store in uppercase
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
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });
        _db.SaveChanges();

        return StatusCode(201, new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားအဖြစ် စာရင်းသွင်းခြင်း အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
        });
    }

    // PUT: api/student/{id} (Update student record)
    [HttpPut("{id}")]
    [Permission("Student.Edit")]
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

        // Check special characters in Roll No if provided
        if (!string.IsNullOrEmpty(request.CurrentRollNo))
        {
            if (!Regex.IsMatch(request.CurrentRollNo, "^[a-zA-Z0-9-]+$"))
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ခုံအမှတ် (Roll No) တွင် '-' မှလွဲ၍ အခြား အထူးသင်္ကေတများ မသုံးရပါ။" });
            }

            // Check duplicate Roll No for other students
            var isRollNoDuplicate = _db.Students.Any(x => x.CurrentRollNo == request.CurrentRollNo && x.StudentId != id && (x.IsDelete == false || x.IsDelete == null));
            if (isRollNoDuplicate)
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤ ခုံအမှတ် (Roll No) ကို အခြားကျောင်းသားတစ်ဦး အသုံးပြုထားပြီး ဖြစ်ပါသည်။" });
            }
        }

        // Update properties
        item.CurrentClassYear = request.CurrentClassYear;
        item.CurrentMajor = request.CurrentMajor;
        item.CurrentRollNo = request.CurrentRollNo?.ToUpper();
        item.Status = request.Status;
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "Student Updated",
            Description = $"Student '{item.CurrentRollNo ?? item.CurrentMajor}' ({item.CurrentClassYear}) was updated in the System.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = "Admin"
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
    [Permission("Student.Edit")]
    public IActionResult PatchStudent(int id, StudentPatchRequestModel request)
    {
        // 1. Check if student exists
        var item = _db.Students.FirstOrDefault(x => x.StudentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new StudentResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ကျောင်းသားမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        int updateCount = 0;

        // 2. Update Class Year if provided
        if (!string.IsNullOrEmpty(request.CurrentClassYear))
        {
            item.CurrentClassYear = request.CurrentClassYear;
            updateCount++;
        }

        // 3. Update Major if provided
        if (!string.IsNullOrEmpty(request.CurrentMajor))
        {
            item.CurrentMajor = request.CurrentMajor;
            updateCount++;
        }

        // 4. Update Roll No with validation
        if (!string.IsNullOrEmpty(request.CurrentRollNo))
        {
            // Regex check: Alphanumeric and hyphens only
            if (!Regex.IsMatch(request.CurrentRollNo, "^[a-zA-Z0-9-]+$"))
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ခုံအမှတ် (Roll No) တွင် '-' မှလွဲ၍ အခြား အထူးသင်္ကေတများ မသုံးရပါ။" });
            }

            // Check duplicate Roll No
            var isRollNoDuplicate = _db.Students.Any(x => x.CurrentRollNo == request.CurrentRollNo && x.StudentId != id && (x.IsDelete == false || x.IsDelete == null));
            if (isRollNoDuplicate)
            {
                return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ဤ ခုံအမှတ် (Roll No) ကို အခြားကျောင်းသားတစ်ဦး အသုံးပြုထားပြီး ဖြစ်ပါသည်။" });
            }

            item.CurrentRollNo = request.CurrentRollNo.ToUpper(); // Save in uppercase
            updateCount++;
        }

        // 5. Update Status if provided (e.g. Active, Absent, Dropped)
        if (!string.IsNullOrEmpty(request.Status))
        {
            item.Status = request.Status;
            updateCount++;
        }

        // 6. Update Semester Results
        if (request.Sem1_Result != null) { item.Sem1_Result = request.Sem1_Result == "None" ? null : request.Sem1_Result; updateCount++; }
        if (request.Sem2_Result != null) { item.Sem2_Result = request.Sem2_Result == "None" ? null : request.Sem2_Result; updateCount++; }
        if (request.Sem3_Result != null) { item.Sem3_Result = request.Sem3_Result == "None" ? null : request.Sem3_Result; updateCount++; }
        if (request.Sem4_Result != null) { item.Sem4_Result = request.Sem4_Result == "None" ? null : request.Sem4_Result; updateCount++; }
        if (request.Sem5_Result != null) { item.Sem5_Result = request.Sem5_Result == "None" ? null : request.Sem5_Result; updateCount++; }
        if (request.Sem6_Result != null) { item.Sem6_Result = request.Sem6_Result == "None" ? null : request.Sem6_Result; updateCount++; }
        if (request.Sem7_Result != null) { item.Sem7_Result = request.Sem7_Result == "None" ? null : request.Sem7_Result; updateCount++; }
        if (request.Sem8_Result != null) { item.Sem8_Result = request.Sem8_Result == "None" ? null : request.Sem8_Result; updateCount++; }
        if (request.Sem9_Result != null) { item.Sem9_Result = request.Sem9_Result == "None" ? null : request.Sem9_Result; updateCount++; }

        // Check if student passed all 9 semesters -> Auto deactivate user account
        CheckAndAutoDeactivateGraduatedStudent(item);

        // 7. Return error if no fields updated
        if (updateCount == 0)
        {
            return BadRequest(new StudentResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် အချက်အလက်များ လိုအပ်ပါသည်။" });
        }

        // Save timestamp
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "Student Updated",
            Description = $"Student '{item.CurrentRollNo ?? item.CurrentMajor}' ({item.CurrentClassYear}) was partially updated in the System.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = "Admin"
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

    // DELETE: api/student/{id} (Soft delete student)
    [HttpDelete("{id}")]
    [Permission("Student.Delete")]
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
            ActivityTitle = "Student Deleted",
            Description = $"Student '{item.CurrentRollNo ?? item.CurrentMajor}' ({item.CurrentClassYear}) was deleted from the System.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = "Admin"
        });
        _db.SaveChanges();

        return Ok(new StudentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းသားမှတ်တမ်းကို ပယ်ဖျက်ခြင်း အောင်မြင်ပါသည်။" : "ပယ်ဖျက်ခြင်း မအောင်မြင်ပါ။"
        });
    }

    private void CheckAndAutoDeactivateGraduatedStudent(Student student)
    {
        var semResults = new[]
        {
            student.Sem1_Result, student.Sem2_Result, student.Sem3_Result,
            student.Sem4_Result, student.Sem5_Result, student.Sem6_Result,
            student.Sem7_Result, student.Sem8_Result, student.Sem9_Result
        };

        bool allPassed = semResults.All(r => 
            !string.IsNullOrEmpty(r) &&
            (string.Equals(r, "Pass", StringComparison.OrdinalIgnoreCase) || 
             string.Equals(r, "Credit_Transferred", StringComparison.OrdinalIgnoreCase)));

        if (allPassed)
        {
            if (student.UserId > 0)
            {
                var user = _db.Users.FirstOrDefault(u => u.UserId == student.UserId);
                if (user != null && user.Status != "Inactive")
                {
                    user.Status = "Inactive";
                }
            }
            student.Status = "Inactive";
        }
    }

    // 🎯 ၆။ GET: api/student/count/active 
    [HttpGet("count/active")]
    [AllowAnonymous]
    public IActionResult GetActiveStudentCount()
    {
        // 1. Check for any students with all 9 semesters passed and deactivate their user accounts
        var allStudents = _db.Students.Where(s => (s.IsDelete == false || s.IsDelete == null)).ToList();
        bool changes = false;
        foreach (var s in allStudents)
        {
            var semResults = new[]
            {
                s.Sem1_Result, s.Sem2_Result, s.Sem3_Result,
                s.Sem4_Result, s.Sem5_Result, s.Sem6_Result,
                s.Sem7_Result, s.Sem8_Result, s.Sem9_Result
            };

            if (semResults.All(r => !string.IsNullOrEmpty(r) && (string.Equals(r, "Pass", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "Credit_Transferred", StringComparison.OrdinalIgnoreCase))))
            {
                if (s.UserId > 0)
                {
                    var u = _db.Users.FirstOrDefault(usr => usr.UserId == s.UserId);
                    if (u != null && u.Status != "Inactive")
                    {
                        u.Status = "Inactive";
                        changes = true;
                    }
                }
                if (s.Status != "Inactive")
                {
                    s.Status = "Inactive";
                    changes = true;
                }
            }
        }
        if (changes) _db.SaveChanges();

        // 2. Count Active students from User Management (Users with RoleId == 3 / Student role and Status == "Active")
        var count = _db.Users.Count(u => 
            (u.RoleId == 3 || (u.Role != null && u.Role.RoleName == "Student")) && 
            u.Status == "Active" && 
            (u.IsDelete == false || u.IsDelete == null)
        );

        return Ok(new { Count = count });
    }

    // GET: api/student/profile/{userId} - Get student profile
    [HttpGet("profile/{userId}")]
    public async Task<IActionResult> GetStudentProfile(int userId)
    {
        var student = await _db.Students
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId && (s.IsDelete == false || s.IsDelete == null));

        if (student == null)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
            {
                var newStudent = new Student
                {
                    UserId = user.UserId,
                    CurrentClassYear = "First Year",
                    CurrentMajor = "N/A",
                    Status = "Active",
                    CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                    IsDelete = false
                };
                _db.Students.Add(newStudent);
                await _db.SaveChangesAsync();
                student = newStudent;
                student.User = user;
            }
            else
            {
                return NotFound(new { IsSuccess = false, Message = "ကျောင်းသားကို ရှာမတွေ့ပါ။" });
            }
        }

        // Single optimized query for registrations and payments
        var registrations = await _db.StudentRegistrations
            .AsNoTracking()
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null))
            .OrderByDescending(r => r.RegistrationId)
            .Select(r => new
            {
                r.RegistrationId,
                r.Status,
                r.AcademicYearRange,
                r.AcademicYearLevel,
                r.Major,
                r.Dob,
                r.Email,
                r.StudentImage,
                r.NrcFrontImage,
                r.NrcBackImage,
                r.CensusImage,
                r.CreatedDatetime,
                Payments = r.RegistrationPayments
                    .Where(p => p.IsDelete == false || p.IsDelete == null)
                    .Select(p => new { p.PaymentId, p.Status, p.CreatedDateTime, p.AmountPaid })
                    .ToList()
            })
            .ToListAsync();

        var reg = registrations.FirstOrDefault();

        var notifications = registrations.Select(r => new
        {
            r.RegistrationId,
            r.Status,
            r.AcademicYearRange,
            r.AcademicYearLevel,
            r.CreatedDatetime,
            r.Payments
        }).ToList();

        var studentImage = student.StudentImage;
        if (string.IsNullOrEmpty(studentImage))
        {
            studentImage = reg?.StudentImage;
            if (string.IsNullOrEmpty(studentImage))
            {
                studentImage = await _db.StudentPersonalInfos
                    .AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.Id)
                    .Select(p => p.student_image)
                    .FirstOrDefaultAsync();
            }
        }

        StudentRetakeStatusModel? retakeStatus = null;
        StudentGraduationStatusModel? graduationStatus = null;
        try
        {
            var rollNo = student.User?.RoleNo ?? student.CurrentRollNo;
            retakeStatus = await _enrollmentService.GetStudentRetakeStatusAsync(userId, student.StudentId, rollNo);
            graduationStatus = await _enrollmentService.GetStudentGraduationStatusAsync(userId, student.StudentId, rollNo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting retake / graduation status: {ex.Message}");
        }

        string currentStatus = graduationStatus?.IsGraduated == true ? "Graduated" : (student.Status ?? "Active");

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
            Status        = currentStatus,
            Dob           = reg?.Dob,
            Email         = reg?.Email,
            Phone         = (string?)null,
            StudentImage  = studentImage,
            NrcFrontImage = student.NrcFrontImage ?? reg?.NrcFrontImage,
            NrcBackImage  = student.NrcBackImage ?? reg?.NrcBackImage,
            CensusImage   = student.CensusImage ?? reg?.CensusImage,
            Sem1_Result   = student.Sem1_Result,
            Sem2_Result   = student.Sem2_Result,
            Sem3_Result   = student.Sem3_Result,
            Sem4_Result   = student.Sem4_Result,
            Sem5_Result   = student.Sem5_Result,
            Sem6_Result   = student.Sem6_Result,
            Sem7_Result   = student.Sem7_Result,
            Sem8_Result   = student.Sem8_Result,
            Sem9_Result   = student.Sem9_Result,
            Registrations = notifications,
            RetakeStatus  = retakeStatus,
            GraduationStatus = graduationStatus
        });
    }

    // GET: api/student/graduation-status/{userId}
    [HttpGet("graduation-status/{userId}")]
    public async Task<IActionResult> GetStudentGraduationStatus(int userId, [FromQuery] int? newStudentAccId = null, [FromQuery] string? rollNo = null)
    {
        var student = await _db.Students
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId && (s.IsDelete == false || s.IsDelete == null));

        var gradStatus = await _enrollmentService.GetStudentGraduationStatusAsync(userId, student?.StudentId, rollNo ?? student?.User?.RoleNo ?? student?.CurrentRollNo, newStudentAccId);
        return Ok(gradStatus);
    }

    // GET: api/student/profile/{userId}/image - Lightweight profile image
    [HttpGet("profile/{userId}/image")]
    public async Task<IActionResult> GetStudentProfileImage(int userId)
    {
        var img = await _db.Students
            .AsNoTracking()
            .Where(s => s.UserId == userId && (s.IsDelete == false || s.IsDelete == null))
            .Select(s => s.StudentImage)
            .FirstOrDefaultAsync();

        return Ok(new { StudentImage = img ?? string.Empty });
    }

    // GET: api/student/retake-status/{userId}
    [HttpGet("retake-status/{userId}")]
    public async Task<IActionResult> GetStudentRetakeStatus(int userId)
    {
        var student = await _db.Students
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId && (s.IsDelete == false || s.IsDelete == null));

        var retakeStatus = await _enrollmentService.GetStudentRetakeStatusAsync(userId, student?.StudentId, student?.User?.RoleNo ?? student?.CurrentRollNo);
        return Ok(retakeStatus);
    }

    // GET: api/student/subject-selections/{userId}
    [HttpGet("subject-selections/{userId}")]
    public async Task<IActionResult> GetStudentSubjectSelections(int userId, [FromQuery] int? newStudentAccId = null)
    {
        var result = await _enrollmentService.GetStudentSubjectSelectionsAsync(userId, newStudentAccId);
        return Ok(result);
    }

    // GET: api/student/settings/max-retake-limit
    [HttpGet("settings/max-retake-limit")]
    public async Task<IActionResult> GetMaxRetakeLimit()
    {
        int limit = await _enrollmentService.GetMaxRetakeLimitAsync();
        return Ok(new { MaxRetakeLimit = limit });
    }

    // PUT: api/student/settings/max-retake-limit
    [HttpPut("settings/max-retake-limit")]
    public async Task<IActionResult> UpdateMaxRetakeLimit([FromBody] SystemSettingModel model)
    {
        int newLimit = 0;
        if (model != null && !string.IsNullOrEmpty(model.SettingValue) && int.TryParse(model.SettingValue, out int parsedLimit))
        {
            newLimit = parsedLimit;
        }

        if (newLimit <= 0)
        {
            return BadRequest(new { IsSuccess = false, Message = "တရားဝင်သော Retake အကြိမ်အရေအတွက် ထည့်သွင်းပေးပါ။" });
        }

        string username = User?.Identity?.Name ?? "Admin";
        bool success = await _enrollmentService.UpdateMaxRetakeLimitAsync(newLimit, username);
        if (success)
        {
            return Ok(new { IsSuccess = true, Message = $"အများဆုံး Retake အကြိမ်အရေအတွက် ({newLimit} ကြိမ်) အား အောင်မြင်စွာ ပြောင်းလဲသတ်မှတ်ပြီးပါပြီ။", MaxRetakeLimit = newLimit });
        }

        return StatusCode(500, new { IsSuccess = false, Message = "Setting အား ပြောင်းလဲသတ်မှတ်ရာတွင် အမှားဖြစ်ပေါ်နေပါသည်။" });
    }

    // GET: api/student/settings/semester-credits
    [HttpGet("settings/semester-credits")]
    public async Task<IActionResult> GetFacultySemesterCredits([FromQuery] int? facultyId = null)
    {
        var faculties = await _db.Faculties
            .AsNoTracking()
            .Where(f => f.IsDelete == false || f.IsDelete == null)
            .OrderBy(f => f.FacultyId)
            .ToListAsync();

        var semesters = await _db.Semesters
            .AsNoTracking()
            .Where(s => s.IsDelete == false || s.IsDelete == null)
            .OrderBy(s => s.Sequence ?? s.SemesterId)
            .ToListAsync();

        var creditsQuery = _db.FacultySemesterCredits
            .AsNoTracking()
            .Where(c => c.IsDelete == false || c.IsDelete == null);

        if (facultyId.HasValue && facultyId.Value > 0)
        {
            creditsQuery = creditsQuery.Where(c => c.FacultyId == facultyId.Value);
        }

        var credits = await creditsQuery.ToListAsync();

        var resultList = new List<FacultySemesterCreditModel>();

        var filteredFaculties = facultyId.HasValue && facultyId.Value > 0 
            ? faculties.Where(f => f.FacultyId == facultyId.Value).ToList() 
            : faculties;

        foreach (var fac in filteredFaculties)
        {
            foreach (var sem in semesters)
            {
                var existing = credits.FirstOrDefault(c => c.FacultyId == fac.FacultyId && c.SemesterId == sem.SemesterId);
                int reqCred = existing?.RequiredCredits ?? 24;
                int minCred = existing?.MinCredits ?? (existing?.RequiredCredits != null && existing.RequiredCredits > 0 ? Math.Min(18, existing.RequiredCredits) : 18);
                int maxCred = existing?.MaxCredits ?? (existing?.RequiredCredits != null && existing.RequiredCredits > 0 ? existing.RequiredCredits : 24);

                resultList.Add(new FacultySemesterCreditModel
                {
                    Id = existing?.Id ?? 0,
                    FacultyId = fac.FacultyId,
                    FacultyName = fac.FacultyName,
                    SemesterId = sem.SemesterId,
                    SemesterName = sem.SemesterName,
                    Sequence = sem.Sequence,
                    RequiredCredits = reqCred,
                    MinCredits = minCred,
                    MaxCredits = maxCred
                });
            }
        }

        return Ok(resultList);
    }

    // PUT: api/student/settings/semester-credits
    [HttpPut("settings/semester-credits")]
    public async Task<IActionResult> UpdateFacultySemesterCredit([FromBody] FacultySemesterCreditUpdateRequest request)
    {
        if (request == null || request.FacultyId <= 0 || request.SemesterId <= 0)
        {
            return BadRequest(new { IsSuccess = false, Message = "မှန်ကန်သော Faculty နှင့် Semester ရွေးချယ်ပေးပါ။" });
        }

        int minCred = request.MinCredits ?? 18;
        int maxCred = request.MaxCredits ?? (request.RequiredCredits > 0 ? request.RequiredCredits : 24);
        int reqCred = request.RequiredCredits > 0 ? request.RequiredCredits : maxCred;

        if (minCred <= 0 || maxCred <= 0)
        {
            return BadRequest(new { IsSuccess = false, Message = "Credit Points တန်ဖိုးများသည် အနည်းဆုံး ၁ မှတ်နှင့်အထက် ဖြစ်ရပါမည်။" });
        }
        if (minCred > maxCred)
        {
            return BadRequest(new { IsSuccess = false, Message = "အနည်းဆုံး Credit (Min Credits) သည် အများဆုံး Credit (Max Credits) ထက် မကြီးရပါ။" });
        }

        string username = User?.Identity?.Name ?? "Admin";

        var existing = await _db.FacultySemesterCredits
            .FirstOrDefaultAsync(c => c.FacultyId == request.FacultyId && c.SemesterId == request.SemesterId);

        if (existing != null)
        {
            existing.RequiredCredits = reqCred;
            existing.MinCredits = minCred;
            existing.MaxCredits = maxCred;
            existing.ModifiedDateTime = DateTime.Now;
            existing.ModifiedBy = username;
            existing.IsDelete = false;
        }
        else
        {
            _db.FacultySemesterCredits.Add(new FacultySemesterCredit
            {
                FacultyId = request.FacultyId,
                SemesterId = request.SemesterId,
                RequiredCredits = reqCred,
                MinCredits = minCred,
                MaxCredits = maxCred,
                CreatedDateTime = DateTime.Now,
                CreatedBy = username,
                IsDelete = false
            });
        }

        await _db.SaveChangesAsync();

        return Ok(new 
        { 
            IsSuccess = true, 
            Message = "Faculty Semester Credit သတ်မှတ်ချက် အောင်မြင်စွာ ပြောင်းလဲသိမ်းဆည်းပြီးပါပြီ။",
            RequiredCredits = reqCred,
            MinCredits = minCred,
            MaxCredits = maxCred
        });
    }

    // GET: api/student/settings/semester-credit/{facultyId}/{semesterId}
    [HttpGet("settings/semester-credit/{facultyId}/{semesterId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSingleFacultySemesterCredit(int facultyId, int semesterId)
    {
        var existing = await _db.FacultySemesterCredits
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.FacultyId == facultyId && c.SemesterId == semesterId && (c.IsDelete == false || c.IsDelete == null));

        int targetCredits = existing?.RequiredCredits ?? 24;
        int minCredits = existing?.MinCredits ?? (existing?.RequiredCredits != null && existing.RequiredCredits > 0 ? Math.Min(18, existing.RequiredCredits) : 18);
        int maxCredits = existing?.MaxCredits ?? (existing?.RequiredCredits != null && existing.RequiredCredits > 0 ? existing.RequiredCredits : 24);

        return Ok(new 
        { 
            FacultyId = facultyId, 
            SemesterId = semesterId, 
            RequiredCredits = targetCredits,
            MinCredits = minCredits,
            MaxCredits = maxCredits
        });
    }

    // PUT: api/student/profile/{userId}/image - Update profile image
    [HttpPut("profile/{userId}/image")]
    public IActionResult UpdateStudentProfileImage(int userId, [FromBody] StudentProfileImageRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            return BadRequest(new { IsSuccess = false, Message = "ဓာတ်ပုံရွေးချယ်ထားခြင်း မရှိပါ။" });
        }

        var student = _db.Students.FirstOrDefault(s => s.UserId == userId && (s.IsDelete == false || s.IsDelete == null));
        if (student == null)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                student = new Student
                {
                    UserId = user.UserId,
                    CurrentClassYear = "First Year",
                    CurrentMajor = "N/A",
                    Status = "Active",
                    CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                    IsDelete = false
                };
                _db.Students.Add(student);
            }
        }

        if (student != null)
        {
            student.StudentImage = request.ImageBase64;
            student.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
        }

        var regs = _db.StudentRegistrations
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null))
            .ToList();

        foreach (var r in regs)
        {
            r.StudentImage = request.ImageBase64;
            r.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
        }

        _db.SaveChanges();

        return Ok(new { IsSuccess = true, Message = "Profile ဓာတ်ပုံ ပြောင်းလဲခြင်း အောင်မြင်ပါသည်။" });
    }
}

public class StudentProfileImageRequest
{
    public string? ImageBase64 { get; set; }
}


