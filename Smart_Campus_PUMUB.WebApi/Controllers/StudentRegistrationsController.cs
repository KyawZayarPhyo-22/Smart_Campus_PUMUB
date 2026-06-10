// // using Microsoft.AspNetCore.Mvc;
// // using System;
// // using System.IO;
// // using System.Linq;
// // using System.Text.RegularExpressions;
// // using Smart_Campus_PUMUB.Database.AppDbContext;
// // using Smart_Campus_PUMUB.WebApi.Models;
// // // using Smart_Campus_PUMUB.WebApi.Models;

// // namespace Smart_Campus_PUMUB.WebApi.Controllers;

// // [ApiController]
// // [Route("api/[controller]")]
// // public class StudentRegistrationsController : ControllerBase
// // {
// //     private readonly SmartCampusDbContext _db;

// //     public StudentRegistrationsController(SmartCampusDbContext db)
// //     {
// //         _db = db;
// //     }

// //     // ၁။ GET: ဖောင်အားလုံး စာရင်းယူရန် (Read All)
// //     [HttpGet]
// //     public IActionResult GetRegistrations()
// //     {
// //         // 💡 Error Fix: _db.StudentRegistrations (သို့မဟုတ်) _db.Student_Registrations တစ်မျိုးတည်း ဖြစ်အောင် ညှိထားသည်
// //         var lst = _db.StudentRegistrations
// //             .Where(x => x.IsDelete == false || x.IsDelete == null)
// //             .OrderByDescending(x => x.RegistrationId)
// //             .ToList();
// //         return Ok(lst);
// //     }

// //     // ၂။ GET: ဖောင်တစ်ခုချင်းစီ အသေးစိတ်ကြည့်ရန် (Read One)
// //     [HttpGet("{id}")]
// //     public IActionResult GetRegistration(int id)
// //     {
// //         // 💡 Error Fix: Entity Model ရဲ့ Property နာမည်များကို PascalCase သို့ ပြောင်းလဲပြင်ဆင်ထားသည်
// //         var item = _db.StudentRegistrations
// //             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

// //         if (item is null)
// //         {
// //             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
// //         }
// //         return Ok(item);
// //     }

// //     // ၃။ POST: ကျောင်းအပ်ဖောင် အသစ်တင်သွင်းရန် (Create)
// //     [HttpPost]
// //     public IActionResult CreateRegistration([FromForm] StudentRegistrationCreateRequestModel request)
// //     {
// //         // --- (က) Email & Phone Formats Validation ---
// //         if (!string.IsNullOrEmpty(request.email) && !Regex.IsMatch(request.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
// //         {
// //             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Email ပုံစံမမှန်ကန်ပါ။" });
// //         }
// //         if (!string.IsNullOrEmpty(request.app_student_phone) && !Regex.IsMatch(request.app_student_phone, @"^09\d{9}$"))
// //         {
// //             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းသားဖုန်းနံပါတ် ပုံစံမမှန်ပါ။ (ဥပမာ- 09XXXXXXXXX)" });
// //         }

// //         // --- (ခ) Roll No Validation (Capital only, No Special characters) ---
// //         if (!string.IsNullOrEmpty(request.roll_no))
// //         {
// //             if (request.roll_no != request.roll_no.ToUpper())
// //             {
// //                 return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Roll No သည် အင်္ဂလိပ်စာလုံးကြီး (CAPITAL LETTERS) သာ ဖြစ်ရပါမည်။" });
// //             }
// //             if (!Regex.IsMatch(request.roll_no, @"^[A-Z0-9/-]+$"))
// //             {
// //                 return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Roll No တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
// //             }
// //         }

// //         // --- (ဂ) Dropdown Values Safe Validation ---
// //         var allowedGenders = new[] { "မောင်", "မ", "ဦး", "ဒေါ်" };
// //         if (string.IsNullOrEmpty(request.gender_relation) || !allowedGenders.Contains(request.gender_relation))
// //         {
// //             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Gender Relation ပုံစံ မှားယွင်းနေပါသည်။" });
// //         }

// //         var allowedBloodTypes = new[] { "A", "B", "AB", "O" };
// //         if (string.IsNullOrEmpty(request.blood_type) || !allowedBloodTypes.Contains(request.blood_type.ToUpper()))
// //         {
// //             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "သွေးအမျိုးအစား မှားယွင်းနေပါသည်။ (A, B, AB, O သာ ဖြစ်ရမည်)" });
// //         }

// //         // --- (ဃ) 📷 Images Upload handling ---
// //         string? studentImagePath = null;
// //         string? signatureImagePath = null;
// //         string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");

// //         if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

// //         // ကျောင်းသားဓာတ်ပုံ သိမ်းခြင်း
// //         if (request.StudentImageFile != null && request.StudentImageFile.Length > 0)
// //         {
// //             string ext = Path.GetExtension(request.StudentImageFile.FileName).ToLower();
// //             string fileName = $"std_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
// //             using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
// //             {
// //                 request.StudentImageFile.CopyTo(stream);
// //             }
// //             studentImagePath = $"/uploads/students/{fileName}";
// //         }

