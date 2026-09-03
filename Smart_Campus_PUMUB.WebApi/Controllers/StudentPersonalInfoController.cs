using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentPersonalInfoController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly IEnrollmentService _enrollmentService;

    public StudentPersonalInfoController(SmartCampusDbContext db, IEnrollmentService enrollmentService)
    {
        _db = db;
        _enrollmentService = enrollmentService;
    }

    [HttpGet]
    public IActionResult GetAllPersonalInfos([FromQuery] int? facultyId = null)
    {
        // Faculty-based scoping: join PersonalInfo → User to filter by FacultyId
        IQueryable<StudentPersonalInfo> query = _db.StudentPersonalInfos;

        var users = _db.Users.AsNoTracking().ToDictionary(u => u.UserId, u => u);
        var faculties = _db.Faculties.Where(f => f.IsDelete == false || f.IsDelete == null).AsNoTracking().ToDictionary(f => f.FacultyId, f => f.FacultyName);
        var majors = _db.Majors.Where(m => m.IsDelete == false || m.IsDelete == null).AsNoTracking().ToList();
        var allStudents = _db.Students.Where(s => s.IsDelete == false || s.IsDelete == null).AsNoTracking().ToList();
        var studentsByUserId = allStudents.Where(s => s.UserId > 0).GroupBy(s => s.UserId).ToDictionary(g => g.Key, g => g.First());
        var studentsByRoll = allStudents.Where(s => !string.IsNullOrWhiteSpace(s.CurrentRollNo)).GroupBy(s => s.CurrentRollNo!.Trim().ToLower()).ToDictionary(g => g.Key, g => g.First());

        var disqualifiedRegIds = _db.StudentSubjectResults
            .Where(r => r.IsDisqualified && r.RegistrationId.HasValue)
            .Select(r => r.RegistrationId!.Value)
            .ToHashSet();

        var disqualifiedRegs = _db.StudentRegistrations
            .Where(r => disqualifiedRegIds.Contains(r.RegistrationId))
            .ToList();

        var disqualifiedUserIds = disqualifiedRegs
            .Where(r => r.UserId.HasValue)
            .Select(r => r.UserId!.Value)
            .ToHashSet();

        var disqualifiedRollNos = disqualifiedRegs
            .Where(r => !string.IsNullOrWhiteSpace(r.RollNo))
            .Select(r => r.RollNo!.Trim().ToLower())
            .ToHashSet();

        if (facultyId.HasValue && facultyId.Value > 0)
        {
            // Only return records whose linked User belongs to the specified Faculty
            var userIdsInFaculty = _db.Users
                .Where(u => u.FacultyId == facultyId.Value)
                .Select(u => u.UserId)
                .ToHashSet();

            var majorsInFaculty = majors
                .Where(m => m.FacultyId == facultyId.Value)
                .Select(m => m.MajorName.Trim().ToLower())
                .ToHashSet();

            query = query.Where(info => userIdsInFaculty.Contains(info.UserId) ||
                (info.major != null && majorsInFaculty.Contains(info.major.Trim().ToLower())));
        }

        var infos = query.ToList();
        var response = infos.Select(info => MapToResponse(info, users, faculties, majors, studentsByUserId, studentsByRoll, disqualifiedUserIds, disqualifiedRollNos)).ToList();

        return Ok(response);
    }

    [HttpGet("paginate")]
    public IActionResult GetPaginatedPersonalInfos(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? facultyId = null,
        [FromQuery] string? major = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        IQueryable<StudentPersonalInfo> query = _db.StudentPersonalInfos;

        var users = _db.Users.AsNoTracking().ToDictionary(u => u.UserId, u => u);
        var faculties = _db.Faculties.Where(f => f.IsDelete == false || f.IsDelete == null).AsNoTracking().ToDictionary(f => f.FacultyId, f => f.FacultyName);
        var majors = _db.Majors.Where(m => m.IsDelete == false || m.IsDelete == null).AsNoTracking().ToList();
        var allStudents = _db.Students.Where(s => s.IsDelete == false || s.IsDelete == null).AsNoTracking().ToList();
        var studentsByUserId = allStudents.Where(s => s.UserId > 0).GroupBy(s => s.UserId).ToDictionary(g => g.Key, g => g.First());
        var studentsByRoll = allStudents.Where(s => !string.IsNullOrWhiteSpace(s.CurrentRollNo)).GroupBy(s => s.CurrentRollNo!.Trim().ToLower()).ToDictionary(g => g.Key, g => g.First());

        var disqualifiedRegIds = _db.StudentSubjectResults
            .Where(r => r.IsDisqualified && r.RegistrationId.HasValue)
            .Select(r => r.RegistrationId!.Value)
            .ToHashSet();

        var disqualifiedRegs = _db.StudentRegistrations
            .Where(r => disqualifiedRegIds.Contains(r.RegistrationId))
            .ToList();

        var disqualifiedUserIds = disqualifiedRegs
            .Where(r => r.UserId.HasValue)
            .Select(r => r.UserId!.Value)
            .ToHashSet();

        var disqualifiedRollNos = disqualifiedRegs
            .Where(r => !string.IsNullOrWhiteSpace(r.RollNo))
            .Select(r => r.RollNo!.Trim().ToLower())
            .ToHashSet();

        if (facultyId.HasValue && facultyId.Value > 0)
        {
            var userIdsInFaculty = _db.Users
                .Where(u => u.FacultyId == facultyId.Value)
                .Select(u => u.UserId)
                .ToHashSet();

            var majorsInFaculty = majors
                .Where(m => m.FacultyId == facultyId.Value)
                .Select(m => m.MajorName.Trim().ToLower())
                .ToHashSet();

            query = query.Where(info => userIdsInFaculty.Contains(info.UserId) ||
                (info.major != null && majorsInFaculty.Contains(info.major.Trim().ToLower())));
        }

        if (!string.IsNullOrWhiteSpace(major) && !string.Equals(major.Trim(), "All", StringComparison.OrdinalIgnoreCase))
        {
            var mLower = major.Trim().ToLower();
            query = query.Where(x => x.major != null && (x.major.ToLower() == mLower || x.major.ToLower().Contains(mLower)));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                (x.student_name_mm != null && x.student_name_mm.ToLower().Contains(term)) ||
                (x.student_name_en != null && x.student_name_en.ToLower().Contains(term)) ||
                (x.roll_no != null && x.roll_no.ToLower().Contains(term)) ||
                (x.father_name != null && x.father_name.ToLower().Contains(term)) ||
                (x.mother_name != null && x.mother_name.ToLower().Contains(term)) ||
                (x.email != null && x.email.ToLower().Contains(term)) ||
                (x.app_student_phone != null && x.app_student_phone.ToLower().Contains(term)) ||
                (x.matric_roll_no != null && x.matric_roll_no.ToLower().Contains(term)) ||
                (x.university_reg_no != null && x.university_reg_no.ToLower().Contains(term)) ||
                (x.major != null && x.major.ToLower().Contains(term))
            );
        }

        int total = query.Count();

        var pagedInfos = query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pagedInfos.Select(info => MapToResponse(info, users, faculties, majors, studentsByUserId, studentsByRoll, disqualifiedUserIds, disqualifiedRollNos)).ToList();

        return Ok(new PagedResult<StudentPersonalInfoResponse>
        {
            Items = items,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    private static StudentPersonalInfoResponse MapToResponse(
        StudentPersonalInfo info,
        Dictionary<int, User> users,
        Dictionary<int, string> faculties,
        List<Major> majors,
        Dictionary<int, Student>? studentsByUserId = null,
        Dictionary<string, Student>? studentsByRollNo = null,
        HashSet<int>? disqualifiedUserIds = null,
        HashSet<string>? disqualifiedRollNos = null)
    {
        int? resolvedFacultyId = null;
        if (info.UserId > 0 && users.TryGetValue(info.UserId, out var u) && u.FacultyId.HasValue && u.FacultyId.Value > 0)
        {
            resolvedFacultyId = u.FacultyId.Value;
        }

        string? resolvedMajor = !string.IsNullOrWhiteSpace(info.major) && info.major.Trim() != "-" ? info.major.Trim() : null;
        if (string.IsNullOrWhiteSpace(resolvedMajor) && info.UserId > 0 && studentsByUserId != null && studentsByUserId.TryGetValue(info.UserId, out var stUserForMajor) && !string.IsNullOrWhiteSpace(stUserForMajor.CurrentMajor) && stUserForMajor.CurrentMajor.Trim() != "-")
        {
            resolvedMajor = stUserForMajor.CurrentMajor.Trim();
        }
        else if (string.IsNullOrWhiteSpace(resolvedMajor) && !string.IsNullOrWhiteSpace(info.roll_no) && studentsByRollNo != null && studentsByRollNo.TryGetValue(info.roll_no.Trim().ToLower(), out var stRollForMajor) && !string.IsNullOrWhiteSpace(stRollForMajor.CurrentMajor) && stRollForMajor.CurrentMajor.Trim() != "-")
        {
            resolvedMajor = stRollForMajor.CurrentMajor.Trim();
        }
        if (string.IsNullOrWhiteSpace(resolvedMajor))
        {
            resolvedMajor = info.major ?? "-";
        }

        if ((!resolvedFacultyId.HasValue || resolvedFacultyId.Value <= 0) && !string.IsNullOrWhiteSpace(resolvedMajor) && resolvedMajor != "-")
        {
            var majorText = resolvedMajor.Trim().ToLower();
            var matchedMajor = majors.FirstOrDefault(m =>
                string.Equals(m.MajorName.Trim(), resolvedMajor, StringComparison.OrdinalIgnoreCase) ||
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

        string? studentStatus = null;
        if (info.UserId > 0 && studentsByUserId != null && studentsByUserId.TryGetValue(info.UserId, out var stUser))
        {
            studentStatus = stUser.Status;
        }
        else if (!string.IsNullOrWhiteSpace(info.roll_no) && studentsByRollNo != null && studentsByRollNo.TryGetValue(info.roll_no.Trim().ToLower(), out var stRoll))
        {
            studentStatus = stRoll.Status;
        }

        bool isDisqualified = (info.UserId > 0 && disqualifiedUserIds != null && disqualifiedUserIds.Contains(info.UserId)) ||
                              (!string.IsNullOrWhiteSpace(info.roll_no) && disqualifiedRollNos != null && disqualifiedRollNos.Contains(info.roll_no.Trim().ToLower())) ||
                              string.Equals(studentStatus, "Disqualified", StringComparison.OrdinalIgnoreCase);

        bool isGrad = !isDisqualified && string.Equals(studentStatus, "Graduated", StringComparison.OrdinalIgnoreCase);

        return new StudentPersonalInfoResponse
        {
            Id = info.Id,
            UserId = info.UserId,
            NewStudentAccId = info.NewStudentAccId,
            Status = isDisqualified ? "Disqualified" : (studentStatus ?? "Active"),
            IsGraduated = isGrad,
            IsDisqualified = isDisqualified,
            GraduationStatus = isDisqualified ? "Disqualified" : (isGrad ? "Graduated" : "Studying"),
            AdmissionSerialNo = info.AdmissionSerialNo,
            academic_year_range = info.academic_year_range,
            academic_year_level = info.academic_year_level,
            major = resolvedMajor,
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
            nrc_front_image = info.nrc_front_image,
            nrc_back_image = info.nrc_back_image,
            census_image = info.census_image,
            CreatedDateTime = info.CreatedDateTime,
            ModifiedDateTime = info.ModifiedDateTime
        };
    }

    [HttpGet("by-roll/{rollNo}")]
    public async Task<IActionResult> GetByRollNo(string rollNo)
    {
        if (string.IsNullOrWhiteSpace(rollNo))
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "No personal info found for this Roll No." });

        string cleanRoll = rollNo.Trim().ToLower();

        var info = await _db.StudentPersonalInfos.AsNoTracking().FirstOrDefaultAsync(x => 
            (x.roll_no != null && x.roll_no.Trim().ToLower() == cleanRoll) ||
            (x.previous_year_roll_no != null && x.previous_year_roll_no.Trim().ToLower() == cleanRoll));

        if (info == null)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.RoleNo != null && u.RoleNo.Trim().ToLower() == cleanRoll && (u.IsDelete == false || u.IsDelete == null));
            if (user != null)
            {
                info = await _db.StudentPersonalInfos.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == user.UserId);
            }
        }

        if (info == null)
        {
            var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.CurrentRollNo != null && s.CurrentRollNo.Trim().ToLower() == cleanRoll && (s.IsDelete == false || s.IsDelete == null));
            if (student != null)
            {
                info = await _db.StudentPersonalInfos.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == student.UserId);
            }
        }

        if (info == null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "No personal info found for this Roll No." });

        var (facId, facName) = ResolveFacultyInfo(info.UserId, info.major);

        // Preload Student model and RetakeStatus in the same request to eliminate client-side HTTP waterfall
        StudentModel? studentData = null;
        StudentRetakeStatusModel? retakeStatus = null;

        int? targetUserId = info.UserId > 0 ? info.UserId : null;
        if (targetUserId.HasValue)
        {
            var st = await _db.Students.AsNoTracking().Include(s => s.User).FirstOrDefaultAsync(x => x.UserId == targetUserId.Value && (x.IsDelete == false || x.IsDelete == null));
            if (st != null)
            {
                studentData = new StudentModel
                {
                    StudentId = st.StudentId,
                    UserId = st.UserId,
                    FullName = st.User?.FullName,
                    CurrentRollNo = st.User?.RoleNo ?? st.CurrentRollNo,
                    CurrentMajor = st.CurrentMajor,
                    CurrentClassYear = st.CurrentClassYear,
                    Status = st.Status ?? "Active",
                    Sem1_Result = st.Sem1_Result,
                    Sem2_Result = st.Sem2_Result,
                    Sem3_Result = st.Sem3_Result,
                    Sem4_Result = st.Sem4_Result,
                    Sem5_Result = st.Sem5_Result,
                    Sem6_Result = st.Sem6_Result,
                    Sem7_Result = st.Sem7_Result,
                    Sem8_Result = st.Sem8_Result,
                    Sem9_Result = st.Sem9_Result
                };
            }

            try
            {
                retakeStatus = await _enrollmentService.GetStudentRetakeStatusAsync(targetUserId.Value, st?.StudentId, info.roll_no);
            }
            catch { }
        }

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
            nrc_front_image = info.nrc_front_image,
            nrc_back_image = info.nrc_back_image,
            census_image = info.census_image,
            student_image = info.student_image,
            father_nrc_front_image = info.father_nrc_front_image,
            father_nrc_back_image = info.father_nrc_back_image,
            mother_nrc_front_image = info.mother_nrc_front_image,
            mother_nrc_back_image = info.mother_nrc_back_image,
            StudentData = studentData,
            RetakeStatus = retakeStatus,
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
            var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.UserId == userId.Value);
            if (user?.FacultyId != null && user.FacultyId > 0)
                facultyId = user.FacultyId;
        }

        if ((!facultyId.HasValue || facultyId.Value <= 0) && !string.IsNullOrWhiteSpace(major))
        {
            var majorText = major.Trim().ToLower();
            var allMajors = _db.Majors.AsNoTracking().Where(m => m.IsDelete == false || m.IsDelete == null).ToList();
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
            facultyName = _db.Faculties.AsNoTracking().FirstOrDefault(f => f.FacultyId == facultyId.Value)?.FacultyName;
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
            nrc_front_image = info.nrc_front_image,
            nrc_back_image = info.nrc_back_image,
            census_image = info.census_image,
            student_image = info.student_image,
            father_nrc_front_image = info.father_nrc_front_image,
            father_nrc_back_image = info.father_nrc_back_image,
            mother_nrc_front_image = info.mother_nrc_front_image,
            mother_nrc_back_image = info.mother_nrc_back_image,
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
            nrc_front_image = info.nrc_front_image,
            nrc_back_image = info.nrc_back_image,
            census_image = info.census_image,
            student_image = info.student_image,
            father_nrc_front_image = info.father_nrc_front_image,
            father_nrc_back_image = info.father_nrc_back_image,
            mother_nrc_front_image = info.mother_nrc_front_image,
            mother_nrc_back_image = info.mother_nrc_back_image,
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
            nrc_front_image = request.nrc_front_image,
            nrc_back_image = request.nrc_back_image,
            census_image = request.census_image,
            student_image = request.student_image,
            father_nrc_front_image = request.father_nrc_front_image,
            father_nrc_back_image = request.father_nrc_back_image,
            mother_nrc_front_image = request.mother_nrc_front_image,
            mother_nrc_back_image = request.mother_nrc_back_image,
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
            nrc_front_image = request.nrc_front_image,
            nrc_back_image = request.nrc_back_image,
            census_image = request.census_image,
            student_image = request.student_image,
            father_nrc_front_image = request.father_nrc_front_image,
            father_nrc_back_image = request.father_nrc_back_image,
            mother_nrc_front_image = request.mother_nrc_front_image,
            mother_nrc_back_image = request.mother_nrc_back_image,
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        };

        _db.StudentPersonalInfos.Add(newInfo);
        SyncFacultyAndRegistration(userId, 0, request.FacultyId, request.major, request.student_name_mm, request.academic_year_level, request.roll_no);
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
        if (!string.IsNullOrEmpty(request.nrc_front_image)) info.nrc_front_image = request.nrc_front_image;
        if (!string.IsNullOrEmpty(request.nrc_back_image)) info.nrc_back_image = request.nrc_back_image;
        if (!string.IsNullOrEmpty(request.census_image)) info.census_image = request.census_image;
        if (!string.IsNullOrEmpty(request.student_image)) info.student_image = request.student_image;
        if (!string.IsNullOrEmpty(request.father_nrc_front_image)) info.father_nrc_front_image = request.father_nrc_front_image;
        if (!string.IsNullOrEmpty(request.father_nrc_back_image)) info.father_nrc_back_image = request.father_nrc_back_image;
        if (!string.IsNullOrEmpty(request.mother_nrc_front_image)) info.mother_nrc_front_image = request.mother_nrc_front_image;
        if (!string.IsNullOrEmpty(request.mother_nrc_back_image)) info.mother_nrc_back_image = request.mother_nrc_back_image;
        info.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        _db.StudentPersonalInfos.Update(info);

        SyncFacultyAndRegistration(info.UserId > 0 ? info.UserId : null, newStudentAccId, request.FacultyId, request.major, request.student_name_mm, request.academic_year_level, request.roll_no);

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
        if (!string.IsNullOrEmpty(request.nrc_front_image)) info.nrc_front_image = request.nrc_front_image;
        if (!string.IsNullOrEmpty(request.nrc_back_image)) info.nrc_back_image = request.nrc_back_image;
        if (!string.IsNullOrEmpty(request.census_image)) info.census_image = request.census_image;
        if (!string.IsNullOrEmpty(request.student_image)) info.student_image = request.student_image;
        if (!string.IsNullOrEmpty(request.father_nrc_front_image)) info.father_nrc_front_image = request.father_nrc_front_image;
        if (!string.IsNullOrEmpty(request.father_nrc_back_image)) info.father_nrc_back_image = request.father_nrc_back_image;
        if (!string.IsNullOrEmpty(request.mother_nrc_front_image)) info.mother_nrc_front_image = request.mother_nrc_front_image;
        if (!string.IsNullOrEmpty(request.mother_nrc_back_image)) info.mother_nrc_back_image = request.mother_nrc_back_image;
        info.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        _db.StudentPersonalInfos.Update(info);

        SyncFacultyAndRegistration(userId, 0, request.FacultyId, request.major, request.student_name_mm, request.academic_year_level, request.roll_no);

        _db.SaveChanges();

        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Personal info updated successfully." });
    }

    private void SyncFacultyAndRegistration(int? userId, int? newStudentAccId, int? requestFacultyId, string? requestMajor, string? studentNameMm, string? academicYearLevel, string? requestRollNo)
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

        if (user != null)
        {
            if (facultyIdToSave.HasValue && facultyIdToSave.Value > 0)
            {
                user.FacultyId = facultyIdToSave.Value;
            }
            if (!string.IsNullOrWhiteSpace(studentNameMm))
            {
                user.FullName = studentNameMm;
            }
            if (!string.IsNullOrWhiteSpace(requestRollNo))
            {
                user.RoleNo = requestRollNo;
            }
            _db.Users.Update(user);
        }

        // Sync Student table
        Student? student = null;
        if (userId.HasValue && userId.Value > 0)
        {
            student = _db.Students.FirstOrDefault(s => s.UserId == userId.Value && (s.IsDelete == false || s.IsDelete == null));
        }
        if (student != null)
        {
            if (!string.IsNullOrWhiteSpace(requestMajor)) student.CurrentMajor = requestMajor;
            if (!string.IsNullOrWhiteSpace(academicYearLevel)) student.CurrentClassYear = academicYearLevel;
            if (!string.IsNullOrWhiteSpace(requestRollNo)) student.CurrentRollNo = requestRollNo;
            else if (!string.IsNullOrWhiteSpace(user?.RoleNo)) student.CurrentRollNo = user.RoleNo;
            if (facultyIdToSave.HasValue && facultyIdToSave.Value > 0) student.FacultyId = facultyIdToSave.Value;
            _db.Students.Update(student);
        }

        var regsQuery = _db.StudentRegistrations.Where(x => (x.IsDelete == false || x.IsDelete == null));
        if (userId.HasValue && userId.Value > 0)
        {
            regsQuery = regsQuery.Where(x => x.UserId == userId.Value);
        }
        else if (newStudentAccId.HasValue && newStudentAccId.Value > 0)
        {
            regsQuery = regsQuery.Where(x => x.NewStudentAccId == newStudentAccId.Value);
        }

        var regs = regsQuery.ToList();
        foreach (var reg in regs)
        {
            if (!string.IsNullOrWhiteSpace(requestMajor)) reg.Major = requestMajor;
            if (!string.IsNullOrWhiteSpace(studentNameMm)) reg.StudentNameMm = studentNameMm;
            if (!string.IsNullOrWhiteSpace(requestRollNo)) reg.RollNo = requestRollNo;
            else if (!string.IsNullOrWhiteSpace(user?.RoleNo)) reg.RollNo = user.RoleNo;
            _db.StudentRegistrations.Update(reg);
        }
    }
}
