using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentPersonalInfoController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public StudentPersonalInfoController(SmartCampusDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetAllPersonalInfos([FromQuery] int? facultyId = null)
    {
        // Faculty-based scoping: join PersonalInfo → User to filter by FacultyId
        IQueryable<StudentPersonalInfo> query = _db.StudentPersonalInfos;

        if (facultyId.HasValue && facultyId.Value > 0)
        {
            // Only return records whose linked User belongs to the specified Faculty
            var userIdsInFaculty = _db.Users
                .Where(u => u.FacultyId == facultyId.Value)
                .Select(u => u.UserId)
                .ToHashSet();

            query = query.Where(info => userIdsInFaculty.Contains(info.UserId));
        }

        var infos = query.ToList();
        var users = _db.Users.AsNoTracking().ToDictionary(u => u.UserId, u => u);
        var faculties = _db.Faculties.Where(f => f.IsDelete == false || f.IsDelete == null).AsNoTracking().ToDictionary(f => f.FacultyId, f => f.FacultyName);
        var majors = _db.Majors.Where(m => m.IsDelete == false || m.IsDelete == null).AsNoTracking().ToList();

        var response = infos.Select(info =>
        {
            int? resolvedFacultyId = null;
            if (info.UserId > 0 && users.TryGetValue(info.UserId, out var u) && u.FacultyId.HasValue && u.FacultyId.Value > 0)
            {
                resolvedFacultyId = u.FacultyId.Value;
            }

            if ((!resolvedFacultyId.HasValue || resolvedFacultyId.Value <= 0) && !string.IsNullOrWhiteSpace(info.major))
            {
                var majorText = info.major.Trim().ToLower();
                var matchedMajor = majors.FirstOrDefault(m =>
                    string.Equals(m.MajorName.Trim(), info.major.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    m.MajorName.Trim().ToLower().Contains(majorText) ||
                    majorText.Contains(m.MajorName.Trim().ToLower())
                );
                if (matchedMajor != null && matchedMajor.FacultyId > 0)
                {
                    resolvedFacultyId = matchedMajor.FacultyId;
                }
            }

            string? facultyName = null;
            if (resolvedFacultyId.HasValue && resolvedFacultyId.Value > 0 && faculties.TryGetValue(resolvedFacultyId.Value, out var fname))
            {
                facultyName = fname;
            }

            return new StudentPersonalInfoResponse
            {
                Id = info.Id,
                UserId = info.UserId,
                NewStudentAccId = info.NewStudentAccId,
                AdmissionSerialNo = info.AdmissionSerialNo,
                academic_year_range = info.academic_year_range,
                academic_year_level = info.academic_year_level,
                major = info.major,
                FacultyId = resolvedFacultyId,
                FacultyName = facultyName,
                roll_no = info.roll_no,
                university_reg_no = info.university_reg_no,
                admission_year = info.admission_year,
                student_name_mm = info.student_name_mm,
                student_name_en = info.student_name_en,
                mother_name = info.mother_name,
                father_name = info.father_name,
                gender_relation = info.gender_relation,
                ethnicity = info.ethnicity,
                religion = info.religion,
                pob = info.pob,
                birth_place_region = info.birth_place_region,
                student_nrc_no = info.student_nrc_no,
                nationality_status = info.nationality_status,
                dob = info.dob,
                email = info.email,
                blood_type = info.blood_type,
                covid_vaccine_status = info.covid_vaccine_status,
                current_address = info.current_address,
                permanent_address_mm = info.permanent_address_mm,
                permanent_address_en = info.permanent_address_en,
                matric_roll_no = info.matric_roll_no,
                matric_passed_year = info.matric_passed_year,
                exam_center = info.exam_center,
                father_occupation = info.father_occupation,
                mother_occupation = info.mother_occupation,
                past_exam_major = info.past_exam_major,
                past_exam_roll_no = info.past_exam_roll_no,
                past_exam_year = info.past_exam_year,
                past_exam_status = info.past_exam_status,
                previous_year_roll_no = info.previous_year_roll_no,
                guardian_name = info.guardian_name,
                guardian_relationship = info.guardian_relationship,
                guardian_occupation = info.guardian_occupation,
                guardian_address_phone = info.guardian_address_phone,
                app_guardian_name = info.app_guardian_name,
                app_guardian_nrc = info.app_guardian_nrc,
                app_guardian_phone = info.app_guardian_phone,
                app_guardian_address = info.app_guardian_address,
                app_student_name = info.app_student_name,
                app_student_phone = info.app_student_phone,
                stipend_requested = info.stipend_requested,
                nrc_state = info.nrc_state,
                nrc_township = info.nrc_township,
                nrc_type = info.nrc_type,
                nrc_number = info.nrc_number,
                CreatedDateTime = info.CreatedDateTime,
                ModifiedDateTime = info.ModifiedDateTime
            };
        }).ToList();

        return Ok(response);
    }

    [HttpGet("by-roll/{rollNo}")]
    public IActionResult GetByRollNo(string rollNo)
    {
        var info = _db.StudentPersonalInfos.FirstOrDefault(x => x.roll_no != null && x.roll_no.ToLower() == rollNo.ToLower());
        if (info == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "No personal info found for this Roll No." });

        var (facId, facName) = ResolveFacultyInfo(info.UserId, info.major);

        var response = new StudentPersonalInfoResponse
        {
            Id = info.Id,
            UserId = info.UserId,
            NewStudentAccId = info.NewStudentAccId,
            AdmissionSerialNo = info.AdmissionSerialNo,
            academic_year_range = info.academic_year_range,
            academic_year_level = info.academic_year_level,
            major = info.major,
            FacultyId = facId,
            FacultyName = facName,
            roll_no = info.roll_no,
            university_reg_no = info.university_reg_no,
            admission_year = info.admission_year,
            student_name_mm = info.student_name_mm,
            student_name_en = info.student_name_en,
            mother_name = info.mother_name,
            father_name = info.father_name,
            gender_relation = info.gender_relation,
            ethnicity = info.ethnicity,
            religion = info.religion,
            pob = info.pob,
            birth_place_region = info.birth_place_region,
            student_nrc_no = info.student_nrc_no,
            nationality_status = info.nationality_status,
            dob = info.dob,
            email = info.email,
            blood_type = info.blood_type,
            covid_vaccine_status = info.covid_vaccine_status,
            current_address = info.current_address,
            permanent_address_mm = info.permanent_address_mm,
            permanent_address_en = info.permanent_address_en,
            matric_roll_no = info.matric_roll_no,
            matric_passed_year = info.matric_passed_year,
            exam_center = info.exam_center,
            father_occupation = info.father_occupation,
            mother_occupation = info.mother_occupation,
            past_exam_major = info.past_exam_major,
            past_exam_roll_no = info.past_exam_roll_no,
            past_exam_year = info.past_exam_year,
            past_exam_status = info.past_exam_status,
            previous_year_roll_no = info.previous_year_roll_no,
            guardian_name = info.guardian_name,
            guardian_relationship = info.guardian_relationship,
            guardian_occupation = info.guardian_occupation,
            guardian_address_phone = info.guardian_address_phone,
            app_guardian_name = info.app_guardian_name,
            app_guardian_nrc = info.app_guardian_nrc,
            app_guardian_phone = info.app_guardian_phone,
            app_guardian_address = info.app_guardian_address,
            app_student_name = info.app_student_name,
            app_student_phone = info.app_student_phone,
            stipend_requested = info.stipend_requested,
            nrc_state = info.nrc_state,
            nrc_township = info.nrc_township,
            nrc_type = info.nrc_type,
            nrc_number = info.nrc_number,
            CreatedDateTime = info.CreatedDateTime,
            ModifiedDateTime = info.ModifiedDateTime
        };
        return Ok(response);
    }

    private (int? FacultyId, string? FacultyName) ResolveFacultyInfo(int? userId, string? major)
    {
        int? facultyId = null;
        if (userId.HasValue && userId.Value > 0)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == userId.Value);
            if (user?.FacultyId != null && user.FacultyId > 0)
                facultyId = user.FacultyId;
        }

        if ((!facultyId.HasValue || facultyId.Value <= 0) && !string.IsNullOrWhiteSpace(major))
        {
            var majorText = major.Trim().ToLower();
            var allMajors = _db.Majors.Where(m => m.IsDelete == false || m.IsDelete == null).ToList();
            var matchedMajor = allMajors.FirstOrDefault(m =>
                string.Equals(m.MajorName.Trim(), major.Trim(), StringComparison.OrdinalIgnoreCase) ||
                m.MajorName.Trim().ToLower().Contains(majorText) ||
                majorText.Contains(m.MajorName.Trim().ToLower())
            );
            if (matchedMajor != null && matchedMajor.FacultyId > 0)
                facultyId = matchedMajor.FacultyId;
        }

        string? facultyName = null;
        if (facultyId.HasValue && facultyId.Value > 0)
        {
            facultyName = _db.Faculties.FirstOrDefault(f => f.FacultyId == facultyId.Value)?.FacultyName;
        }

        return (facultyId, facultyName);
    }

    [HttpGet("newstudent/{newStudentAccId}")]
    public IActionResult GetPersonalInfoForNewStudent(int newStudentAccId)
    {
        var info = _db.StudentPersonalInfos.FirstOrDefault(x => x.NewStudentAccId == newStudentAccId);
        if (info == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "No personal info found." });

        var (facId, facName) = ResolveFacultyInfo(info.UserId, info.major);

        var response = new StudentPersonalInfoResponse
        {
            Id = info.Id,
            UserId = info.UserId,
            NewStudentAccId = info.NewStudentAccId,
            AdmissionSerialNo = info.AdmissionSerialNo,
            academic_year_range = info.academic_year_range,
            academic_year_level = info.academic_year_level,
            major = info.major,
            FacultyId = facId,
            FacultyName = facName,
            roll_no = info.roll_no,
            university_reg_no = info.university_reg_no,
            admission_year = info.admission_year,
            student_name_mm = info.student_name_mm,
            student_name_en = info.student_name_en,
            mother_name = info.mother_name,
            father_name = info.father_name,
            gender_relation = info.gender_relation,
            ethnicity = info.ethnicity,
            religion = info.religion,
            pob = info.pob,
            birth_place_region = info.birth_place_region,
            student_nrc_no = info.student_nrc_no,
            nationality_status = info.nationality_status,
            dob = info.dob,
            email = info.email,
            blood_type = info.blood_type,
            covid_vaccine_status = info.covid_vaccine_status,
            current_address = info.current_address,
            permanent_address_mm = info.permanent_address_mm,
            permanent_address_en = info.permanent_address_en,
            matric_roll_no = info.matric_roll_no,
            matric_passed_year = info.matric_passed_year,
            exam_center = info.exam_center,
            father_occupation = info.father_occupation,
            mother_occupation = info.mother_occupation,
            past_exam_major = info.past_exam_major,
            past_exam_roll_no = info.past_exam_roll_no,
            past_exam_year = info.past_exam_year,
            past_exam_status = info.past_exam_status,
            previous_year_roll_no = info.previous_year_roll_no,
            guardian_name = info.guardian_name,
            guardian_relationship = info.guardian_relationship,
            guardian_occupation = info.guardian_occupation,
            guardian_address_phone = info.guardian_address_phone,
            app_guardian_name = info.app_guardian_name,
            app_guardian_nrc = info.app_guardian_nrc,
            app_guardian_phone = info.app_guardian_phone,
            app_guardian_address = info.app_guardian_address,
            app_student_name = info.app_student_name,
            app_student_phone = info.app_student_phone,
            stipend_requested = info.stipend_requested,
            nrc_state = info.nrc_state,
            nrc_township = info.nrc_township,
            nrc_type = info.nrc_type,
            nrc_number = info.nrc_number,
            CreatedDateTime = info.CreatedDateTime,
            ModifiedDateTime = info.ModifiedDateTime
        };

        return Ok(response);
    }

    [HttpGet("{userId}")]
    public IActionResult GetPersonalInfo(int userId)
    {
        var info = _db.StudentPersonalInfos.FirstOrDefault(x => x.UserId == userId);
        if (info == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "No personal info found." });

        var (facId, facName) = ResolveFacultyInfo(info.UserId, info.major);

        var response = new StudentPersonalInfoResponse
        {
            Id = info.Id,
            UserId = info.UserId,
            NewStudentAccId = info.NewStudentAccId,
            AdmissionSerialNo = info.AdmissionSerialNo,
            academic_year_range = info.academic_year_range,
            academic_year_level = info.academic_year_level,
            major = info.major,
            FacultyId = facId,
            FacultyName = facName,
            roll_no = info.roll_no,
            university_reg_no = info.university_reg_no,
            admission_year = info.admission_year,
            student_name_mm = info.student_name_mm,
            student_name_en = info.student_name_en,
            mother_name = info.mother_name,
            father_name = info.father_name,
            gender_relation = info.gender_relation,
            ethnicity = info.ethnicity,
            religion = info.religion,
            pob = info.pob,
            birth_place_region = info.birth_place_region,
            student_nrc_no = info.student_nrc_no,
            nationality_status = info.nationality_status,
            dob = info.dob,
            email = info.email,
            blood_type = info.blood_type,
            covid_vaccine_status = info.covid_vaccine_status,
            current_address = info.current_address,
            permanent_address_mm = info.permanent_address_mm,
            permanent_address_en = info.permanent_address_en,
            matric_roll_no = info.matric_roll_no,
            matric_passed_year = info.matric_passed_year,
            exam_center = info.exam_center,
            father_occupation = info.father_occupation,
            mother_occupation = info.mother_occupation,
            past_exam_major = info.past_exam_major,
            past_exam_roll_no = info.past_exam_roll_no,
            past_exam_year = info.past_exam_year,
            past_exam_status = info.past_exam_status,
            previous_year_roll_no = info.previous_year_roll_no,
            guardian_name = info.guardian_name,
            guardian_relationship = info.guardian_relationship,
            guardian_occupation = info.guardian_occupation,
            guardian_address_phone = info.guardian_address_phone,
            app_guardian_name = info.app_guardian_name,
            app_guardian_nrc = info.app_guardian_nrc,
            app_guardian_phone = info.app_guardian_phone,
            app_guardian_address = info.app_guardian_address,
            app_student_name = info.app_student_name,
            app_student_phone = info.app_student_phone,
            stipend_requested = info.stipend_requested,
            nrc_state = info.nrc_state,
            nrc_township = info.nrc_township,
            nrc_type = info.nrc_type,
            nrc_number = info.nrc_number,
            CreatedDateTime = info.CreatedDateTime,
            ModifiedDateTime = info.ModifiedDateTime
        };

        return Ok(response);
    }

    [HttpPost("newstudent/{newStudentAccId}")]
    public IActionResult CreatePersonalInfoForNewStudent(int newStudentAccId, [FromBody] StudentPersonalInfoRequest request)
    {
        if (request == null)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Invalid request." });

        var acc = _db.NewStudentAccs.FirstOrDefault(x => x.NewStudentAccId == newStudentAccId);
        if (acc == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "New student account not found." });

        var existingInfo = _db.StudentPersonalInfos.FirstOrDefault(x => x.NewStudentAccId == newStudentAccId);
        if (existingInfo != null)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Personal info already exists. Use PUT to update." });

        var newInfo = new StudentPersonalInfo
        {
            UserId = 0,
            NewStudentAccId = newStudentAccId,
            AdmissionSerialNo = request.AdmissionSerialNo,
            academic_year_range = request.academic_year_range,
            academic_year_level = request.academic_year_level,
            major = request.major,
            roll_no = request.roll_no,
            university_reg_no = request.university_reg_no,
            admission_year = request.admission_year,
            student_name_mm = request.student_name_mm,
            student_name_en = request.student_name_en,
            mother_name = request.mother_name,
            father_name = request.father_name,
            gender_relation = request.gender_relation,
            ethnicity = request.ethnicity,
            religion = request.religion,
            pob = request.pob,
            birth_place_region = request.birth_place_region,
            student_nrc_no = request.student_nrc_no,
            nationality_status = request.nationality_status,
            dob = request.dob,
            email = request.email,
            blood_type = request.blood_type,
            covid_vaccine_status = request.covid_vaccine_status,
            current_address = request.current_address,
            permanent_address_mm = request.permanent_address_mm,
            permanent_address_en = request.permanent_address_en,
            matric_roll_no = request.matric_roll_no,
            matric_passed_year = request.matric_passed_year,
            exam_center = request.exam_center,
            father_occupation = request.father_occupation,
            mother_occupation = request.mother_occupation,
            past_exam_major = request.past_exam_major,
            past_exam_roll_no = request.past_exam_roll_no,
            past_exam_year = request.past_exam_year,
            past_exam_status = request.past_exam_status,
            previous_year_roll_no = request.previous_year_roll_no,
            guardian_name = request.guardian_name,
            guardian_relationship = request.guardian_relationship,
            guardian_occupation = request.guardian_occupation,
            guardian_address_phone = request.guardian_address_phone,
            app_guardian_name = request.app_guardian_name,
            app_guardian_nrc = request.app_guardian_nrc,
            app_guardian_phone = request.app_guardian_phone,
            app_guardian_address = request.app_guardian_address,
            app_student_name = request.app_student_name,
            app_student_phone = request.app_student_phone,
            stipend_requested = request.stipend_requested,
            nrc_state = request.nrc_state,
            nrc_township = request.nrc_township,
            nrc_type = request.nrc_type,
            nrc_number = request.nrc_number,
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        };

        _db.StudentPersonalInfos.Add(newInfo);
        _db.SaveChanges();

        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Personal info created successfully." });
    }

    [HttpPost("{userId}")]
    public IActionResult CreatePersonalInfo(int userId, [FromBody] StudentPersonalInfoRequest request)
    {
        if (request == null)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Invalid request." });

        var user = _db.Users.FirstOrDefault(x => x.UserId == userId);
        if (user == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "User not found." });

        var existingInfo = _db.StudentPersonalInfos.FirstOrDefault(x => x.UserId == userId);
        if (existingInfo != null)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Personal info already exists. Use PUT to update." });

        var newInfo = new StudentPersonalInfo
        {
            UserId = userId,
            AdmissionSerialNo = request.AdmissionSerialNo,
            academic_year_range = request.academic_year_range,
            academic_year_level = request.academic_year_level,
            major = request.major,
            roll_no = request.roll_no,
            university_reg_no = request.university_reg_no,
            admission_year = request.admission_year,
            student_name_mm = request.student_name_mm,
            student_name_en = request.student_name_en,
            mother_name = request.mother_name,
            father_name = request.father_name,
            gender_relation = request.gender_relation,
            ethnicity = request.ethnicity,
            religion = request.religion,
            pob = request.pob,
            birth_place_region = request.birth_place_region,
            student_nrc_no = request.student_nrc_no,
            nationality_status = request.nationality_status,
            dob = request.dob,
            email = request.email,
            blood_type = request.blood_type,
            covid_vaccine_status = request.covid_vaccine_status,
            current_address = request.current_address,
            permanent_address_mm = request.permanent_address_mm,
            permanent_address_en = request.permanent_address_en,
            matric_roll_no = request.matric_roll_no,
            matric_passed_year = request.matric_passed_year,
            exam_center = request.exam_center,
            father_occupation = request.father_occupation,
            mother_occupation = request.mother_occupation,
            past_exam_major = request.past_exam_major,
            past_exam_roll_no = request.past_exam_roll_no,
            past_exam_year = request.past_exam_year,
            past_exam_status = request.past_exam_status,
            previous_year_roll_no = request.previous_year_roll_no,
            guardian_name = request.guardian_name,
            guardian_relationship = request.guardian_relationship,
            guardian_occupation = request.guardian_occupation,
            guardian_address_phone = request.guardian_address_phone,
            app_guardian_name = request.app_guardian_name,
            app_guardian_nrc = request.app_guardian_nrc,
            app_guardian_phone = request.app_guardian_phone,
            app_guardian_address = request.app_guardian_address,
            app_student_name = request.app_student_name,
            app_student_phone = request.app_student_phone,
            stipend_requested = request.stipend_requested,
            nrc_state = request.nrc_state,
            nrc_township = request.nrc_township,
            nrc_type = request.nrc_type,
            nrc_number = request.nrc_number,
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        };

        _db.StudentPersonalInfos.Add(newInfo);
        _db.SaveChanges();

        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Personal info created successfully." });
    }

    [HttpPut("newstudent/{newStudentAccId}")]
    public IActionResult UpdatePersonalInfoForNewStudent(int newStudentAccId, [FromBody] StudentPersonalInfoRequest request)
    {
        if (request == null)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Invalid request." });

        var info = _db.StudentPersonalInfos.FirstOrDefault(x => x.NewStudentAccId == newStudentAccId);
        if (info == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "Personal info not found." });

        info.AdmissionSerialNo = request.AdmissionSerialNo;
        info.academic_year_range = request.academic_year_range;
        info.academic_year_level = request.academic_year_level;
        info.major = request.major;
        info.roll_no = request.roll_no;
        info.university_reg_no = request.university_reg_no;
        info.admission_year = request.admission_year;
        info.student_name_mm = request.student_name_mm;
        info.student_name_en = request.student_name_en;
        info.mother_name = request.mother_name;
        info.father_name = request.father_name;
        info.gender_relation = request.gender_relation;
        info.ethnicity = request.ethnicity;
        info.religion = request.religion;
        info.pob = request.pob;
        info.birth_place_region = request.birth_place_region;
        info.student_nrc_no = request.student_nrc_no;
        info.nationality_status = request.nationality_status;
        info.dob = request.dob;
        info.email = request.email;
        info.blood_type = request.blood_type;
        info.covid_vaccine_status = request.covid_vaccine_status;
        info.current_address = request.current_address;
        info.permanent_address_mm = request.permanent_address_mm;
        info.permanent_address_en = request.permanent_address_en;
        info.matric_roll_no = request.matric_roll_no;
        info.matric_passed_year = request.matric_passed_year;
        info.exam_center = request.exam_center;
        info.father_occupation = request.father_occupation;
        info.mother_occupation = request.mother_occupation;
        info.past_exam_major = request.past_exam_major;
        info.past_exam_roll_no = request.past_exam_roll_no;
        info.past_exam_year = request.past_exam_year;
        info.past_exam_status = request.past_exam_status;
        info.previous_year_roll_no = request.previous_year_roll_no;
        info.guardian_name = request.guardian_name;
        info.guardian_relationship = request.guardian_relationship;
        info.guardian_occupation = request.guardian_occupation;
        info.guardian_address_phone = request.guardian_address_phone;
        info.app_guardian_name = request.app_guardian_name;
        info.app_guardian_nrc = request.app_guardian_nrc;
        info.app_guardian_phone = request.app_guardian_phone;
        info.app_guardian_address = request.app_guardian_address;
        info.app_student_name = request.app_student_name;
        info.app_student_phone = request.app_student_phone;
        info.stipend_requested = request.stipend_requested;
        info.nrc_state = request.nrc_state;
        info.nrc_township = request.nrc_township;
        info.nrc_type = request.nrc_type;
        info.nrc_number = request.nrc_number;
        info.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        _db.StudentPersonalInfos.Update(info);

        SyncFacultyAndRegistration(0, newStudentAccId, request.FacultyId, request.major, request.student_name_mm, request.academic_year_level);

        _db.SaveChanges();

        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Personal info updated successfully." });
    }

    [HttpPut("{userId}")]
    public IActionResult UpdatePersonalInfo(int userId, [FromBody] StudentPersonalInfoRequest request)
    {
        if (request == null)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Invalid request." });

        var info = _db.StudentPersonalInfos.FirstOrDefault(x => x.UserId == userId);
        if (info == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "Personal info not found." });

        info.AdmissionSerialNo = request.AdmissionSerialNo;
        info.academic_year_range = request.academic_year_range;
        info.academic_year_level = request.academic_year_level;
        info.major = request.major;
        info.roll_no = request.roll_no;
        info.university_reg_no = request.university_reg_no;
        info.admission_year = request.admission_year;
        info.student_name_mm = request.student_name_mm;
        info.student_name_en = request.student_name_en;
        info.mother_name = request.mother_name;
        info.father_name = request.father_name;
        info.gender_relation = request.gender_relation;
        info.ethnicity = request.ethnicity;
        info.religion = request.religion;
        info.pob = request.pob;
        info.birth_place_region = request.birth_place_region;
        info.student_nrc_no = request.student_nrc_no;
        info.nationality_status = request.nationality_status;
        info.dob = request.dob;
        info.email = request.email;
        info.blood_type = request.blood_type;
        info.covid_vaccine_status = request.covid_vaccine_status;
        info.current_address = request.current_address;
        info.permanent_address_mm = request.permanent_address_mm;
        info.permanent_address_en = request.permanent_address_en;
        info.matric_roll_no = request.matric_roll_no;
        info.matric_passed_year = request.matric_passed_year;
        info.exam_center = request.exam_center;
        info.father_occupation = request.father_occupation;
        info.mother_occupation = request.mother_occupation;
        info.past_exam_major = request.past_exam_major;
        info.past_exam_roll_no = request.past_exam_roll_no;
        info.past_exam_year = request.past_exam_year;
        info.past_exam_status = request.past_exam_status;
        info.previous_year_roll_no = request.previous_year_roll_no;
        info.guardian_name = request.guardian_name;
        info.guardian_relationship = request.guardian_relationship;
        info.guardian_occupation = request.guardian_occupation;
        info.guardian_address_phone = request.guardian_address_phone;
        info.app_guardian_name = request.app_guardian_name;
        info.app_guardian_nrc = request.app_guardian_nrc;
        info.app_guardian_phone = request.app_guardian_phone;
        info.app_guardian_address = request.app_guardian_address;
        info.app_student_name = request.app_student_name;
        info.app_student_phone = request.app_student_phone;
        info.stipend_requested = request.stipend_requested;
        info.nrc_state = request.nrc_state;
        info.nrc_township = request.nrc_township;
        info.nrc_type = request.nrc_type;
        info.nrc_number = request.nrc_number;
        info.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        _db.StudentPersonalInfos.Update(info);

        SyncFacultyAndRegistration(userId, 0, request.FacultyId, request.major, request.student_name_mm, request.academic_year_level);

        _db.SaveChanges();

        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Personal info updated successfully." });
    }

    private void SyncFacultyAndRegistration(int? userId, int? newStudentAccId, int? requestFacultyId, string? requestMajor, string? studentNameMm, string? academicYearLevel)
    {
        int? facultyIdToSave = requestFacultyId;
        if ((!facultyIdToSave.HasValue || facultyIdToSave.Value <= 0) && !string.IsNullOrWhiteSpace(requestMajor))
        {
            var majorText = requestMajor.Trim().ToLower();
            var allMajors = _db.Majors.Where(m => m.IsDelete == false || m.IsDelete == null).ToList();
            var matchedMajor = allMajors.FirstOrDefault(m =>
                string.Equals(m.MajorName.Trim(), requestMajor.Trim(), StringComparison.OrdinalIgnoreCase) ||
                m.MajorName.Trim().ToLower().Contains(majorText) ||
                majorText.Contains(m.MajorName.Trim().ToLower())
            );
            if (matchedMajor != null && matchedMajor.FacultyId > 0)
            {
                facultyIdToSave = matchedMajor.FacultyId;
            }
        }

        User? user = null;
        if (userId.HasValue && userId.Value > 0)
        {
            user = _db.Users.FirstOrDefault(x => x.UserId == userId.Value);
        }

        if (user != null && facultyIdToSave.HasValue && facultyIdToSave.Value > 0)
        {
            user.FacultyId = facultyIdToSave.Value;
            _db.Users.Update(user);
        }

        StudentRegistration? reg = null;
        if (userId.HasValue && userId.Value > 0)
        {
            reg = _db.StudentRegistrations.FirstOrDefault(x => x.UserId == userId.Value && (x.IsDelete == false || x.IsDelete == null));
        }
        else if (newStudentAccId.HasValue && newStudentAccId.Value > 0)
        {
            reg = _db.StudentRegistrations.FirstOrDefault(x => x.NewStudentAccId == newStudentAccId.Value && (x.IsDelete == false || x.IsDelete == null));
        }

        if (reg != null)
        {
            if (!string.IsNullOrWhiteSpace(requestMajor)) reg.Major = requestMajor;
            if (!string.IsNullOrWhiteSpace(studentNameMm)) reg.StudentNameMm = studentNameMm;
            if (!string.IsNullOrWhiteSpace(academicYearLevel)) reg.AcademicYearLevel = academicYearLevel;
            _db.StudentRegistrations.Update(reg);
        }
    }
}