// //         // လက်မှတ်ပုံ သိမ်းခြင်း
// //         if (request.SignatureImageFile != null && request.SignatureImageFile.Length > 0)
// //         {
// //             string ext = Path.GetExtension(request.SignatureImageFile.FileName).ToLower();
// //             string fileName = $"sig_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
// //             using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
// //             {
// //                 request.SignatureImageFile.CopyTo(stream);
// //             }
// //             signatureImagePath = $"/uploads/students/{fileName}";
// //         }

// //         // --- (င) DB ထဲသို့ ဒေတာထည့်သွင်းခြင်း ---
// //         // 💡 Error Fix: Scaffold လုပ်ထားသော သို့မဟုတ် ခေါ်ယူထားသော Entity Class Name မှာ 'StudentRegistration' ဖြစ်ပြီး Property များမှာ PascalCase ဖြစ်သည်။
// //         var newReg = new StudentRegistration
// //         {
// //             UserId = request.UserId,
// //             AdmissionSerialNo = request.AdmissionSerialNo,
// //             AcademicYearRange = request.academic_year_range,
// //             AcademicYearLevel = request.academic_year_level,
// //             Major = request.major,
// //             RollNo = request.roll_no,
// //             UniversityRegNo = request.university_reg_no,
// //             AdmissionYear = request.admission_year,
// //             ApplicationDate = DateTime.UtcNow.AddHours(6).AddMinutes(30),
// //             StudentNameMm = request.student_name_mm,
// //             StudentNameEn = request.student_name_en,
// //             MotherName = request.mother_name,
// //             FatherName = request.father_name,
// //             GenderRelation = request.gender_relation,
// //             Ethnicity = request.ethnicity,
// //             Religion = request.religion,
// //             Pob = request.pob,
// //             BirthPlaceRegion = request.birth_place_region,
// //             StudentNrcNo = request.student_nrc_no,
// //             NationalityStatus = request.nationality_status,
// //             Dob = DateOnly.FromDateTime(request.dob),
// //             Email = request.email,
// //             BloodType = request.blood_type.ToUpper(),
// //             CovidVaccineStatus = request.covid_vaccine_status,

// //             // 💡 မင်းရဲ့ SQL Script ထဲမှာ ဓာတ်ပုံအတွက် Column သီးသန့်မပါလို့ လောလောဆယ် လမ်းကြောင်းကို CurrentAddress ထဲမှာ တွဲသိမ်းပေးထားပါတယ်ဗျာ။
// //             CurrentAddress = !string.IsNullOrEmpty(studentImagePath) ? $"{request.current_address} | Image: {studentImagePath}" : request.current_address,

// //             PermanentAddressMm = request.permanent_address_mm,
// //             PermanentAddressEn = request.permanent_address_en,
// //             MatricRollNo = request.matric_roll_no,
// //             MatricPassedYear = request.matric_passed_year,
// //             ExamCenter = request.exam_center,
// //             FatherOccupation = request.father_occupation,
// //             MotherOccupation = request.mother_occupation,
// //             PastExamMajor = request.past_exam_major,
// //             PastExamRollNo = request.past_exam_roll_no,
// //             PastExamYear = request.past_exam_year,
// //             PastExamStatus = request.past_exam_status,
// //             PreviousYearRollNo = request.previous_year_roll_no,
// //             GuardianName = request.guardian_name,
// //             GuardianRelationship = request.guardian_relationship,
// //             GuardianOccupation = request.guardian_occupation,
// //             GuardianAddressPhone = request.guardian_address_phone,
// //             AppGuardianName = request.app_guardian_name,
// //             AppGuardianNrc = request.app_guardian_nrc,
// //             AppGuardianPhone = request.app_guardian_phone,
// //             AppGuardianAddress = request.app_guardian_address,
// //             AppStudentName = request.app_student_name,
// //             AppStudentPhone = request.app_student_phone,
// //             StipendRequested = request.stipend_requested ?? false,
// //             Status = "Pending", 
// //             CreatedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
// //             CreatedBy = request.created_by,
// //             IsDelete = false
// //         };

// //         _db.StudentRegistrations.Add(newReg);
// //         int result = _db.SaveChanges();

// //         return StatusCode(201, new StudentRegistrationResponseModel
// //         {
// //             IsSuccess = result > 0,
// //             Message = result > 0 ? "ကျောင်းအပ်ဖောင် တင်သွင်းခြင်း အောင်မြင်ပါသည်။" : "တင်သွင်းမှု မအောင်မြင်ပါ။"
// //         });
// //     }

// //     // ၄။ PUT: ဖောင်အချက်အလက် ပြင်ရန် (Update)
// //     [HttpPut("{id}")]
// //     public IActionResult UpdateRegistration(int id, [FromForm] StudentRegistrationUpdateRequestModel request)
// //     {
// //         var item = _db.StudentRegistrations
// //             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

