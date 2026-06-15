using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentRegistrationsController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public StudentRegistrationsController(SmartCampusDbContext db)
    {
        _db = db;
    }

    // ၁။ GET: api/StudentRegistrations (Read All)
    [HttpGet]
    public IActionResult GetRegistrations()
    {
        var lst = _db.StudentRegistrations
                     .Include(x => x.RegistrationPayments)
                     .Where(x => x.IsDelete == false || x.IsDelete == null)
                     .OrderByDescending(x => x.RegistrationId)
                     .ToList();

        return Ok(lst);
    }

    // ၂။ GET: api/StudentRegistrations/{id} (Read One)
    [HttpGet("{id}")]
    public IActionResult GetRegistration(int id)
    {
        if (id <= 0) return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Invalid ID" });

        var item = _db.StudentRegistrations
                      .Include(x => x.RegistrationPayments)
                      .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
        }
        return Ok(item);
    }

    // ၃။ POST: api/StudentRegistrations (Create)
    [HttpPost]
    public IActionResult CreateRegistration([FromForm] StudentRegistrationCreateRequestModel request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request.UserId <= 0) return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအိုင်ဒီ (UserId) ထည့်သွင်းရန် လိုအပ်သည်။" });

        var userCheck = _db.Users.FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);
        if (userCheck is null) return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအကောင့်ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
        if (userCheck.RoleId != 4) return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းသားအကောင့်များသာ ကျောင်းအပ်ဖောင် တင်သွင်းခွင့်ရှိသည်။" });

        // Email Format Validation
        if (!string.IsNullOrEmpty(request.email) && !Regex.IsMatch(request.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Email ပုံစံမမှန်ကန်ပါ။" });
        }

        // Phone Validation
        string phone = request.app_student_phone?.Trim() ?? "";
        if (!Regex.IsMatch(phone, @"^09\d{9}$"))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "မြန်မာဖုန်းနံပါတ်သည် '09' ဖြင့် စတင်ရမည်ဖြစ်ပြီး ဂဏန်း ၁၁ လုံး ကွက်တိ ဖြစ်ရပါမည်။" });
        }

        // NRC Logic Processing
        string fullNrcNo = $"{request.nrc_state}/{request.nrc_township?.ToUpper()}(N){request.nrc_number}";

        // Images File Stream Upload Handling
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

        var newReg = new StudentRegistration
        {
            UserId = request.UserId,
            AdmissionSerialNo = request.AdmissionSerialNo,
            AcademicYearRange = request.academic_year_range,
            AcademicYearLevel = request.academic_year_level,
            Major = request.major,
            RollNo = request.roll_no?.ToUpper(),
            UniversityRegNo = request.university_reg_no,
            AdmissionYear = request.admission_year,
            ApplicationDate = DateTime.Now,
            StudentNameMm = request.student_name_mm?.Trim(),
            StudentNameEn = request.student_name_en?.Trim(),
            MotherName = request.mother_name?.Trim(),
            FatherName = request.father_name?.Trim(),
            GenderRelation = request.gender_relation,
            Ethnicity = request.ethnicity,
            Religion = request.religion,
            Pob = request.pob,
            BirthPlaceRegion = request.birth_place_region,
            StudentNrcNo = fullNrcNo,
            NationalityStatus = request.nationality_status,
            Dob = DateOnly.FromDateTime(request.dob),
            Email = request.email,
            BloodType = request.blood_type?.ToUpper(),
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
            AppStudentPhone = phone,
            StipendRequested = request.stipend_requested ?? false,
            Status = "Pending",
            CreatedDatetime = DateTime.Now,
            CreatedBy = request.created_by ?? "Student_Portal",
            IsDelete = false
        };

        _db.StudentRegistrations.Add(newReg);
        int result = _db.SaveChanges();

        return StatusCode(201, new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = result > 0 ? "ကျောင်းအပ်ဖောင် တင်သွင်းခြင်း အောင်မြင်ပါသည်။" : "တင်သွင်းမှု မအောင်မြင်ပါ။" });
    }

    // ၄။ PATCH: api/StudentRegistrations/{id}/status (Status Update)
    [HttpPatch("{id}/status")]
    public IActionResult PatchStatus(int id, [FromBody] StudentRegistrationStatusPatchModel request)
    {
        var item = _db.StudentRegistrations.FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null) return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });

        var allowedStatuses = new[] { "Pending", "Approved", "Rejected" };
        if (!allowedStatuses.Contains(request.Status)) return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Status ပြောင်းလဲမှုပုံစံ မှားယွင်းနေပါသည်။" });

        item.Status = request.Status;
        item.ModifiedBy = request.modified_by ?? "Admin_Panel";
        item.ModifiedDatetime = DateTime.Now;

        int result = _db.SaveChanges();
        return Ok(new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = $"ကျောင်းအပ်ဖောင်ကို {request.Status} ပြုလုပ်ခြင်း အောင်မြင်ပါသည်။" });
    }
}