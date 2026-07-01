using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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

    public StudentRegistrationsController(SmartCampusDbContext db)
    {
        _db = db;
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
    public IActionResult GetRegistrationsPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? level = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.StudentRegistrations
            .AsNoTracking()
            .Include(x => x.RegistrationPayments)
            .Where(x => x.IsDelete == false || x.IsDelete == null);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(s => (s.RollNo != null && s.RollNo.Contains(searchTerm)) ||
                                   (s.StudentNameMm != null && s.StudentNameMm.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(level) && !level.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.AcademicYearLevel == level);
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x.RegistrationId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        foreach (var item in items)
        {
            item.Status = NormalizeRegistrationStatus(item.Status);
        }

        var result = new PagedResult<StudentRegistration>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(result);
    }

    // ₉. GET: UserId ဖြင့် နောက်ဆုံး Registration တစ်ခု ရယူရန် (Auto-Fill အတွက်)
    [HttpGet("latest/{userId}")]
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
    public IActionResult CreateRegistration([FromForm] StudentRegistrationCreateRequestModel request)
    {
        // 💡 Fix: UserId သည် int? ဖြစ်သွားသဖြင့် null စစ်ရန် ထည့်သွင်းထားသည်
        if (request.UserId == null || request.UserId <= 0)
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအိုင်ဒီ (UserId) ထည့်သွင်းရန် လိုအပ်သည်။" });
        }

        //var userCheck = _db.Users.FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);

        //if (userCheck is null)
        //{
        //    return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအကောင့်ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
        //}

        //if (userCheck.RoleId != 3)
        //{
        //    return BadRequest(new StudentRegistrationResponseModel
        //    {
        //        IsSuccess = false,
        //        Message = "ကျောင်းသားအကောင့်များသာ ကျောင်းအပ်ဖောင် တင်သွင်းခွင့်ရှိသည်။ (Admin သို့မဟုတ် Tutor အကောင့်များ တင်၍မရပါ)"
        //    });
        //}

        var userCheck = _db.Users
          .Include(x => x.Role)
          .FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);

        if (userCheck is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအကောင့်ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
        }

        // ၂။ RoleName ကို အခြေခံ၍ စစ်ဆေးပါ
        if (userCheck.Role?.RoleName != "Student")
        {
            return BadRequest(new StudentRegistrationResponseModel
            {
                IsSuccess = false,
                Message = "ကျောင်းသားအကောင့်များသာ ကျောင်းအပ်ဖောင် တင်သွင်းခွင့်ရှိသည်။"
            });
        }

        // =========================================================
        // Semester Progression Validation
        // =========================================================
        var studentRecord = _db.Students
            .AsNoTracking()
            .FirstOrDefault(x => x.UserId == request.UserId && (x.IsDelete == false || x.IsDelete == null));

        if (studentRecord != null && !string.IsNullOrWhiteSpace(request.academic_year_level))
        {
            // Find the target semester in the DB to get its Sequence number
            var targetSemester = _db.Semesters
                .AsNoTracking()
                .FirstOrDefault(x => x.SemesterName == request.academic_year_level && x.IsDelete == false);

            if (targetSemester?.Sequence != null)
            {
                int targetSeq = targetSemester.Sequence.Value;

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
                        reason = $"Semester {allowedSeq} ကို ဦးစွာ Pass ရပမည်မဟုတ်မနေ တင်သွင်းရမည်ဖြစ်ပြီး Semester {targetSeq} ကို ကျော်လိုက်၍ မရပါ။";
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

        if (!string.IsNullOrEmpty(request.email))
        {
            if (!Regex.IsMatch(request.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Email ပုံစံမမှန်ကန်ပါ။" });
            }

            try
            {
                string domain = request.email.Split('@')[1];
                var hostEntry = System.Net.Dns.GetHostEntry(domain);
                if (hostEntry.AddressList.Length == 0)
                {
                    return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "တည်ရှိခြင်းမရှိသော Email အတု ဖြစ်နေပါသည်။ (Domain ရှာမတွေ့ပါ)" });
                }
            }
            catch
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ဤ Email သည် အစစ်အမှန် မဟုတ်ပါ။ ကျေးဇူးပြု၍ တကယ့် Email အစစ်ကို ဖြည့်ပေးပါ။" });
            }
        }

        if (string.IsNullOrEmpty(request.app_student_phone))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းသားဖုန်းနံပါတ် ဖြည့်သွင်းရန် လိုအပ်ပါသည်။" });
        }

        string phone = request.app_student_phone.Trim();

        if (!Regex.IsMatch(phone, @"^09\d{9}$"))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "မြန်မာဖုန်းနံပါတ်သည် '09' ဖြင့် စတင်ရမည်ဖြစ်ပြီး ဂဏန်း ၁၁ လုံး ကွက်တိ ဖြစ်ရပါမည်။" });
        }

        string backNineDigits = phone.Substring(2);
        if (new string(backNineDigits[0], backNineDigits.Length) == backNineDigits)
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ဖုန်းနံပါတ် ပုံစံဖြစ်နေပါသည်။ (ဂဏန်းတူများ ဆက်တိုက်မသုံးရပါ)" });
        }

        string sequentialPatternUp = "123456789";
        string sequentialPatternDown = "987654321";
        if (sequentialPatternUp.Contains(backNineDigits) || sequentialPatternDown.Contains(backNineDigits))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ဖုန်းနံပါတ်ကို အစဉ်လိုက် ဂဏန်းများ (၁၂၃၄၅...) ဖြင့် အလွယ်တကူ မဖြည့်ရပါ။" });
        }

        // --- (ခ) Gender Validation (💡 Fix: Optional ဖြစ်သွား၍ ပါလာမှသာ စစ်မည်) ---
        var allowedGenders = new[] { "ကျား", "မ", "မောင်", "ဦး", "ဒေါ်" };
        if (!string.IsNullOrEmpty(request.gender_relation) && !allowedGenders.Contains(request.gender_relation))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Gender Relation ပုံစံ မှားယွင်းနေပါသည်။" });
        }

        // --- (ဂ) NRC Validation (💡 Fix: Optional ဖြစ်သွား၍ ပါလာမှသာ စစ်မည်) ---
        string fullNrcNo;
        var nrcType = string.IsNullOrWhiteSpace(request.nrc_type) ? "(နိုင်)" : request.nrc_type.Trim();
        if (!string.IsNullOrEmpty(request.nrc_state) && !string.IsNullOrEmpty(request.nrc_township) && !string.IsNullOrEmpty(request.nrc_number))
        {
            if (!int.TryParse(request.nrc_state, out int stateCode) || stateCode < 1 || stateCode > 14)
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC ပြည်နယ်ကုဒ်သည် ၁ မှ ၁၄ အတွင်းသာ ဖြစ်ရပါမည်။" });
            }

            if (!IsMyanmarText(request.nrc_township))
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC မြို့နယ်အတိုကောက်ကို မြန်မာစာဖြင့်သာ ဖြည့်ပါ။" });
            }

            if (!IsMyanmarNrcType(nrcType))
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC အမျိုးအစားကို မြန်မာစာဖြင့်သာ ရွေးချယ်ပါ။" });
            }

            if (!Regex.IsMatch(request.nrc_number, @"^\d{6}$"))
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC နောက်ဆုံး အမှတ်စဉ်သည် ဂဏန်း ၆ လုံးကွက်တိ ဖြစ်ရပါမည်။" });
            }

            fullNrcNo = $"{request.nrc_state}/{request.nrc_township}{nrcType}{request.nrc_number}";
        }
        else
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC အချက်အလက်များကို အပြည့်အစုံ ဖြည့်ပါ။" });
        }

        if (!string.IsNullOrWhiteSpace(request.app_guardian_nrc) && request.app_guardian_nrc != "-" && !IsFullMyanmarNrc(request.app_guardian_nrc))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အုပ်ထိန်းသူ NRC ကို မြန်မာစာ township/type နှင့် ဂဏန်း ၆ လုံးဖြင့်သာ ဖြည့်ပါ။" });
        }

        // --- (ဃ) Roll No & Blood Type Validation (💡 Fix: ပါလာမှသာ စစ်မည်) ---
        if (!string.IsNullOrEmpty(request.roll_no))
        {
            if (request.roll_no != request.roll_no.ToUpper())
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Roll No သည် အင်္ဂလိပ်စာလုံးကြီး (CAPITAL LETTERS) သာ ဖြစ်ရပါမည်။" });
            }
            if (!Regex.IsMatch(request.roll_no, @"^[A-Z0-9/-]+$"))
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Roll No တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
            }
        }

        var allowedBloodTypes = new[] { "A", "B", "AB", "O", "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        if (!string.IsNullOrEmpty(request.blood_type) && !allowedBloodTypes.Contains(request.blood_type.ToUpper()))
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
    public IActionResult GetMajors()
    {
        var majors = new List<string> { "Computer Science", "Computer Technology", "Information Technology" };
        return Ok(majors);
    }

    // ၈။ GET: ကျောင်းသား Registration ကို UserId ဖြင့် ရှာရန် (Payment အတွက် Data ယူဖို့)
    [HttpGet("GetByUserId/{userId}")]
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