// //         if (item is null)
// //         {
// //             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ဖောင်ကို ရှာမတွေ့ပါ။" });
// //         }

// //         item.AcademicYearRange = request.academic_year_range;
// //         item.AcademicYearLevel = request.academic_year_level;
// //         item.Major = request.major;
// //         item.RollNo = request.roll_no;
// //         item.StudentNameMm = request.student_name_mm;
// //         item.StudentNameEn = request.student_name_en;
// //         item.Email = request.email;
// //         item.ModifiedBy = request.modified_by;
// //         item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

// //         int result = _db.SaveChanges();
// //         return Ok(new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = "ဖောင်အချက်အလက် ပြင်ဆင်ပြီးပါပြီ။" });
// //     }

// //     // ၅။ DELETE: ဖောင်ကို ဖျက်ရန် (Soft Delete)
// //     [HttpDelete("{id}")]
// //     public IActionResult DeleteRegistration(int id)
// //     {
// //         var item = _db.StudentRegistrations
// //             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

// //         if (item is null)
// //         {
// //             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ဖျက်မည့် ဖောင်ကို ရှာမတွေ့ပါ။" });
// //         }

// //         item.IsDelete = true; // Soft delete
// //         int result = _db.SaveChanges();

// //         return Ok(new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = "ဖောင်ကို ဖျက်သိမ်းပြီးပါပြီ။" });
// //     }

// //     // ၆။ GET: Roll No ဖြင့် အချက်အလက်ဟောင်း Auto ရှာရန် (Special API)
// //     [HttpGet("search-past-student")]
// //     public IActionResult SearchPastStudent([FromQuery] string rollNo)
// //     {
// //         if (string.IsNullOrEmpty(rollNo))
// //         {
// //             return BadRequest(new { message = "Roll No ထည့်သွင်းပေးရန် လိုအပ်သည်။" });
// //         }

// //         var student = _db.StudentRegistrations
// //             .FirstOrDefault(x => x.RollNo == rollNo.ToUpper() && (x.IsDelete == false || x.IsDelete == null));

// //         if (student is null)
// //         {
// //             return NotFound(new { message = "ဤ Roll No ဖြင့် ကျောင်းသားမှတ်တမ်း ဟောင်းမရှိပါ။" });
// //         }

// //         return Ok(new
// //         {
// //             studentNameMm = student.StudentNameMm,
// //             studentNameEn = student.StudentNameEn,
// //             fatherName = student.FatherName,
// //             motherName = student.MotherName,
// //             nrcNo = student.StudentNrcNo,
// //             major = student.Major
// //         });
// //     }
// //     [HttpGet("majors")]
// // public IActionResult GetMajors()
// // {
// //     // မင်းရဲ့ Major အခွဲတွေကို List အနေနဲ့ ပို့ပေးတာပါ
// //     var majors = new List<string> { "Computer Science", "Computer Technology", "Information Technology" };
// //     return Ok(majors);
// // }

// //     // ၇။ PATCH: ဖောင်ကို Approved / Rejected လုပ်ရန် (Special API)
// //     [HttpPatch("{id}/status")]
// //     public IActionResult PatchStatus(int id, [FromBody] StudentRegistrationStatusPatchModel request)
// //     {
// //         var item = _db.StudentRegistrations
// //             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

// //         if (item is null)
// //         {
// //             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
// //         }

// //         var allowedStatuses = new[] { "Pending", "Approved", "Rejected" };
// //         if (!allowedStatuses.Contains(request.Status))
// //         {
// //             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Status ပြောင်းလဲမှုပုံစံ မှားယွင်းနေပါသည်။ (Approved သို့မဟုတ် Rejected သာဖြစ်ရမည်)" });
// //         }

// //         item.Status = request.Status;
// //         item.ModifiedBy = request.modified_by;
// //         item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

// //         int result = _db.SaveChanges();
// //         return Ok(new StudentRegistrationResponseModel 
// //         { 
// //             IsSuccess = result > 0, 
// //             Message = $"ကျောင်းအပ်ဖောင်ကို {request.Status} ပြုလုပ်ခြင်း အောင်မြင်ပါသည်။" 
// //         });
// //     }
// // }
// using Microsoft.AspNetCore.Mvc;
// using System;
// using System.IO;
// using System.Linq;
// using System.Text.RegularExpressions;
// using Smart_Campus_PUMUB.Database.AppDbContext;
// using Smart_Campus_PUMUB.WebApi.Models;
// // using NLADotNetInternshipTraining.WebApi.Models;

// namespace Smart_Campus_PUMUB.WebApi.Controllers;

// [ApiController]
// [Route("api/[controller]")]
// public class StudentRegistrationsController : ControllerBase
// {
//     private readonly SmartCampusDbContext _db;

//     public StudentRegistrationsController(SmartCampusDbContext db)
//     {
//         _db = db;
//     }

