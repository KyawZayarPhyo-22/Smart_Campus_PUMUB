using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

using Smart_Campus_PUMUB.WebApi.Services;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentRegistrationsController : ControllerBase
{
    private const string PendingConfirmationStatus = "Pending Confirmation";
    private const string LegacyPendingStatus = "Pending";
    private const string ApprovedStatus = "Approved";
    private const string RejectedStatus = "Rejected";

    private readonly SmartCampusDbContext _db;
    private readonly IFacultyDataScopeService _scopeService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IEmailService _emailService;

    public StudentRegistrationsController(SmartCampusDbContext db, IFacultyDataScopeService scopeService, IEnrollmentService enrollmentService, IEmailService emailService)
    {
        _db = db;
        _scopeService = scopeService;
        _enrollmentService = enrollmentService;
        _emailService = emailService;
    }

    private static string NormalizeRegistrationStatus(string? status)
    {
        if (string.Equals(status, LegacyPendingStatus, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(status))
        {
            return PendingConfirmationStatus;
        }

        if (string.Equals(status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return ApprovedStatus;
        }

        if (string.Equals(status, RejectedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return RejectedStatus;
        }

        return status;
    }

    private static bool CanProceedToPayment(string? status)
    {
        return string.Equals(status, ApprovedStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPendingReviewStatus(string? status)
    {
        return string.Equals(status, PendingConfirmationStatus, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, LegacyPendingStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMyanmarText(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, @"^[\u1000-\u109F]+$");
    }

    private static bool IsMyanmarNrcType(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, @"^\([\u1000-\u109F]+\)$");
    }

    private static bool IsFullMyanmarNrc(string value)
    {
        return Regex.IsMatch(value, @"^[0-9]{1,2}/[\u1000-\u109F]+\([\u1000-\u109F]+\)[0-9]{6}$");
    }

    private static object ToRegistrationResponse(StudentRegistration item)
    {
        var status = NormalizeRegistrationStatus(item.Status);

        return new
        {
            item.RegistrationId,
            item.UserId,
            item.AdmissionSerialNo,
            item.AcademicYearRange,
            item.AcademicYearLevel,
            item.Major,
            item.RollNo,
            item.UniversityRegNo,
            item.AdmissionYear,
            item.ApplicationDate,
            item.StudentNameMm,
            item.StudentNameEn,
            item.MotherName,
            item.FatherName,
            item.GenderRelation,
            item.Ethnicity,
            item.Religion,
            item.Pob,
            item.BirthPlaceRegion,
            item.StudentNrcNo,
            item.NationalityStatus,
            item.Dob,
            item.Email,
            item.BloodType,
            item.CovidVaccineStatus,
            item.CurrentAddress,
            item.PermanentAddressMm,
            item.PermanentAddressEn,
            item.MatricRollNo,
            item.MatricPassedYear,
            item.ExamCenter,
            item.FatherOccupation,
            item.MotherOccupation,
            item.PastExamMajor,
            item.PastExamRollNo,
            item.PastExamYear,
            item.PastExamStatus,
            item.PreviousYearRollNo,
            item.GuardianName,
            item.GuardianRelationship,
            item.GuardianOccupation,
            item.GuardianAddressPhone,
            item.AppGuardianName,
            item.AppGuardianNrc,
            item.AppGuardianPhone,
            item.AppGuardianAddress,
            item.AppStudentName,
            item.AppStudentPhone,
            item.StipendRequested,
            Status = status,
            CanProceedToPayment = CanProceedToPayment(status),
            item.CreatedDatetime,
            item.CreatedBy,
            item.ModifiedDatetime,
            item.ModifiedBy,
            item.IsDelete,
            item.StudentImage,
            item.SignatureImage,
            item.NrcFrontImage,
            item.NrcBackImage,
            item.CensusImage,
            item.FatherNrcFrontImage,
            item.FatherNrcBackImage,
            item.MotherNrcFrontImage,
            item.MotherNrcBackImage,
            item.RegistrationPayments
        };
    }

    [HttpGet("paginate")]
    [Permission("StudentRegistrations.View")]
    public IActionResult GetRegistrationsPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? level = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.StudentRegistrations
            .AsNoTracking()
            .Include(x => x.User)
                .ThenInclude(u => u!.Faculty)
            .Include(x => x.RegistrationPayments)
            .Where(x => x.IsDelete == false || x.IsDelete == null);

        // Hierarchical RBAC Faculty Scoping:
        if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
        {
            var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
            if (scopedFacultyId.HasValue)
            {
                query = query.Where(x => x.User != null && x.User.FacultyId == scopedFacultyId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => (s.RollNo != null && s.RollNo.Contains(searchTerm)) ||
                                   (s.StudentNameMm != null && s.StudentNameMm.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(level) && !level.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.AcademicYearLevel == level);
        }

        if (fromDate.HasValue)
        {
            var fDate = fromDate.Value.Date;
            query = query.Where(s => s.CreatedDatetime >= fDate);
        }

        if (toDate.HasValue)
        {
            var tDate = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(s => s.CreatedDatetime <= tDate);
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x.RegistrationId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var majors = _db.Majors
            .Include(m => m.Faculty)
            .Where(m => m.IsDelete == false || m.IsDelete == null)
            .ToList();

        var dataItems = items.Select(item =>
        {
            var majorText = (item.Major ?? "").Trim();
            var matchedMajor = majors.FirstOrDefault(m =>
                !string.IsNullOrEmpty(majorText) && (
                    string.Equals(m.MajorName.Trim(), majorText, StringComparison.OrdinalIgnoreCase) ||
                    m.MajorName.Trim().ToLower().Contains(majorText.ToLower()) ||
                    majorText.ToLower().Contains(m.MajorName.Trim().ToLower())
                )
            );

            var facultyName = item.User?.Faculty?.FacultyName ?? matchedMajor?.Faculty?.FacultyName;

            return new StudentRegistrationDataModel
            {
                RegistrationId = item.RegistrationId,
                StudentNameMm = item.StudentNameMm,
                Major = item.Major,
                FacultyName = facultyName,
                RollNo = item.RollNo ?? "",
                AcademicYearLevel = item.AcademicYearLevel,
                CreatedDatetime = item.CreatedDatetime ?? DateTime.MinValue,
                Status = NormalizeRegistrationStatus(item.Status),
                RegistrationPayments = item.RegistrationPayments.Select(p => new RegistrationPaymentModel
                {
                    PaymentId = p.PaymentId,
                    RegistrationId = p.RegistrationId,
                    AmountPaid = p.AmountPaid,
                    PaymentMethod = p.PaymentMethod,
                    ReceiptImage = p.ReceiptImage,
                    Status = p.Status,
                    CreatedDateTime = p.CreatedDateTime
                }).ToList()
            };
        }).ToList();

        var result = new PagedResult<StudentRegistrationDataModel>
        {
            Items = dataItems,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(result);
    }

    // ₉. GET: UserId ဖြင့် နောက်ဆုံး Registration တစ်ခု ရယူရန် (Auto-Fill အတွက်)
    [HttpGet("latest/{userId}")]
    [AllowAnonymous]
    public IActionResult GetLatestRegistrationByUser(int userId)
    {
        var item = _db.StudentRegistrations
            .AsNoTracking()
            .Where(x => x.UserId == userId && (x.IsDelete == false || x.IsDelete == null))
            .OrderByDescending(x => x.RegistrationId)
            .FirstOrDefault();

        if (item is null)
        {
            return NotFound(new { message = "ဤ User ၏ Registration မှတ်တမ်းမရှိပါ။" });
        }

        // Split stored NRC back into components for the form
        string? nrcState = null, nrcTownship = null, nrcType = "(နိုင်)", nrcNumber = null;
        if (!string.IsNullOrWhiteSpace(item.StudentNrcNo) && item.StudentNrcNo != "-")
        {
            var slashIdx = item.StudentNrcNo.IndexOf('/');
            if (slashIdx > 0)
            {
                nrcState = item.StudentNrcNo[..slashIdx];
                var rest = item.StudentNrcNo[(slashIdx + 1)..];
                // Extract type (text inside parentheses)
                var typeStart = rest.IndexOf('(');
                var typeEnd = rest.IndexOf(')');
                if (typeStart >= 0 && typeEnd > typeStart)
                {
                    nrcTownship = rest[..typeStart];
                    nrcType = rest[typeStart..(typeEnd + 1)];
                    nrcNumber = rest[(typeEnd + 1)..];
                }
                else
                {
                    nrcTownship = rest;
                }
            }
        }

        return Ok(new
        {
            item.RegistrationId,
            item.UserId,
            item.AdmissionSerialNo,
            item.AcademicYearRange,
            item.AcademicYearLevel,
            item.Major,
            item.RollNo,
            item.UniversityRegNo,
            item.AdmissionYear,
            item.StudentNameMm,
            item.StudentNameEn,
            item.MotherName,
            item.FatherName,
            item.GenderRelation,
            item.Ethnicity,
            item.Religion,
            item.Pob,
            item.BirthPlaceRegion,
            item.StudentNrcNo,
            NrcState = nrcState,
            NrcTownship = nrcTownship,
            NrcType = nrcType,
            NrcNumber = nrcNumber,
            item.NationalityStatus,
            item.Dob,
            item.Email,
            item.BloodType,
            item.CovidVaccineStatus,
            item.CurrentAddress,
            item.PermanentAddressMm,
            item.PermanentAddressEn,
            item.MatricRollNo,
            item.MatricPassedYear,
            item.ExamCenter,
            item.FatherOccupation,
            item.MotherOccupation,
            item.PastExamMajor,
            item.PastExamRollNo,
            item.PastExamYear,
            item.PastExamStatus,
            item.PreviousYearRollNo,
            item.GuardianName,
            item.GuardianRelationship,
            item.GuardianOccupation,
            item.GuardianAddressPhone,
            item.AppGuardianName,
            item.AppGuardianNrc,
            item.AppGuardianPhone,
            item.AppGuardianAddress,
            item.AppStudentName,
            item.AppStudentPhone,
            item.StipendRequested
        });
    }

    // ၁။ GET: ဖောင်အားလုံး စာရင်းယူရန် (Read All)
    [HttpGet]
    [Permission("StudentRegistrations.View")]
    public IActionResult GetRegistrations()
    {
        var lst = _db.StudentRegistrations
            .AsNoTracking()
            .Include(x => x.RegistrationPayments)
            .Where(x => x.IsDelete == false || x.IsDelete == null)
            .OrderByDescending(x => x.RegistrationId)
            .ToList();

        foreach (var item in lst)
        {
            item.Status = NormalizeRegistrationStatus(item.Status);
        }

        return Ok(lst);
    }

    // ၂။ GET: ဖောင်တစ်ခုချင်းစီ အသေးစိတ်ကြည့်ရန် (Read One)
    [HttpGet("{id}")]
    [AllowAnonymous]
    public IActionResult GetRegistration(int id)
    {
        var item = _db.StudentRegistrations
            .AsNoTracking()
            .Include(x => x.RegistrationPayments)
            .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
        }

        // Fallback missing images from StudentPersonalInfo or Students table if available
        var pInfo = _db.StudentPersonalInfos
            .AsNoTracking()
            .FirstOrDefault(p => (item.UserId != null && p.UserId == item.UserId) || (!string.IsNullOrEmpty(item.RollNo) && p.roll_no == item.RollNo));

        if (pInfo != null)
        {
            if (string.IsNullOrEmpty(item.StudentImage)) item.StudentImage = pInfo.student_image;
            if (string.IsNullOrEmpty(item.NrcFrontImage)) item.NrcFrontImage = pInfo.nrc_front_image;
            if (string.IsNullOrEmpty(item.NrcBackImage)) item.NrcBackImage = pInfo.nrc_back_image;
            if (string.IsNullOrEmpty(item.CensusImage)) item.CensusImage = pInfo.census_image;
            if (string.IsNullOrEmpty(item.FatherNrcFrontImage)) item.FatherNrcFrontImage = pInfo.father_nrc_front_image;
            if (string.IsNullOrEmpty(item.FatherNrcBackImage)) item.FatherNrcBackImage = pInfo.father_nrc_back_image;
            if (string.IsNullOrEmpty(item.MotherNrcFrontImage)) item.MotherNrcFrontImage = pInfo.mother_nrc_front_image;
            if (string.IsNullOrEmpty(item.MotherNrcBackImage)) item.MotherNrcBackImage = pInfo.mother_nrc_back_image;
        }

        if (string.IsNullOrEmpty(item.StudentImage) && item.UserId.HasValue)
        {
            var studentObj = _db.Students.AsNoTracking().FirstOrDefault(s => s.UserId == item.UserId);
            if (studentObj != null && !string.IsNullOrEmpty(studentObj.StudentImage))
            {
                item.StudentImage = studentObj.StudentImage;
            }
        }

        return Ok(ToRegistrationResponse(item));
    }

    // ၃။ POST: ကျောင်းအပ်ဖောင် အသစ်တင်သွင်းရန် (Create)
    [HttpPost]
    [AllowAnonymous]
    public IActionResult CreateRegistration([FromForm] StudentRegistrationCreateRequestModel request)
    {
        bool isNewStudent = request.NewStudentAccId.HasValue && request.NewStudentAccId > 0;

        if (!isNewStudent && (request.UserId == null || request.UserId <= 0))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအိုင်ဒီ (UserId သို့မဟုတ် NewStudentAccId) ထည့်သွင်းရန် လိုအပ်သည်။" });
        }

        if (isNewStudent)
        {
            var newStudentCheck = _db.NewStudentAccs.FirstOrDefault(x => x.NewStudentAccId == request.NewStudentAccId);
            if (newStudentCheck == null)
            {
                return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းသားအသစ်အကောင့်ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
            }
        }
        else
        {
            var userCheck = _db.Users
              .Include(x => x.Role)
              .FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);

            if (userCheck is null)
            {
                return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအကောင့်ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
            }

            // ၂။ RoleId သို့မဟုတ် RoleName ကို အခြေခံ၍ စစ်ဆေးပါ
            if (userCheck.RoleId != 3 && !string.Equals(userCheck.Role?.RoleName, "Student", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new StudentRegistrationResponseModel
                {
                    IsSuccess = false,
                    Message = "ကျောင်းသားအကောင့်များသာ ကျောင်းအပ်ဖောင် တင်သွင်းခွင့်ရှိသည်။"
                });
            }

            // ၃။ Retake စည်းမျဉ်း စစ်ဆေးခြင်း (Max Retake Limit Check)
            var retakeStatus = _enrollmentService.GetStudentRetakeStatusAsync(request.UserId.Value, null, request.roll_no).GetAwaiter().GetResult();
            if (retakeStatus.IsDisqualified)
            {
                return BadRequest(new StudentRegistrationResponseModel
                {
                    IsSuccess = false,
                    Message = $"သတ်မှတ်ထားသော အများဆုံး Retake အကြိမ်အရေအတွက် ({retakeStatus.MaxRetakeLimit} ကြိမ်) ပြည့်သွားပြီဖြစ်ပါသဖြင့် ကျောင်းအပ်နှံခြင်း/ဘာသာရပ်စာရင်းသွင်းခြင်း ပြုလုပ်ခွင့် မရှိတော့ပါ။ ကျောင်းတက်ရောက်ခွင့် အရည်အချင်း မပြည့်မီတော့ပါ။"
                });
            }
        }

        // =========================================================
        // Semester & Student Record Lookup
        // =========================================================
        var studentRecord = _db.Students
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == request.UserId && (x.IsDelete == false || x.IsDelete == null));

        var targetSemester = _db.Semesters
            .AsNoTracking()
            .FirstOrDefault(x => x.SemesterName == request.academic_year_level && x.IsDelete == false);

        int targetSeq = targetSemester?.Sequence ?? 1;

        // Check if student has FAILED this target semester
        bool isSemesterFailed = false;
        if (studentRecord != null)
        {
            var semResults = new string?[]
            {
                studentRecord.Sem1_Result, studentRecord.Sem2_Result, studentRecord.Sem3_Result,
                studentRecord.Sem4_Result, studentRecord.Sem5_Result, studentRecord.Sem6_Result,
                studentRecord.Sem7_Result, studentRecord.Sem8_Result, studentRecord.Sem9_Result
            };

            if (targetSeq >= 1 && targetSeq <= 9)
            {
                var semRes = semResults[targetSeq - 1];
                if (string.Equals(semRes, "Fail", StringComparison.OrdinalIgnoreCase))
                {
                    isSemesterFailed = true;
                }
            }
        }

        // Also check if any completed past registration of this user for this semester was graded and failed
        if (!isSemesterFailed)
        {
            var pastUserRegIds = _db.StudentRegistrations
                .AsNoTracking()
                .Where(x => (x.IsDelete == false || x.IsDelete == null) &&
                            x.AcademicYearLevel == request.academic_year_level &&
                            (isNewStudent ? x.NewStudentAccId == request.NewStudentAccId : x.UserId == request.UserId))
                .Select(x => x.RegistrationId)
                .ToList();

            if (pastUserRegIds.Any())
            {
                var latestRegId = pastUserRegIds.Last();
                var pastResults = _db.StudentSubjectResults
                    .AsNoTracking()
                    .Where(r => r.RegistrationId == latestRegId && r.SubjectId.HasValue && r.SubjectId > 0)
                    .ToList();

                if (pastResults.Any() && pastResults.All(r => !string.IsNullOrEmpty(r.Grade)))
                {
                    int total = pastResults.Count;
                    int pass = pastResults.Count(r => r.IsPass);
                    if (total > 0 && pass <= total / 2.0)
                    {
                        isSemesterFailed = true;
                    }
                }
            }
        }

        // =========================================================
        // Duplicate Registration Validation
        // =========================================================
        var existingRegQuery = _db.StudentRegistrations
            .AsNoTracking()
            .Where(x => (x.IsDelete == false || x.IsDelete == null) &&
                        x.AcademicYearRange == request.academic_year_range &&
                        x.AcademicYearLevel == request.academic_year_level &&
                        (x.Status == null || x.Status != "Rejected"));

        var existingUserReg = isNewStudent
            ? existingRegQuery.Where(x => x.NewStudentAccId == request.NewStudentAccId)
            : existingRegQuery.Where(x => x.UserId == request.UserId);

        // If the student failed this semester, they are allowed to repeat/re-register!
        // Only block if they haven't failed (i.e. already enrolled and pending/approved/passed).
        if (!isSemesterFailed && existingUserReg.Any())
        {
            return BadRequest(new StudentRegistrationResponseModel
            {
                IsSuccess = false,
                Message = $"သင်သည် {request.academic_year_range} ပညာသင်နှစ်အတွက် {request.academic_year_level} သို့ ကျောင်းအပ်နှံထားပြီးဖြစ်ပါသည်။ ထပ်မံကျောင်းအပ်၍ မရပါ။"
            });
        }

        // RollNo validation (Same Roll No cannot be registered by ANOTHER person in the same semester)
        if (!string.IsNullOrWhiteSpace(request.roll_no))
        {
            var existingRollNoReg = isNewStudent
                ? existingRegQuery.Where(x => x.RollNo == request.roll_no && x.NewStudentAccId != request.NewStudentAccId)
                : existingRegQuery.Where(x => x.RollNo == request.roll_no && x.UserId != request.UserId);

            if (existingRollNoReg.Any())
            {
                return BadRequest(new StudentRegistrationResponseModel
                {
                    IsSuccess = false,
                    Message = $"သင်ဖြည့်သွင်းထားသော ခုံအမှတ် (Roll No - {request.roll_no}) သည် ဤ Semester တွင် အခြားသူတစ်ဦးမှ အသုံးပြုထားပြီးဖြစ်ပါသည်။"
                });
            }
        }

        // =========================================================
        // Semester Progression Validation
        // =========================================================
        if (studentRecord != null && !string.IsNullOrWhiteSpace(request.academic_year_level))
        {
            if (targetSemester?.Sequence != null)
            {
                // Collect results array; index 0 = Sem1, index 8 = Sem9
                var semResults = new string?[]
                {
                    studentRecord.Sem1_Result, studentRecord.Sem2_Result, studentRecord.Sem3_Result,
                    studentRecord.Sem4_Result, studentRecord.Sem5_Result, studentRecord.Sem6_Result,
                    studentRecord.Sem7_Result, studentRecord.Sem8_Result, studentRecord.Sem9_Result
                };

                // Determine allowed next semester:
                // Find last 'Fail' result; that is the retake semester.
                // Otherwise allowed = highestPassed + 1.
                int highestPassed = 0;
                int? failedSemSeq = null;
                for (int i = 0; i < semResults.Length; i++)
                {
                    int seq = i + 1;
                    if (string.Equals(semResults[i], "Pass", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(semResults[i], "Credit_Transferred", StringComparison.OrdinalIgnoreCase))
                    {
                        highestPassed = seq;
                    }
                    else if (string.Equals(semResults[i], "Fail", StringComparison.OrdinalIgnoreCase))
                    {
                        // Student must retake the first failed semester above passed ones
                        if (failedSemSeq == null)
                        {
                            failedSemSeq = seq;
                        }
                    }
                }

                int allowedSeq = failedSemSeq ?? (highestPassed + 1);

                // If student already passed all 9 semesters, block any new registration
                if (highestPassed >= 9 && failedSemSeq == null)
                {
                    return BadRequest(new StudentRegistrationResponseModel
                    {
                        IsSuccess = false,
                        Message = "ကျောင်းသားသည် Semester ၁ မှ ၉ အထိ အားလုံး Pass ဖြေဆိုပြီးဖြစ်သောကြောင့် ကျောင်းအပ်ဖောင် တင်သွင်း၍ မရတော့ပါ။"
                    });
                }

                if (targetSeq != allowedSeq)
                {
                    string reason;
                    if (targetSeq < allowedSeq)
                    {
                        reason = $"Semester {targetSeq} ကို ယခင်က Pass သတ်မှတ်ပြီးဖြစ်သောကြောင့် ထပ်မံ တင်သွင်း၍ မရပါ။ Semester {allowedSeq} သာ တင်သွင်းနိုင်ပါသည်။";
                    }
                    else
                    {
                        reason = $"Semester {allowedSeq} ကို ဦးစွာ Pass ရမည်ဖြစ်ပြီး Semester {targetSeq} ကို ကျော်လိုက်၍ မရပါ။";
                    }

                    return BadRequest(new StudentRegistrationResponseModel
                    {
                        IsSuccess = false,
                        Message = reason
                    });
                }
            }
        }
        // =========================================================

        // =========================================================
        // Email Format Validation (Regex only, no live DNS lookup)
        // =========================================================
        if (!string.IsNullOrWhiteSpace(request.email))
        {
            if (!Regex.IsMatch(request.email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Email ပုံစံမမှန်ကန်ပါ။" });
            }
        }

        // =========================================================
        // Phone Number Validation (Sanitize spaces, dashes & digits)
        // =========================================================
        if (string.IsNullOrWhiteSpace(request.app_student_phone))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းသားဖုန်းနံပါတ် ဖြည့်သွင်းရန် လိုအပ်ပါသည်။" });
        }

        string phone = request.app_student_phone.Trim().Replace(" ", "").Replace("-", "");

        if (!Regex.IsMatch(phone, @"^09\d{7,10}$"))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "မြန်မာဖုန်းနံပါတ်သည် '09' ဖြင့် စတင်ရပါမည်။" });
        }

        // =========================================================
        // Gender Relation Validation (Optional)
        // =========================================================
        var allowedGenders = new[] { "ကျား", "မ", "မောင်", "ဦး", "ဒေါ်" };
        if (!string.IsNullOrWhiteSpace(request.gender_relation) && !allowedGenders.Contains(request.gender_relation))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Gender Relation ပုံစံ မှားယွင်းနေပါသည်။" });
        }

        // =========================================================
        // NRC Validation (Flexible & Safe)
        // =========================================================
        string fullNrcNo = request.student_nrc_no ?? "-";
        var nrcType = string.IsNullOrWhiteSpace(request.nrc_type) ? "(နိုင်)" : request.nrc_type.Trim();
        if (!string.IsNullOrWhiteSpace(request.nrc_state) && !string.IsNullOrWhiteSpace(request.nrc_township) && !string.IsNullOrWhiteSpace(request.nrc_number))
        {
            fullNrcNo = $"{request.nrc_state}/{request.nrc_township}{nrcType}{request.nrc_number}";
        }

        // =========================================================
        // Roll No & Blood Type Validation (Auto-Uppercase Roll No)
        // =========================================================
        if (!string.IsNullOrWhiteSpace(request.roll_no))
        {
            request.roll_no = request.roll_no.Trim().ToUpper();
        }

        var allowedBloodTypes = new[] { "A", "B", "AB", "O", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        if (!string.IsNullOrWhiteSpace(request.blood_type) && !allowedBloodTypes.Contains(request.blood_type.ToUpper()))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "သွေးအမျိုးအစား မှားယွင်းနေပါသည်။" });
        }

        // --- (င) 📷 Images Upload handling ---
        string studentImagePath = request.StudentImageFile != null ? "" : (request.student_image ?? "");
        string signatureImagePath = "";
        string nrcFrontImagePath = request.nrc_front_image ?? "";
        string nrcBackImagePath = request.nrc_back_image ?? "";
        string censusImagePath = request.census_image ?? "";
        string fatherNrcFrontImagePath = request.father_nrc_front_image ?? "";
        string fatherNrcBackImagePath = request.father_nrc_back_image ?? "";
        string motherNrcFrontImagePath = request.mother_nrc_front_image ?? "";
        string motherNrcBackImagePath = request.mother_nrc_back_image ?? "";
        string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");

        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

        if (request.StudentImageFile != null && request.StudentImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.StudentImageFile.FileName).ToLower();
            string fileName = $"std_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.StudentImageFile.CopyTo(stream);
            }
            studentImagePath = $"/uploads/students/{fileName}";
        }

        if (request.SignatureImageFile != null && request.SignatureImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.SignatureImageFile.FileName).ToLower();
            string fileName = $"sig_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.SignatureImageFile.CopyTo(stream);
            }
            signatureImagePath = $"/uploads/students/{fileName}";
        }

        if (request.NrcFrontImageFile != null && request.NrcFrontImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.NrcFrontImageFile.FileName).ToLower();
            string fileName = $"nrc_f_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.NrcFrontImageFile.CopyTo(stream);
            }
            nrcFrontImagePath = $"/uploads/students/{fileName}";
        }

        if (request.NrcBackImageFile != null && request.NrcBackImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.NrcBackImageFile.FileName).ToLower();
            string fileName = $"nrc_b_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.NrcBackImageFile.CopyTo(stream);
            }
            nrcBackImagePath = $"/uploads/students/{fileName}";
        }

        if (request.CensusImageFile != null && request.CensusImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.CensusImageFile.FileName).ToLower();
            string fileName = $"census_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.CensusImageFile.CopyTo(stream);
            }
            censusImagePath = $"/uploads/students/{fileName}";
        }

        if (request.FatherNrcFrontImageFile != null && request.FatherNrcFrontImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.FatherNrcFrontImageFile.FileName).ToLower();
            string fileName = $"fnrc_f_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.FatherNrcFrontImageFile.CopyTo(stream);
            }
            fatherNrcFrontImagePath = $"/uploads/students/{fileName}";
        }

        if (request.FatherNrcBackImageFile != null && request.FatherNrcBackImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.FatherNrcBackImageFile.FileName).ToLower();
            string fileName = $"fnrc_b_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.FatherNrcBackImageFile.CopyTo(stream);
            }
            fatherNrcBackImagePath = $"/uploads/students/{fileName}";
        }

        if (request.MotherNrcFrontImageFile != null && request.MotherNrcFrontImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.MotherNrcFrontImageFile.FileName).ToLower();
            string fileName = $"mnrc_f_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.MotherNrcFrontImageFile.CopyTo(stream);
            }
            motherNrcFrontImagePath = $"/uploads/students/{fileName}";
        }

        if (request.MotherNrcBackImageFile != null && request.MotherNrcBackImageFile.Length > 0)
        {
            string ext = Path.GetExtension(request.MotherNrcBackImageFile.FileName).ToLower();
            string fileName = $"mnrc_b_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
            using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                request.MotherNrcBackImageFile.CopyTo(stream);
            }
            motherNrcBackImagePath = $"/uploads/students/{fileName}";
        }

        // Fallback missing images from StudentPersonalInfo if not uploaded
        if (request.UserId.HasValue || !string.IsNullOrEmpty(request.roll_no))
        {
            var pInfo = _db.StudentPersonalInfos
                .AsNoTracking()
                .FirstOrDefault(p => (request.UserId.HasValue && p.UserId == request.UserId.Value) || (!string.IsNullOrEmpty(request.roll_no) && p.roll_no == request.roll_no));

            if (pInfo != null)
            {
                if (string.IsNullOrEmpty(studentImagePath)) studentImagePath = pInfo.student_image ?? "";
                if (string.IsNullOrEmpty(nrcFrontImagePath)) nrcFrontImagePath = pInfo.nrc_front_image ?? "";
                if (string.IsNullOrEmpty(nrcBackImagePath)) nrcBackImagePath = pInfo.nrc_back_image ?? "";
                if (string.IsNullOrEmpty(censusImagePath)) censusImagePath = pInfo.census_image ?? "";
                if (string.IsNullOrEmpty(fatherNrcFrontImagePath)) fatherNrcFrontImagePath = pInfo.father_nrc_front_image ?? "";
                if (string.IsNullOrEmpty(fatherNrcBackImagePath)) fatherNrcBackImagePath = pInfo.father_nrc_back_image ?? "";
                if (string.IsNullOrEmpty(motherNrcFrontImagePath)) motherNrcFrontImagePath = pInfo.mother_nrc_front_image ?? "";
                if (string.IsNullOrEmpty(motherNrcBackImagePath)) motherNrcBackImagePath = pInfo.mother_nrc_back_image ?? "";
            }

            if (string.IsNullOrEmpty(studentImagePath) && request.UserId.HasValue)
            {
                var studentObj = _db.Students.AsNoTracking().FirstOrDefault(s => s.UserId == request.UserId.Value);
                if (studentObj != null && !string.IsNullOrEmpty(studentObj.StudentImage))
                {
                    studentImagePath = studentObj.StudentImage;
                }
            }
        }

        // --- (စ) DB ထဲသို့ ဒေတာထည့်သွင်းခြင်း ---
        int? validUserId = null;
        if (request.UserId.HasValue && request.UserId.Value > 0)
        {
            var userExists = _db.Users.Any(u => u.UserId == request.UserId.Value);
            if (userExists)
            {
                validUserId = request.UserId.Value;
            }
        }

        int? validNewStudentAccId = null;
        if (request.NewStudentAccId.HasValue && request.NewStudentAccId.Value > 0)
        {
            var accExists = _db.NewStudentAccs.Any(a => a.NewStudentAccId == request.NewStudentAccId.Value);
            if (accExists)
            {
                validNewStudentAccId = request.NewStudentAccId.Value;
            }
        }

        var newReg = new StudentRegistration
        {
            UserId = validUserId,
            NewStudentAccId = validNewStudentAccId,
            AdmissionSerialNo = request.AdmissionSerialNo,
            AcademicYearRange = request.academic_year_range ?? "",
            AcademicYearLevel = request.academic_year_level ?? "",
            Major = request.major ?? "",
            RollNo = request.roll_no,
            UniversityRegNo = request.university_reg_no,
            AdmissionYear = request.admission_year,
            ApplicationDate = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            StudentNameMm = request.student_name_mm ?? "",
            StudentNameEn = request.student_name_en ?? "",
            MotherName = request.mother_name ?? "",
            FatherName = request.father_name ?? "",
            GenderRelation = request.gender_relation ?? "",
            Ethnicity = request.ethnicity ?? "",
            Religion = request.religion ?? "",
            Pob = request.pob ?? "",
            BirthPlaceRegion = request.birth_place_region ?? "",
            StudentNrcNo = fullNrcNo ?? "",
            NationalityStatus = request.nationality_status ?? "",
            Dob = request.dob != default ? DateOnly.FromDateTime(request.dob) : DateOnly.FromDateTime(DateTime.Today.AddYears(-18)),
            Email = request.email,
            BloodType = string.IsNullOrEmpty(request.blood_type) ? "" : request.blood_type.ToUpper(),
            CovidVaccineStatus = request.covid_vaccine_status,
            CurrentAddress = request.current_address,
            PermanentAddressMm = request.permanent_address_mm ?? "",
            PermanentAddressEn = request.permanent_address_en ?? "",
            MatricRollNo = request.matric_roll_no ?? "",
            MatricPassedYear = request.matric_passed_year,
            ExamCenter = request.exam_center ?? "",
            FatherOccupation = request.father_occupation,
            MotherOccupation = request.mother_occupation,
            PastExamMajor = request.past_exam_major,
            PastExamRollNo = request.past_exam_roll_no,
            PastExamYear = request.past_exam_year,
            PastExamStatus = request.past_exam_status,
            PreviousYearRollNo = request.previous_year_roll_no,
            GuardianName = request.guardian_name,
            GuardianRelationship = request.guardian_relationship,
            GuardianOccupation = request.guardian_occupation,
            GuardianAddressPhone = request.guardian_address_phone,
            AppGuardianName = request.app_guardian_name,
            AppGuardianNrc = request.app_guardian_nrc,
            AppGuardianPhone = request.app_guardian_phone,
            AppGuardianAddress = request.app_guardian_address,
            AppStudentName = request.app_student_name,
            AppStudentPhone = request.app_student_phone,
            StipendRequested = request.stipend_requested ?? false,
            Status = PendingConfirmationStatus,
            StudentImage = studentImagePath,
            SignatureImage = signatureImagePath,
            NrcFrontImage = nrcFrontImagePath,
            NrcBackImage = nrcBackImagePath,
            CensusImage = censusImagePath,
            FatherNrcFrontImage = fatherNrcFrontImagePath,
            FatherNrcBackImage = fatherNrcBackImagePath,
            MotherNrcFrontImage = motherNrcFrontImagePath,
            MotherNrcBackImage = motherNrcBackImagePath,
            CreatedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = string.IsNullOrEmpty(request.created_by) ? "System" : request.created_by,
            IsDelete = false
        };

        try
        {
            _db.StudentRegistrations.Add(newReg);
            int result = _db.SaveChanges();

            if (result > 0 && !string.IsNullOrWhiteSpace(request.selected_subject_ids))
            {
                int? realStudentId = null;
                if (newReg.UserId.HasValue && newReg.UserId.Value > 0)
                {
                    var studentRec = _db.Students.FirstOrDefault(s => s.UserId == newReg.UserId.Value);
                    if (studentRec != null && studentRec.StudentId > 0)
                    {
                        realStudentId = studentRec.StudentId;
                    }
                }

                var subIdStrings = request.selected_subject_ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var subIdStr in subIdStrings)
                {
                    if (int.TryParse(subIdStr, out int subId) && subId > 0)
                    {
                        var subjectObj = _db.Subjects.FirstOrDefault(s => s.SubjectId == subId && (s.IsDelete == false || s.IsDelete == null));
                        if (subjectObj != null)
                        {
                            var newResult = new StudentSubjectResult
                            {
                                RegistrationId = newReg.RegistrationId,
                                StudentId = realStudentId,
                                SubjectId = subId,
                                SemesterId = subjectObj.SemesterId > 0 ? subjectObj.SemesterId : (int?)null,
                                Grade = null,
                                IsPass = false,
                                IsDisqualified = false,
                                AttemptNumber = 1,
                                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                                CreatedBy = string.IsNullOrEmpty(request.created_by) ? "System" : request.created_by
                            };
                            _db.StudentSubjectResults.Add(newResult);
                        }
                    }
                }
                _db.SaveChanges();

                // Sync StudentPersonalInfo and Student table
                if (!string.IsNullOrWhiteSpace(newReg.Major) && newReg.Major != "-")
                {
                    StudentPersonalInfo? pInfo = null;
                    if (newReg.UserId.HasValue && newReg.UserId.Value > 0)
                        pInfo = _db.StudentPersonalInfos.FirstOrDefault(p => p.UserId == newReg.UserId.Value);
                    else if (newReg.NewStudentAccId.HasValue && newReg.NewStudentAccId.Value > 0)
                        pInfo = _db.StudentPersonalInfos.FirstOrDefault(p => p.NewStudentAccId == newReg.NewStudentAccId.Value);

                    if (pInfo != null)
                    {
                        pInfo.major = newReg.Major;
                        if (!string.IsNullOrWhiteSpace(newReg.RollNo)) pInfo.roll_no = newReg.RollNo;
                        _db.StudentPersonalInfos.Update(pInfo);
                    }

                    if (newReg.UserId.HasValue && newReg.UserId.Value > 0)
                    {
                        var studentObj = _db.Students.FirstOrDefault(s => s.UserId == newReg.UserId.Value && (s.IsDelete == false || s.IsDelete == null));
                        if (studentObj != null)
                        {
                            studentObj.CurrentMajor = newReg.Major;
                            if (!string.IsNullOrWhiteSpace(newReg.RollNo)) studentObj.CurrentRollNo = newReg.RollNo;
                            if (!string.IsNullOrWhiteSpace(newReg.AcademicYearLevel)) studentObj.CurrentClassYear = newReg.AcademicYearLevel;
                            _db.Students.Update(studentObj);
                        }
                    }
                    _db.SaveChanges();
                }
            }

            return StatusCode(201, new StudentRegistrationResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ကျောင်းအပ်ဖောင် တင်သွင်းခြင်း အောင်မြင်ပါသည်။" : "တင်သွင်းမှု မအောင်မြင်ပါ။",
                Data = new
                {
                    id = newReg.RegistrationId,
                    registrationId = newReg.RegistrationId,
                    userId = newReg.UserId,
                    status = newReg.Status,
                    canProceedToPayment = false
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new StudentRegistrationResponseModel
            {
                IsSuccess = false,
                Message = $"ကျောင်းအပ်ဖောင် သိမ်းဆည်းရာတွင် အမှားဖြစ်ပေါ်ပါသည်: {ex.InnerException?.Message ?? ex.Message}"
            });
        }
    }

    // ၄။ PUT: ဖောင်အချက်အလက် ပြင်ရန် (Update)
    [HttpPut("{id}")]
    [Permission("StudentRegistrations.Edit")]
    public IActionResult UpdateRegistration(int id, [FromForm] StudentRegistrationUpdateRequestModel request)
    {
        var item = _db.StudentRegistrations
            .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ဖောင်ကို ရှာမတွေ့ပါ။" });
        }

        item.AcademicYearRange = request.academic_year_range;
        item.AcademicYearLevel = request.academic_year_level;
        item.Major = request.major;
        item.RollNo = request.roll_no;
        item.StudentNameMm = request.student_name_mm;
        item.StudentNameEn = request.student_name_en;
        item.Email = request.email;
        item.ModifiedBy = request.modified_by;
        item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        if (!string.IsNullOrWhiteSpace(request.major) && request.major != "-")
        {
            StudentPersonalInfo? pInfo = null;
            if (item.UserId.HasValue && item.UserId.Value > 0)
                pInfo = _db.StudentPersonalInfos.FirstOrDefault(p => p.UserId == item.UserId.Value);
            else if (item.NewStudentAccId.HasValue && item.NewStudentAccId.Value > 0)
                pInfo = _db.StudentPersonalInfos.FirstOrDefault(p => p.NewStudentAccId == item.NewStudentAccId.Value);

            if (pInfo != null)
            {
                pInfo.major = request.major;
                if (!string.IsNullOrWhiteSpace(request.roll_no)) pInfo.roll_no = request.roll_no;
                _db.StudentPersonalInfos.Update(pInfo);
            }

            if (item.UserId.HasValue && item.UserId.Value > 0)
            {
                var studentObj = _db.Students.FirstOrDefault(s => s.UserId == item.UserId.Value && (s.IsDelete == false || s.IsDelete == null));
                if (studentObj != null)
                {
                    studentObj.CurrentMajor = request.major;
                    if (!string.IsNullOrWhiteSpace(request.roll_no)) studentObj.CurrentRollNo = request.roll_no;
                    if (!string.IsNullOrWhiteSpace(request.academic_year_level)) studentObj.CurrentClassYear = request.academic_year_level;
                    _db.Students.Update(studentObj);
                }
            }
        }

        int result = _db.SaveChanges();
        return Ok(new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = "ဖောင်အချက်အလက် ပြင်ဆင်ပြီးပါပြီ။" });
    }

    // ၅။ DELETE: ဖောင်ကို ဖျက်ရန် (Soft Delete)
    [HttpDelete("{id}")]
    [Permission("StudentRegistrations.Delete")]
    public IActionResult DeleteRegistration(int id)
    {
        var item = _db.StudentRegistrations
            .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ဖျက်မည့် ဖောင်ကို ရှာမတွေ့ပါ။" });
        }

        item.IsDelete = true;
        int result = _db.SaveChanges();

        return Ok(new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = "ဖောင်ကို ဖျက်သိမ်းပြီးပါပြီ။" });
    }

    // ၆။ GET: Roll No ဖြင့် အချက်အလက်ဟောင်း အားလုံး Auto ရှာရန် (Special API - All Fields)
    [HttpGet("search-past-student")]
    [Permission("StudentRegistrations.View")]
    public IActionResult SearchPastStudent([FromQuery] string rollNo)
    {
        if (string.IsNullOrEmpty(rollNo))
        {
            return BadRequest(new { message = "Roll No ထည့်သွင်းပေးရန် လိုအပ်သည်။" });
        }

        var student = _db.StudentRegistrations
            .Where(x => x.RollNo == rollNo.ToUpper() && (x.IsDelete == false || x.IsDelete == null))
            .OrderByDescending(x => x.RegistrationId)
            .FirstOrDefault();

        if (student is null)
        {
            return NotFound(new { message = "ဤ Roll No ဖြင့် ကျောင်းသားမှတ်တမ်း ဟောင်းမရှိပါ။" });
        }

        return Ok(new
        {
            userId = student.UserId,
            admissionSerialNo = student.AdmissionSerialNo,
            academicYearRange = student.AcademicYearRange,
            academicYearLevel = student.AcademicYearLevel,
            major = student.Major,
            rollNo = student.RollNo,
            universityRegNo = student.UniversityRegNo,
            admissionYear = student.AdmissionYear,
            studentNameMm = student.StudentNameMm,
            studentNameEn = student.StudentNameEn,
            motherName = student.MotherName,
            fatherName = student.FatherName,
            genderRelation = student.GenderRelation,
            ethnicity = student.Ethnicity,
            religion = student.Religion,
            pob = student.Pob,
            birthPlaceRegion = student.BirthPlaceRegion,
            studentNrcNo = student.StudentNrcNo,
            nationalityStatus = student.NationalityStatus,
            dob = student.Dob,
            email = student.Email,
            bloodType = student.BloodType,
            covidVaccineStatus = student.CovidVaccineStatus,
            currentAddress = student.CurrentAddress,
            permanentAddressMm = student.PermanentAddressMm,
            permanentAddressEn = student.PermanentAddressEn,
            matricRollNo = student.MatricRollNo,
            matricPassedYear = student.MatricPassedYear,
            examCenter = student.ExamCenter,
            fatherOccupation = student.FatherOccupation,
            motherOccupation = student.MotherOccupation,
            pastExamMajor = student.PastExamMajor,
            pastExamRollNo = student.PastExamRollNo,
            pastExamYear = student.PastExamYear,
            pastExamStatus = student.PastExamStatus,
            previousYearRollNo = student.PreviousYearRollNo,
            guardianName = student.GuardianName,
            guardianRelationship = student.GuardianRelationship,
            guardianOccupation = student.GuardianOccupation,
            guardianAddressPhone = student.GuardianAddressPhone,
            appGuardianName = student.AppGuardianName,
            appGuardianNrc = student.AppGuardianNrc,
            appGuardianPhone = student.AppGuardianPhone,
            appGuardianAddress = student.AppGuardianAddress,
            appStudentName = student.AppStudentName,
            appStudentPhone = student.AppStudentPhone,
            stipendRequested = student.StipendRequested
        });
    }

    [HttpGet("majors")]
    [Permission("StudentRegistrations.View")]
    public IActionResult GetMajors()
    {
        var majors = new List<string> { "Computer Science", "Computer Technology", "Information Technology" };
        return Ok(majors);
    }

    // ၈။ GET: ကျောင်းသား Registration ကို UserId ဖြင့် ရှာရန် (Payment အတွက် Data ယူဖို့)
    [HttpGet("GetByUserId/{userId}")]
    [AllowAnonymous]
    public IActionResult GetRegistrationByUserId(int userId)
    {
        var registration = _db.StudentRegistrations
            .AsNoTracking()
            .Where(x => x.UserId == userId && (x.IsDelete == false || x.IsDelete == null))
            .OrderByDescending(x => x.RegistrationId)
            .FirstOrDefault();

        if (registration is null)
        {
            return NotFound(new StudentRegistrationResponseModel
            {
                IsSuccess = false,
                Message = "ဤအသုံးပြုသူအတွက် မှတ်ပုံတင်ခြင်း မှတ်တမ်းမရှိပါ။"
            });
        }

        return Ok(new StudentRegistrationResponseModel
        {
            IsSuccess = true,
            Message = "ကျောင်းအပ်ဖောင် အချက်အလက်များ အောင်မြင်စွာ ရှာတွေ့ပါသည်။",
            Data = new
            {
                registrationId = registration.RegistrationId,
                userId = registration.UserId,
                student_name_mm = registration.StudentNameMm ?? "",
                student_name_en = registration.StudentNameEn ?? "",
                academic_year_level = registration.AcademicYearLevel ?? "",
                previous_year_roll_no = registration.PreviousYearRollNo ?? "",
                roll_no = registration.RollNo ?? "",
                major = registration.Major ?? "",
                admission_year = registration.AdmissionYear,
                created_datetime = registration.CreatedDatetime,
                status = NormalizeRegistrationStatus(registration.Status),
                canProceedToPayment = CanProceedToPayment(registration.Status)
            }
        });
    }

    // ၇။ PATCH: ဖောင်ကို Approved / Rejected လုပ်ရန်
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> PatchStatus(int id, [FromBody] StudentRegistrationStatusPatchModel request)
    {
        var item = _db.StudentRegistrations
            .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
        }

        var requestedStatus = NormalizeRegistrationStatus(request.Status);
        var allowedStatuses = new[] { PendingConfirmationStatus, ApprovedStatus, RejectedStatus };
        if (!allowedStatuses.Contains(requestedStatus, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Status ပြောင်းလဲမှုပုံစံ မှားယွင်းနေပါသည်။" });
        }

        string? generatedUsername = null;
        string? plainPassword = null;

        if (string.Equals(requestedStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            // If student doesn't have an account in User table yet, auto-create one with Student role (RoleId = 3)
            if (!item.UserId.HasValue || item.UserId.Value <= 0)
            {
                var existingUser = !string.IsNullOrWhiteSpace(item.Email)
                    ? await _db.Users.FirstOrDefaultAsync(u => u.Email == item.Email && u.IsDelete == false)
                    : null;

                if (existingUser != null)
                {
                    item.UserId = existingUser.UserId;
                }
                else
                {
                    // Auto-generate username from FullName (all lowercase, spaces replaced by underscore)
                    string namePart = !string.IsNullOrWhiteSpace(item.StudentNameEn) ? item.StudentNameEn : (item.StudentNameMm ?? "student");
                    string baseUsername = GenerateUsername(namePart);

                    string finalUsername = baseUsername;
                    int suffix = 1;
                    while (await _db.Users.AnyAsync(u => u.UserName == finalUsername && u.IsDelete == false))
                    {
                        finalUsername = $"{baseUsername}_{suffix}";
                        suffix++;
                    }

                    // Auto-generate secure password
                    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
                    var rng = new Random();
                    plainPassword = "SC@" + new string(Enumerable.Range(0, 8).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

                    var newUser = new User
                    {
                        RoleId = 3, // Student role (shows in User Management)
                        FullName = !string.IsNullOrWhiteSpace(item.StudentNameMm) ? item.StudentNameMm : (item.StudentNameEn ?? "Student"),
                        UserName = finalUsername,
                        RoleNo = item.RollNo,
                        Email = item.Email,
                        Password = hashedPassword,
                        MustChangePassword = true,
                        Status = "Active",
                        IsDelete = false,
                        CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                        CreatedBy = request.modified_by ?? "Admin"
                    };

                    _db.Users.Add(newUser);
                    await _db.SaveChangesAsync();

                    item.UserId = newUser.UserId;
                    generatedUsername = finalUsername;

                    // Also create or link Student record in Student table
                    var existingStudent = await _db.Students.FirstOrDefaultAsync(s => s.UserId == newUser.UserId);
                    if (existingStudent == null)
                    {
                        var student = new Student
                        {
                            UserId = newUser.UserId,
                            CurrentRollNo = !string.IsNullOrWhiteSpace(newUser.RoleNo) ? newUser.RoleNo : item.RollNo,
                            CurrentClassYear = item.AcademicYearLevel ?? "First Year",
                            CurrentMajor = item.Major ?? "N/A",
                            StudentImage = item.StudentImage,
                            NrcFrontImage = item.NrcFrontImage,
                            NrcBackImage = item.NrcBackImage,
                            CensusImage = item.CensusImage,
                            FatherNrcFrontImage = item.FatherNrcFrontImage,
                            FatherNrcBackImage = item.FatherNrcBackImage,
                            MotherNrcFrontImage = item.MotherNrcFrontImage,
                            MotherNrcBackImage = item.MotherNrcBackImage,
                            Status = "Active",
                            IsDelete = false,
                            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                            CreatedBy = request.modified_by ?? "Admin"
                        };
                        _db.Students.Add(student);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.StudentImage)) existingStudent.StudentImage = item.StudentImage;
                        if (!string.IsNullOrEmpty(item.NrcFrontImage)) existingStudent.NrcFrontImage = item.NrcFrontImage;
                        if (!string.IsNullOrEmpty(item.NrcBackImage)) existingStudent.NrcBackImage = item.NrcBackImage;
                        if (!string.IsNullOrEmpty(item.CensusImage)) existingStudent.CensusImage = item.CensusImage;
                        if (!string.IsNullOrEmpty(item.FatherNrcFrontImage)) existingStudent.FatherNrcFrontImage = item.FatherNrcFrontImage;
                        if (!string.IsNullOrEmpty(item.FatherNrcBackImage)) existingStudent.FatherNrcBackImage = item.FatherNrcBackImage;
                        if (!string.IsNullOrEmpty(item.MotherNrcFrontImage)) existingStudent.MotherNrcFrontImage = item.MotherNrcFrontImage;
                        if (!string.IsNullOrEmpty(item.MotherNrcBackImage)) existingStudent.MotherNrcBackImage = item.MotherNrcBackImage;
                        await _db.SaveChangesAsync();
                    }

                    // Send approval and credentials email
                    if (!string.IsNullOrWhiteSpace(item.Email) && !string.IsNullOrWhiteSpace(plainPassword))
                    {
                        try
                        {
                            string subject = "Polytechnic University Maubin - Registration Approved & Login Details";
                            string htmlBody = BuildRegistrationApprovalEmail(newUser.FullName, finalUsername, plainPassword);
                            _ = _emailService.SendEmailAsync(item.Email, newUser.FullName, subject, htmlBody);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Email Error] Failed to send registration credentials email: {ex.Message}");
                        }
                    }
                }
            }
        }

        item.Status = requestedStatus;
        item.ModifiedBy = request.modified_by;
        item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = await _db.SaveChangesAsync();
        return Ok(new StudentRegistrationResponseModel
        {
            IsSuccess = result > 0,
            Message = generatedUsername != null
                ? $"ကျောင်းအပ်ဖောင်ကို Approved ပြုလုပ်ပြီးပါပြီ။ User Management တွင် Student အကောင့် ဖန်တီး၍ Username '{generatedUsername}' နှင့် Password ကို Email ဖြင့် ပေးပို့ပြီးပါပြီ။"
                : $"ကျောင်းအပ်ဖောင်ကို {requestedStatus} ပြုလုပ်ခြင်း အောင်မြင်ပါသည်။",
            Data = new
            {
                registrationId = item.RegistrationId,
                userId = item.UserId,
                status = item.Status,
                canProceedToPayment = CanProceedToPayment(item.Status)
            }
        });
    }

    private static string BuildRegistrationApprovalEmail(string name, string username, string password)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f0f4ff; margin: 0; padding: 20px; }}
    .card {{ background: #fff; border-radius: 16px; max-width: 520px; margin: auto; padding: 36px; box-shadow: 0 4px 24px rgba(37,99,235,0.1); }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .badge {{ display:inline-block; background: #10b981; color: white; border-radius: 50px; padding: 6px 18px; font-size: 0.85rem; font-weight:700; letter-spacing:0.05em; }}
    h2 {{ color: #1e3a8a; font-size: 1.4rem; margin: 12px 0 4px; }}
    p {{ color: #475569; line-height: 1.7; }}
    .cred-box {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 20px; margin: 20px 0; }}
    .cred-row {{ display: flex; justify-content: space-between; margin-bottom: 10px; }}
    .cred-label {{ color: #94a3b8; font-size: 0.85rem; font-weight: 600; }}
    .cred-val {{ color: #0f172a; font-weight: 700; font-family: monospace; font-size: 1rem; }}
    .footer {{ text-align: center; margin-top: 24px; color: #94a3b8; font-size: 0.8rem; }}
    .warn {{ background: #fef3c7; border-radius: 8px; padding: 12px 16px; color: #92400e; font-size: 0.85rem; margin-top: 16px; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='header'>
      <span class='badge'>✓ ADMISSION APPROVED</span>
      <h2>Smart Campus Registration Approved</h2>
      <p>Polytechnic University Maubin (PUMUB)</p>
    </div>
    <p>Dear <strong>{name}</strong>,</p>
    <p>သင်၏ ကျောင်းအပ်နှံမှု လျှောက်ထားချက်ကို အောင်မြင်စွာ အတည်ပြုပြီးပါပြီ။ သင်၏ Student Portal သို့ ဝင်ရောက်ရန် အောက်ပါ အကောင့်အချက်အလက်များကို အသုံးပြုနိုင်ပါသည်-</p>
    <div class='cred-box'>
      <div class='cred-row'>
        <span class='cred-label'>Username</span>
        <span class='cred-val'>{username}</span>
      </div>
      <div class='cred-row'>
        <span class='cred-label'>Password</span>
        <span class='cred-val'>{password}</span>
      </div>
    </div>
    <div class='warn'>
      ⚠️ ကျေးဇူးပြု၍ ပထမဆုံးဝင်ချိန်တွင် Password ကို ချက်ချင်း ပြောင်းလဲပေးပါ။ ဤ ယာယီ Password ကို အခြားသူများအား မျှဝေခြင်း မပြုပါနှင့်။
    </div>
    <div class='footer'>
      © {DateTime.Now.Year} Smart Campus PUMUB &nbsp;·&nbsp; Polytechnic University Maubin
    </div>
  </div>
</body>
</html>";
    }

    private static string GenerateUsername(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "student";

        // Full Name အပြည့်အစုံကို အသေးစာလုံးပြောင်းပြီး space နေရာတွင် '_' ထည့်သွင်းခြင်း
        string lower = fullName.Trim().ToLowerInvariant();
        string replaced = System.Text.RegularExpressions.Regex.Replace(lower, @"\s+", "_");
        string cleaned = new string(replaced.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"_+", "_").Trim('_');

        return string.IsNullOrWhiteSpace(cleaned) ? "student" : cleaned;
    }
}


