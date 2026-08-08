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

    public StudentController(SmartCampusDbContext db, IFacultyDataScopeService scopeService)
    {
        _db = db;
        _scopeService = scopeService;
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

        var lst = studentsQuery
            .OrderByDescending(s => s.StudentId)
            .ToList()
            .Select(s =>
            {
                var currentMajorText = (s.CurrentMajor ?? "").Trim();
                // Match Student.CurrentMajor with Major.MajorName to resolve Faculty
                var matchedMajor = majors.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(currentMajorText) && (
                        string.Equals(m.MajorName.Trim(), currentMajorText, StringComparison.OrdinalIgnoreCase) ||
                        m.MajorName.Trim().ToLower().Contains(currentMajorText.ToLower()) ||
                        currentMajorText.ToLower().Contains(m.MajorName.Trim().ToLower())
                    )
                );

                return new StudentModel
                {
                    StudentId = s.StudentId,
                    UserId = s.UserId,
                    FullName = s.User.FullName,
                    CurrentClassYear = s.CurrentClassYear,
                    CurrentMajor = s.CurrentMajor,
                    FacultyName = s.User?.Faculty?.FacultyName ?? matchedMajor?.Faculty?.FacultyName,
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
                };
            })
            .ToList();

        return Ok(lst);
    }

    // GET: api/student/{id} (Get student details)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,admin")]
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

        var data = new StudentModel
        {
            StudentId = item.StudentId,
            UserId = item.UserId,
            FullName = item.User?.FullName,
            CurrentClassYear = item.CurrentClassYear,
            CurrentMajor = item.CurrentMajor,
            FacultyName = item.User?.Faculty?.FacultyName,
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

    // GET: api/student/count/active (Active student count)
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
    public IActionResult GetStudentProfile(int userId)
    {
        var student = _db.Students
            .Include(s => s.User)
            .FirstOrDefault(s => s.UserId == userId && (s.IsDelete == false || s.IsDelete == null));

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
                _db.SaveChanges();
                student.User = user;
            }
            else
            {
                return NotFound(new { IsSuccess = false, Message = "ကျောင်းသားကို ရှာမတွေ့ပါ။" });
            }
        }

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

        var studentImage = _db.StudentRegistrations
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null) && !string.IsNullOrEmpty(r.StudentImage))
            .OrderByDescending(r => r.RegistrationId)
            .Select(r => r.StudentImage)
            .FirstOrDefault();

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
            StudentImage  = studentImage,
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

    // PUT: api/student/profile/{userId}/image - Update profile image
    [HttpPut("profile/{userId}/image")]
    public IActionResult UpdateStudentProfileImage(int userId, [FromBody] StudentProfileImageRequest request)
    {
        var regs = _db.StudentRegistrations
            .Where(r => r.UserId == userId && (r.IsDelete == false || r.IsDelete == null))
            .ToList();

        if (!regs.Any())
            return NotFound(new { IsSuccess = false, Message = "ကျောင်းအပ်နှံမှု မှတ်တမ်းကို ရှာမတွေ့ပါ။" });

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