//     // ၁။ GET: ဖောင်အားလုံး စာရင်းယူရန် (Read All)
//     [HttpGet]
//     public IActionResult GetRegistrations()
//     {
//         var lst = _db.StudentRegistrations
//             .Where(x => x.IsDelete == false || x.IsDelete == null)
//             .OrderByDescending(x => x.RegistrationId)
//             .ToList();
//         return Ok(lst);
//     }

//     // ၂။ GET: ဖောင်တစ်ခုချင်းစီ အသေးစိတ်ကြည့်ရန် (Read One)
//     [HttpGet("{id}")]
//     public IActionResult GetRegistration(int id)
//     {
//         var item = _db.StudentRegistrations
//             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

//         if (item is null)
//         {
//             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
//         }
//         return Ok(item);
//     }

//     // ၃။ POST: ကျောင်းအပ်ဖောင် အသစ်တင်သွင်းရန် (Create)
//     [HttpPost]
//     public IActionResult CreateRegistration([FromForm] StudentRegistrationCreateRequestModel request)
//     {
//         // --- (က) Email & Phone Formats Validation ---
//         if (!string.IsNullOrEmpty(request.email) && !Regex.IsMatch(request.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
//         {
//             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Email ပုံစံမမှန်ကန်ပါ။" });
//         }
//         if (!string.IsNullOrEmpty(request.app_student_phone) && !Regex.IsMatch(request.app_student_phone, @"^09\d{9}$"))
//         {
//             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းသားဖုန်းနံပါတ် ပုံစံမမှန်ပါ။ (ဥပမာ- 09XXXXXXXXX)" });
//         }

//         // --- (ခ) Roll No Validation (Capital only, No Special characters) ---
//         if (!string.IsNullOrEmpty(request.roll_no))
//         {
//             if (request.roll_no != request.roll_no.ToUpper())
//             {
//                 return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Roll No သည် အင်္ဂလိပ်စာလုံးကြီး (CAPITAL LETTERS) သာ ဖြစ်ရပါမည်။" });
//             }
//             if (!Regex.IsMatch(request.roll_no, @"^[A-Z0-9/-]+$"))
//             {
//                 return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Roll No တွင် သင်္ကေတ (Special Characters) များ မသုံးရပါ။" });
//             }
//         }

//         // --- (ဂ) Dropdown Values Safe Validation ---
//         var allowedGenders = new[] { "မောင်", "မ", "ဦး", "ဒေါ်" };
//         if (string.IsNullOrEmpty(request.gender_relation) || !allowedGenders.Contains(request.gender_relation))
//         {
//             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Gender Relation ပုံစံ မှားယွင်းနေပါသည်။" });
//         }

//         var allowedBloodTypes = new[] { "A", "B", "AB", "O" };
//         if (string.IsNullOrEmpty(request.blood_type) || !allowedBloodTypes.Contains(request.blood_type.ToUpper()))
//         {
//             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "သွေးအမျိုးအစား မှားယွင်းနေပါသည်။ (A, B, AB, O သာ ဖြစ်ရမည်)" });
//         }

//         // --- (ဃ) 📷 Images Upload handling ---
//         string studentImagePath = "";
//         string signatureImagePath = "";
//         string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "students");

//         if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

//         if (request.StudentImageFile != null && request.StudentImageFile.Length > 0)
//         {
//             string ext = Path.GetExtension(request.StudentImageFile.FileName).ToLower();
//             string fileName = $"std_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
//             using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
//             {
//                 request.StudentImageFile.CopyTo(stream);
//             }
//             studentImagePath = $"/uploads/students/{fileName}";
//         }

//         if (request.SignatureImageFile != null && request.SignatureImageFile.Length > 0)
//         {
//             string ext = Path.GetExtension(request.SignatureImageFile.FileName).ToLower();
//             string fileName = $"sig_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
//             using (var stream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
//             {
//                 request.SignatureImageFile.CopyTo(stream);
//             }
//             signatureImagePath = $"/uploads/students/{fileName}";
//         }

