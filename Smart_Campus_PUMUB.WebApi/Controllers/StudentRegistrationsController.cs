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

    public StudentRegistrationsController(SmartCampusDbContext db, IFacultyDataScopeService scopeService)
    {
        _db = db;
        _scopeService = scopeService;
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
        string studentImagePath = "";
        string signatureImagePath = "";
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

        // --- (စ) DB ထဲသို့ ဒေတာထည့်သွင်းခြင်း ---
        var newReg = new StudentRegistration
        {
            UserId = request.UserId,
            NewStudentAccId = request.NewStudentAccId,
            AdmissionSerialNo = request.AdmissionSerialNo,
            AcademicYearRange = request.academic_year_range,
            AcademicYearLevel = request.academic_year_level,
            Major = request.major,
            RollNo = request.roll_no,
            UniversityRegNo = request.university_reg_no,
            AdmissionYear = request.admission_year,
            ApplicationDate = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            StudentNameMm = request.student_name_mm,
            StudentNameEn = request.student_name_en,
            MotherName = request.mother_name,
            FatherName = request.father_name,
            GenderRelation = request.gender_relation,
            Ethnicity = request.ethnicity,
            Religion = request.religion,
            Pob = request.pob,
            BirthPlaceRegion = request.birth_place_region,
            StudentNrcNo = fullNrcNo, // 💡 Null ဖြစ်လျှင် Null အတိုင်း ဝင်သွားမည်
            NationalityStatus = request.nationality_status,
            Dob = DateOnly.FromDateTime(request.dob),
            Email = request.email,
            BloodType = string.IsNullOrEmpty(request.blood_type) ? null : request.blood_type.ToUpper(), // 💡 Fix: Null error မတက်အောင် ကာကွယ်ထားသည်
            CovidVaccineStatus = request.covid_vaccine_status,
            CurrentAddress = request.current_address,
            PermanentAddressMm = request.permanent_address_mm,
            PermanentAddressEn = request.permanent_address_en,
            MatricRollNo = request.matric_roll_no,
            MatricPassedYear = request.matric_passed_year,
            ExamCenter = request.exam_center,
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
            CreatedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = string.IsNullOrEmpty(request.created_by) ? "System" : request.created_by,
            IsDelete = false
        };

        _db.StudentRegistrations.Add(newReg);
        int result = _db.SaveChanges();

        if (result > 0 && !string.IsNullOrWhiteSpace(request.selected_subject_ids))
        {
            int? realStudentId = null;
            if (newReg.UserId.HasValue)
            {
                var studentRec = _db.Students.FirstOrDefault(s => s.UserId == newReg.UserId.Value);
                realStudentId = studentRec?.StudentId;
            }

            var subIdStrings = request.selected_subject_ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var subIdStr in subIdStrings)
            {
                if (int.TryParse(subIdStr, out int subId) && subId > 0)
                {
                    var subjectObj = _db.Subjects.FirstOrDefault(s => s.SubjectId == subId);
                    var newResult = new StudentSubjectResult
                    {
                        RegistrationId = newReg.RegistrationId,
                        StudentId = realStudentId,
                        SubjectId = subId,
                        SemesterId = subjectObj?.SemesterId,
                        Grade = null,
                        IsPass = false,
                        CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                        CreatedBy = string.IsNullOrEmpty(request.created_by) ? "System" : request.created_by
                    };
                    _db.StudentSubjectResults.Add(newResult);
                }
            }
            _db.SaveChanges();
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
    public IActionResult PatchStatus(int id, [FromBody] StudentRegistrationStatusPatchModel request)
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

        item.Status = requestedStatus;
        item.ModifiedBy = request.modified_by;
        item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();
        return Ok(new StudentRegistrationResponseModel
        {
            IsSuccess = result > 0,
            Message = $"ကျောင်းအပ်ဖောင်ကို {requestedStatus} ပြုလုပ်ခြင်း အောင်မြင်ပါသည်။",
            Data = new
            {
                registrationId = item.RegistrationId,
                userId = item.UserId,
                status = item.Status,
                canProceedToPayment = CanProceedToPayment(item.Status)
            }
        });
    }
}