//         // --- (င) DB ထဲသို့ ဒေတာထည့်သွင်းခြင်း ---
//         var newReg = new StudentRegistration
//         {
//             UserId = request.UserId,
//             AdmissionSerialNo = request.AdmissionSerialNo,
//             AcademicYearRange = request.academic_year_range,
//             AcademicYearLevel = request.academic_year_level,
//             Major = request.major,
//             RollNo = request.roll_no,
//             UniversityRegNo = request.university_reg_no,
//             AdmissionYear = request.admission_year,
//             ApplicationDate = DateTime.UtcNow.AddHours(6).AddMinutes(30),
//             StudentNameMm = request.student_name_mm,
//             StudentNameEn = request.student_name_en,
//             MotherName = request.mother_name,
//             FatherName = request.father_name,
//             GenderRelation = request.gender_relation,
//             Ethnicity = request.ethnicity,
//             Religion = request.religion,
//             Pob = request.pob,
//             BirthPlaceRegion = request.birth_place_region,
//             StudentNrcNo = request.student_nrc_no,
//             NationalityStatus = request.nationality_status,
//             Dob = DateOnly.FromDateTime(request.dob),
//             Email = request.email,
//             BloodType = request.blood_type.ToUpper(),
//             CovidVaccineStatus = request.covid_vaccine_status,
//             CurrentAddress = request.current_address,
//             PermanentAddressMm = request.permanent_address_mm,
//             PermanentAddressEn = request.permanent_address_en,
//             MatricRollNo = request.matric_roll_no,
//             MatricPassedYear = request.matric_passed_year,
//             ExamCenter = request.exam_center,
//             FatherOccupation = request.father_occupation,
//             MotherOccupation = request.mother_occupation,
//             PastExamMajor = request.past_exam_major,
//             PastExamRollNo = request.past_exam_roll_no,
//             PastExamYear = request.past_exam_year,
//             PastExamStatus = request.past_exam_status,
//             PreviousYearRollNo = request.previous_year_roll_no,
//             GuardianName = request.guardian_name,
//             GuardianRelationship = request.guardian_relationship,
//             GuardianOccupation = request.guardian_occupation,
//             GuardianAddressPhone = request.guardian_address_phone,
//             AppGuardianName = request.app_guardian_name,
//             AppGuardianNrc = request.app_guardian_nrc,
//             AppGuardianPhone = request.app_guardian_phone,
//             AppGuardianAddress = request.app_guardian_address,
//             AppStudentName = request.app_student_name,
//             AppStudentPhone = request.app_student_phone,
//             StipendRequested = request.stipend_requested ?? false,
//             Status = "Pending", 
//             CreatedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
//             CreatedBy = request.created_by,
//             IsDelete = false
//         };

//         _db.StudentRegistrations.Add(newReg);
//         int result = _db.SaveChanges();

//         return StatusCode(201, new StudentRegistrationResponseModel
//         {
//             IsSuccess = result > 0,
//             Message = result > 0 ? "ကျောင်းအပ်ဖောင် တင်သွင်းခြင်း အောင်မြင်ပါသည်။" : "တင်သွင်းမှု မအောင်မြင်ပါ။"
//         });
//     }

//     // ၄။ PUT: ဖောင်အချက်အလက် ပြင်ရန် (Update)
//     [HttpPut("{id}")]
//     public IActionResult UpdateRegistration(int id, [FromForm] StudentRegistrationUpdateRequestModel request)
//     {
//         var item = _db.StudentRegistrations
//             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

//         if (item is null)
//         {
//             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ဖောင်ကို ရှာမတွေ့ပါ။" });
//         }

//         item.AcademicYearRange = request.academic_year_range;
//         item.AcademicYearLevel = request.academic_year_level;
//         item.Major = request.major;
//         item.RollNo = request.roll_no;
//         item.StudentNameMm = request.student_name_mm;
//         item.StudentNameEn = request.student_name_en;
//         item.Email = request.email;
//         item.ModifiedBy = request.modified_by;
//         item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

//         int result = _db.SaveChanges();
//         return Ok(new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = "ဖောင်အချက်အလက် ပြင်ဆင်ပြီးပါပြီ။" });
//     }

//     // ၅။ DELETE: ဖောင်ကို ဖျက်ရန် (Soft Delete)
//     [HttpDelete("{id}")]
//     public IActionResult DeleteRegistration(int id)
//     {
//         var item = _db.StudentRegistrations
//             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

//         if (item is null)
//         {
//             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ဖျက်မည့် ဖောင်ကို ရှာမတွေ့ပါ။" });
//         }

//         item.IsDelete = true;
//         int result = _db.SaveChanges();

//         return Ok(new StudentRegistrationResponseModel { IsSuccess = result > 0, Message = "ဖောင်ကို ဖျက်သိမ်းပြီးပါပြီ။" });
//     }

//     // ၆။ GET: Roll No ဖြင့် အချက်အလက်ဟောင်း အားလုံး Auto ရှာရန် (Special API - All Fields)
//     [HttpGet("search-past-student")]
//     public IActionResult SearchPastStudent([FromQuery] string rollNo)
//     {
//         if (string.IsNullOrEmpty(rollNo))
//         {
//             return BadRequest(new { message = "Roll No ထည့်သွင်းပေးရန် လိုအပ်သည်။" });
//         }

//         // ဤ Roll No ဖြင့် နောက်ဆုံးအပ်နှံထားသော အောင်မြင်သည့် မှတ်တမ်းကို ရှာဖွေခြင်း
//         var student = _db.StudentRegistrations
//             .Where(x => x.RollNo == rollNo.ToUpper() && (x.IsDelete == false || x.IsDelete == null))
//             .OrderByDescending(x => x.RegistrationId)
//             .FirstOrDefault();

//         if (student is null)
//         {
//             return NotFound(new { message = "ဤ Roll No ဖြင့် ကျောင်းသားမှတ်တမ်း ဟောင်းမရှိပါ။" });
//         }

//         // 🎯 ဇယားတစ်ခုလုံးရှိ ကျောင်းသားကိုယ်ရေးအချက်အလက် ကော်လံအားလုံး (All Fields) ကို Front-end ဆီ လွှတ်ပေးခြင်း
//         return Ok(new
//         {
//             userId = student.UserId,
//             admissionSerialNo = student.AdmissionSerialNo,
//             academicYearRange = student.AcademicYearRange,
//             academicYearLevel = student.AcademicYearLevel,
//             major = student.Major,
//             rollNo = student.RollNo,
//             universityRegNo = student.UniversityRegNo,
//             admissionYear = student.AdmissionYear,
//             studentNameMm = student.StudentNameMm,
//             studentNameEn = student.StudentNameEn,
//             motherName = student.MotherName,
//             fatherName = student.FatherName,
//             genderRelation = student.GenderRelation,
//             ethnicity = student.Ethnicity,
//             religion = student.Religion,
//             pob = student.Pob,
//             birthPlaceRegion = student.BirthPlaceRegion,
//             studentNrcNo = student.StudentNrcNo,
//             nationalityStatus = student.NationalityStatus,
//             dob = student.Dob, // DateOnly format
//             email = student.Email,
//             bloodType = student.BloodType,
//             covidVaccineStatus = student.CovidVaccineStatus,
//             currentAddress = student.CurrentAddress,
//             permanentAddressMm = student.PermanentAddressMm,
//             permanentAddressEn = student.PermanentAddressEn,
//             matricRollNo = student.MatricRollNo,
//             matricPassedYear = student.MatricPassedYear,
//             examCenter = student.ExamCenter,
//             fatherOccupation = student.FatherOccupation,
//             motherOccupation = student.MotherOccupation,
//             pastExamMajor = student.PastExamMajor,
//             pastExamRollNo = student.PastExamRollNo,
//             pastExamYear = student.PastExamYear,
//             pastExamStatus = student.PastExamStatus,
//             previousYearRollNo = student.PreviousYearRollNo,
//             guardianName = student.GuardianName,
//             guardianRelationship = student.GuardianRelationship,
//             guardianOccupation = student.GuardianOccupation,
//             guardianAddressPhone = student.GuardianAddressPhone,
//             appGuardianName = student.AppGuardianName,
//             appGuardianNrc = student.AppGuardianNrc,
//             appGuardianPhone = student.AppGuardianPhone,
//             appGuardianAddress = student.AppGuardianAddress,
//             appStudentName = student.AppStudentName,
//             appStudentPhone = student.AppStudentPhone,
//             stipendRequested = student.StipendRequested
//         });
//     }

//     // ၇။ PATCH: ဖောင်ကို Approved / Rejected လုပ်ရန် (Special API)
//     [HttpPatch("{id}/status")]
//     public IActionResult PatchStatus(int id, [FromBody] StudentRegistrationStatusPatchModel request)
//     {
//         var item = _db.StudentRegistrations
//             .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

//         if (item is null)
//         {
//             return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
//         }

//         var allowedStatuses = new[] { "Pending", "Approved", "Rejected" };
//         if (!allowedStatuses.Contains(request.Status))
//         {
//             return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Status ပြောင်းလဲမှုပုံစံ မှားယွင်းနေပါသည်။ (Approved သို့မဟုတ် Rejected သာဖြစ်ရမည်)" });
//         }

//         item.Status = request.Status;
//         item.ModifiedBy = request.modified_by;
//         item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

//         int result = _db.SaveChanges();
//         return Ok(new StudentRegistrationResponseModel 
//         { 
//             IsSuccess = result > 0, 
//             Message = $"ကျောင်းအပ်ဖောင်ကို {request.Status} ပြုလုပ်ခြင်း အောင်မြင်ပါသည်။" 
//         });
//     }
// }
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.EntityFrameworkCore;

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

    // ၁။ GET: ဖောင်အားလုံး စာရင်းယူရန် (Read All)
    [HttpGet]
    public IActionResult GetRegistrations()
    {
        var lst = _db.StudentRegistrations
            // 💡 ဤစာကြောင်းလေး ထည့်ပေးခြင်းဖြင့် ရလဒ်ထဲမှာ registrationPayments တွေပါ ပါလာမည် ဖြစ်သည်။
            .Include(x => x.RegistrationPayments)
            .Where(x => x.IsDelete == false || x.IsDelete == null)
            .OrderByDescending(x => x.RegistrationId)
            .ToList();

        return Ok(lst);
    }

    // ၂။ GET: ဖောင်တစ်ခုချင်းစီ အသေးစိတ်ကြည့်ရန် (Read One)
    [HttpGet("{id}")]
    public IActionResult GetRegistration(int id)
    {
        var item = _db.StudentRegistrations
            .Include(x => x.RegistrationPayments)
            .FirstOrDefault(x => x.RegistrationId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းအပ်ဖောင် ရှာမတွေ့ပါ။" });
        }
        return Ok(item);
    }

    // ၃။ POST: ကျောင်းအပ်ဖောင် အသစ်တင်သွင်းရန် (Create)
    [HttpPost]
    public IActionResult CreateRegistration([FromForm] StudentRegistrationCreateRequestModel request)
    {
        if (request.UserId <= 0)
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအိုင်ဒီ (UserId) ထည့်သွင်းရန် လိုအပ်သည်။" });
        }
        // DB ထဲမှာ အဲ့ဒီ User ရှိမရှိ စစ်ဆေးမည်
        var userCheck = _db.Users.FirstOrDefault(x => x.UserId == request.UserId && x.IsDelete == false);

        if (userCheck is null)
        {
            return NotFound(new StudentRegistrationResponseModel { IsSuccess = false, Message = "အသုံးပြုသူအကောင့်ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
        }

        // 💡 မင်းရဲ့ Role သတ်မှတ်ချက်အရ 4 က Student ဖြစ်ရင် Student ပဲ ဖောင်တင်ခွင့်ပေးမည်
        // (တကယ်လို့ Admin သို့မဟုတ် Tutor ဖြစ်နေရင် ပိတ်ပစ်မည်)
        if (userCheck.RoleId != 4)
        {
            return BadRequest(new StudentRegistrationResponseModel
            {
                IsSuccess = false,
                Message = "ကျောင်းသားအကောင့်များသာ ကျောင်းအပ်ဖောင် တင်သွင်းခွင့်ရှိသည်။ (Admin သို့မဟုတ် Tutor အကောင့်များ တင်၍မရပါ)"
            });
        }
        // ------------------------------------------------------------------------
        // --- (က) Email Smart & Real Existence Validation ---
        // ------------------------------------------------------------------------
        if (!string.IsNullOrEmpty(request.email))
        {
            // ၁။ ပုံစံ မှန်ကန်မှု ရှိမရှိ အရင်စစ်မည်
            if (!Regex.IsMatch(request.email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Email ပုံစံမမှန်ကန်ပါ။" });
            }

            // ၂။ တကယ်ရှိတဲ့ Email ဟုတ်မဟုတ် သိနိုင်ရန် Mail Domain (MX Record) ကို အနောက်ကနေ စစ်ဆေးခြင်း
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
                // Domain မရှိရင် သို့မဟုတ် စစ်လို့မရရင် Email အတုလို့ သတ်မှတ်မည်
                return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ဤ Email သည် အစစ်အမှန် မဟုတ်ပါ။ ကျေးဇူးပြု၍ တကယ့် Email အစစ်ကို ဖြည့်ပေးပါ။" });
            }
        }

        // ------------------------------------------------------------------------
        // --- (ခ) 🇲🇲 Myanmar Phone Number Strict Validation (11 Digits Only) ---
        // ------------------------------------------------------------------------
        if (string.IsNullOrEmpty(request.app_student_phone))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ကျောင်းသားဖုန်းနံပါတ် ဖြည့်သွင်းရန် လိုအပ်ပါသည်။" });
        }

        string phone = request.app_student_phone.Trim();

        // ၁။ 09 နဲ့ စပြီး ဂဏန်း ၁၁ လုံး ကွက်တိ ဖြစ်ရမည် (များလို့၊ နည်းလို့ မရပါ)
        if (!Regex.IsMatch(phone, @"^09\d{9}$"))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "မြန်မာဖုန်းနံပါတ်သည် '09' ဖြင့် စတင်ရမည်ဖြစ်ပြီး ဂဏန်း ၁၁ လုံး ကွက်တိ ဖြစ်ရပါမည်။" });
        }

        // ၂။ ဂဏန်းတူတွေ ဆက်တိုက် ရိုက်ထားခြင်း ရှိမရှိ စစ်ဆေးခြင်း (ဥပမာ- 09222222222, 09999999999)
        string backNineDigits = phone.Substring(2); // 09 ကို ဖယ်ပြီး အနောက်က ၉ လုံးပဲ ယူမည်
        if (new string(backNineDigits[0], backNineDigits.Length) == backNineDigits)
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ဖုန်းနံပါတ် ပုံစံဖြစ်နေပါသည်။ (ဂဏန်းတူများ ဆက်တိုက်မသုံးရပါ)" });
        }

        // ၃။ အစဉ်လိုက် ဂဏန်းများ ဖြစ်နေခြင်း ရှိမရှိ စစ်ဆေးခြင်း (ဥပမာ- 09123456789)
        string sequentialPatternUp = "123456789";
        string sequentialPatternDown = "987654321";

        if (sequentialPatternUp.Contains(backNineDigits) || sequentialPatternDown.Contains(backNineDigits))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "ဖုန်းနံပါတ်ကို အစဉ်လိုက် ဂဏန်းများ (၁၂၃၄၅...) ဖြင့် အလွယ်တကူ မဖြည့်ရပါ။" });
        }

        // ------------------------------------------------------------------------
        // --- (ဂ) Gender (ကျား / မ) သီးသန့်စစ်ဆေးခြင်း ---
        // ------------------------------------------------------------------------
        var allowedGenders = new[] { "ကျား", "မ" };
        if (string.IsNullOrEmpty(request.gender_relation) || !allowedGenders.Contains(request.gender_relation))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Gender Relation သည် (မောင်၊ မ၊ ဦး၊ ဒေါ်) ထဲမှ တစ်ခုခုသာ ဖြစ်ရပါမည်။" });
        }

        // ------------------------------------------------------------------------
        // --- (ဃ) 🇲🇲 NRC Smart Validation (Dropdowns + Text Input Logic) ---
        // ------------------------------------------------------------------------
        if (string.IsNullOrEmpty(request.nrc_state) || string.IsNullOrEmpty(request.nrc_township) || string.IsNullOrEmpty(request.nrc_number))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC အချက်အလက်များကို ပြည့်စုံစွာ ဖြည့်သွင်းပေးပါ။" });
        }

        // ၁။ တိုင်းနှင့်ပြည်နယ်ကုဒ် (၁ မှ ၁၄ အတွင်း ရှိမရှိ စစ်ဆေးခြင်း)
        if (!int.TryParse(request.nrc_state, out int stateCode) || stateCode < 1 || stateCode > 14)
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC ပြည်နယ်ကုဒ်သည် ၁ မှ ၁၄ အတွင်းသာ ဖြစ်ရပါမည်။" });
        }

        // ၂။ နောက်ဆုံးဂဏန်းအပိုင်း (၆ လုံးကွက်တိ ဖြစ်ရမည်၊ ငယ်လို့ ကြီးလို့ မရပါ)
        if (!Regex.IsMatch(request.nrc_number, @"^\d{6}$"))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "NRC နောက်ဆုံး အမှတ်စဉ်သည် ဂဏန်း ၆ လုံးကွက်တိ ဖြစ်ရပါမည်။" });
        }

        // ၃။ ကော်လံထဲ သိမ်းဆည်းရန် NRC ပုံစံ အချောသပ် တည်ဆောက်ခြင်း (Eg. 11/THAKANA(N)122344)
        string nrcType = "N";
        string fullNrcNo = $"{request.nrc_state}/{request.nrc_township.ToUpper()}({nrcType}){request.nrc_number}";

        // ------------------------------------------------------------------------
        // --- (င) Roll No & Blood Type Validation ---
        // ------------------------------------------------------------------------
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

        var allowedBloodTypes = new[] { "A", "B", "AB", "O" };
        if (string.IsNullOrEmpty(request.blood_type) || !allowedBloodTypes.Contains(request.blood_type.ToUpper()))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "သွေးအမျိုးအစား မှားယွင်းနေပါသည်။ (A, B, AB, O သာ ဖြစ်ရမည်)" });
        }

        // ------------------------------------------------------------------------
        // --- (စ) 📷 Images Upload handling ---
        // ------------------------------------------------------------------------
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

        // ------------------------------------------------------------------------
        // --- (ဆ) DB ထဲသို့ ဒေတာထည့်သွင်းခြင်း ---
        // ------------------------------------------------------------------------
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

            // 💡 စစ်ဆေးပြီးသား fullNrcNo (ဥပမာ- 11/THAKANA(N)122344) အား ထည့်သွင်းခြင်း
            StudentNrcNo = fullNrcNo,

            NationalityStatus = request.nationality_status,
            Dob = DateOnly.FromDateTime(request.dob),
            Email = request.email,
            BloodType = request.blood_type.ToUpper(),
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
            Status = "Pending",
            CreatedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = request.created_by ?? "Admin", // CreatedBy ကို Null လို့ မပေးရင် "System" အဖြစ်သတ်မှတ်မည်
            IsDelete = false
        };

        _db.StudentRegistrations.Add(newReg);
        int result = _db.SaveChanges();

        return StatusCode(201, new StudentRegistrationResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ကျောင်းအပ်ဖောင် တင်သွင်းခြင်း အောင်မြင်ပါသည်။" : "တင်သွင်းမှု မအောင်မြင်ပါ။"
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
        // မင်းရဲ့ Major အခွဲတွေကို List အနေနဲ့ ပို့ပေးတာပါ
        var majors = new List<string> { "Computer Science", "Computer Technology", "Information Technology" };
        return Ok(majors);
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

        var allowedStatuses = new[] { "Pending", "Approved", "Rejected" };
        if (!allowedStatuses.Contains(request.Status))
        {
            return BadRequest(new StudentRegistrationResponseModel { IsSuccess = false, Message = "Status ပြောင်းလဲမှုပုံစံ မှားယွင်းနေပါသည်။" });
        }

        item.Status = request.Status;
        item.ModifiedBy = request.modified_by;
        item.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();
        return Ok(new StudentRegistrationResponseModel
        {
            IsSuccess = result > 0,
            Message = $"ကျောင်းအပ်ဖောင်ကို {request.Status} ပြုလုပ်ခြင်း အောင်မြင်ပါသည်။"
        });
    }
}

