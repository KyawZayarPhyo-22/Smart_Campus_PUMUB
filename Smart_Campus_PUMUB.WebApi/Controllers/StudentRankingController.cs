using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.Database.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentRankingController : ControllerBase
    {
        private readonly SmartCampusDbContext _context;
        private readonly IEnrollmentService _enrollmentService;

        public StudentRankingController(SmartCampusDbContext context, IEnrollmentService enrollmentService)
        {
            _context = context;
            _enrollmentService = enrollmentService;
        }

        private async Task<Dictionary<string, string>> GetMajorToFacultyMapAsync()
        {
            var majors = await _context.Majors
                .AsNoTracking()
                .Include(m => m.Faculty)
                .Where(m => m.IsDelete == false || m.IsDelete == null)
                .Select(m => new { m.MajorName, FacultyName = m.Faculty != null ? m.Faculty.FacultyName : "" })
                .ToListAsync();

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in majors)
            {
                if (!string.IsNullOrEmpty(m.MajorName) && !string.IsNullOrEmpty(m.FacultyName))
                {
                    map[m.MajorName] = m.FacultyName;
                }
            }
            return map;
        }

        private async Task<Dictionary<string, int>> GetStudentDisqualificationMapAsync()
        {
            var results = await _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.Subject != null && (r.Subject.IsDelete == false || r.Subject.IsDelete == null))
                .Select(r => new
                {
                    r.ResultId,
                    r.StudentId,
                    RollNo = (r.Registration != null && !string.IsNullOrEmpty(r.Registration.RollNo) ? r.Registration.RollNo : null)
                          ?? (r.Student != null && !string.IsNullOrEmpty(r.Student.CurrentRollNo) ? r.Student.CurrentRollNo : null)
                          ?? "",
                    SubjectId = r.SubjectId ?? 0,
                    SemesterId = r.SemesterId ?? (r.Subject != null ? r.Subject.SemesterId : 0),
                    SemesterSequence = r.Semester != null ? r.Semester.Sequence : (r.Subject != null && r.Subject.Semester != null ? r.Subject.Semester.Sequence : 0),
                    Grade = r.Grade ?? "",
                    r.IsPass,
                    r.IsDisqualified,
                    ReexamGrade = r.ReexamGrade ?? "",
                    r.ReexamIsPass
                })
                .ToListAsync();

            var students = await _context.Students
                .AsNoTracking()
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .Select(s => new { s.StudentId, s.CurrentRollNo, s.Status })
                .ToListAsync();

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var st in students)
            {
                if (string.Equals(st.Status, "Disqualified", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(st.CurrentRollNo))
                        map[$"ROLL_{st.CurrentRollNo.Trim().ToUpperInvariant()}"] = 0;
                    if (st.StudentId > 0)
                        map[$"STU_{st.StudentId}"] = 0;
                }
            }

            var studentGroups = results.GroupBy(r => 
                !string.IsNullOrWhiteSpace(r.RollNo) && r.RollNo != "-" 
                    ? $"ROLL_{r.RollNo.Trim().ToUpperInvariant()}" 
                    : (r.StudentId > 0 ? $"STU_{r.StudentId}" : "")
            );

            foreach (var group in studentGroups)
            {
                if (string.IsNullOrEmpty(group.Key)) continue;

                int earliestDisqSem = int.MaxValue;
                var subGroups = group.Where(r => r.SubjectId > 0).GroupBy(r => r.SubjectId);

                foreach (var sg in subGroups)
                {
                    var attempts = sg.OrderBy(r => r.SemesterSequence > 0 ? r.SemesterSequence : r.SemesterId).ThenBy(r => r.ResultId).ToList();
                    
                    var disqRecord = attempts.FirstOrDefault(r => r.IsDisqualified);
                    if (disqRecord != null)
                    {
                        int sem = (disqRecord.SemesterSequence ?? 0) > 0 ? (disqRecord.SemesterSequence ?? 0) : disqRecord.SemesterId;
                        if (sem > 0 && sem < earliestDisqSem) earliestDisqSem = sem;
                    }

                    int failCount = 0;
                    for (int i = 0; i < attempts.Count; i++)
                    {
                        var att = attempts[i];
                        bool isPass = att.IsPass || (att.ReexamIsPass == true) || (att.Grade != "F" && att.Grade != "D" && !string.IsNullOrWhiteSpace(att.Grade));
                        if (!isPass)
                        {
                            failCount++;
                            if (failCount >= 3)
                            {
                                int sem = (att.SemesterSequence ?? 0) > 0 ? (att.SemesterSequence ?? 0) : att.SemesterId;
                                if (sem > 0 && sem < earliestDisqSem) earliestDisqSem = sem;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                if (earliestDisqSem != int.MaxValue)
                {
                    map[group.Key] = earliestDisqSem;
                }
            }

            return map;
        }

        public class StudentPersonalInfoNameDto
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public string? roll_no { get; set; }
            public string? student_name_mm { get; set; }
            public string? student_name_en { get; set; }
        }

        private async Task<(Dictionary<int, StudentPersonalInfoNameDto> byUserId, Dictionary<string, StudentPersonalInfoNameDto> byRollNo)> GetStudentPersonalInfoMapAsync()
        {
            var list = await _context.StudentPersonalInfos
                .AsNoTracking()
                .Select(p => new StudentPersonalInfoNameDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    roll_no = p.roll_no,
                    student_name_mm = p.student_name_mm,
                    student_name_en = p.student_name_en
                })
                .ToListAsync();

            var byUserId = list
                .Where(p => p.UserId > 0)
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Id).First());

            var byRollNo = list
                .Where(p => !string.IsNullOrWhiteSpace(p.roll_no))
                .GroupBy(p => p.roll_no!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.Id).First(), StringComparer.OrdinalIgnoreCase);

            return (byUserId, byRollNo);
        }

        /// <summary>
        /// 1. Subject-wise Student Ranking with Pagination (ဘာသာရပ်အလိုက် အဆင့်သတ်မှတ်ချက်)
        /// Filter by Faculty, Major, Subject Code, Semester, Academic Year with Top-N limit and Pagination.
        /// </summary>
        [HttpGet("subject-ranking")]
        public async Task<IActionResult> GetSubjectRankings(
            [FromQuery] string? facultyName = null,
            [FromQuery] string? majorName = null,
            [FromQuery] string? subjectCode = null,
            [FromQuery] int? subjectId = null,
            [FromQuery] string? semesterName = null,
            [FromQuery] int? semesterId = null,
            [FromQuery] string? academicYear = null,
            [FromQuery] string eligibilityFilter = "EligibleOnly",
            [FromQuery] int topN = 0,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var majorToFaculty = await GetMajorToFacultyMapAsync();
            var disqMap = await GetStudentDisqualificationMapAsync();
            var (pInfoByUserId, pInfoByRoll) = await GetStudentPersonalInfoMapAsync();

            var baseQuery = _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.Subject != null && (r.Subject.IsDelete == false || r.Subject.IsDelete == null));

            if (subjectId.HasValue && subjectId.Value > 0)
            {
                baseQuery = baseQuery.Where(r => r.SubjectId == subjectId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(subjectCode) && !subjectCode.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var cleanCode = subjectCode.Trim();
                baseQuery = baseQuery.Where(r => r.Subject!.SubjectCode == cleanCode);
            }

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                baseQuery = baseQuery.Where(r => r.SemesterId == semesterId.Value || (r.Subject != null && r.Subject.SemesterId == semesterId.Value));
            }
            else if (!string.IsNullOrWhiteSpace(semesterName) && !semesterName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var cleanSem = semesterName.Trim();
                baseQuery = baseQuery.Where(r => 
                    (r.Semester != null && r.Semester.SemesterName == cleanSem) ||
                    (r.Subject != null && r.Subject.Semester != null && r.Subject.Semester.SemesterName == cleanSem));
            }

            if (!string.IsNullOrWhiteSpace(academicYear) && !academicYear.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(r => 
                    (r.Registration != null && r.Registration.AcademicYearRange == academicYear) ||
                    (r.Student != null && r.Student.CurrentClassYear == academicYear));
            }

            var flatList = await baseQuery.Select(r => new
            {
                r.ResultId,
                UserId = (r.Student != null ? r.Student.UserId : (r.Registration != null ? r.Registration.UserId : 0)) ?? 0,
                StudentId = r.StudentId ?? (r.Registration != null ? r.Registration.UserId ?? 0 : (r.Enrollment != null ? r.Enrollment.StudentId : 0)),
                r.RegistrationId,
                RollNo = (r.Registration != null && !string.IsNullOrEmpty(r.Registration.RollNo) ? r.Registration.RollNo : null)
                      ?? (r.Student != null && !string.IsNullOrEmpty(r.Student.CurrentRollNo) ? r.Student.CurrentRollNo : null)
                      ?? (r.Student != null && r.Student.User != null && !string.IsNullOrEmpty(r.Student.User.RoleNo) ? r.Student.User.RoleNo : null)
                      ?? (r.Enrollment != null && r.Enrollment.Student != null && !string.IsNullOrEmpty(r.Enrollment.Student.CurrentRollNo) ? r.Enrollment.Student.CurrentRollNo : null)
                      ?? (r.Enrollment != null && r.Enrollment.Student != null && r.Enrollment.Student.User != null && !string.IsNullOrEmpty(r.Enrollment.Student.User.RoleNo) ? r.Enrollment.Student.User.RoleNo : null)
                      ?? "",
                StudentNameEn = (r.Registration != null ? r.Registration.StudentNameEn : "") ?? "",
                StudentNameMm = (r.Registration != null ? r.Registration.StudentNameMm : "") ?? "",
                StudentName = (r.Student != null && !string.IsNullOrEmpty(r.Student.StudentName) ? r.Student.StudentName : null)
                           ?? (r.Student != null && r.Student.User != null && !string.IsNullOrEmpty(r.Student.User.FullName) ? r.Student.User.FullName : null)
                           ?? (r.Enrollment != null && r.Enrollment.Student != null && !string.IsNullOrEmpty(r.Enrollment.Student.StudentName) ? r.Enrollment.Student.StudentName : null)
                           ?? (r.Enrollment != null && r.Enrollment.Student != null && r.Enrollment.Student.User != null && !string.IsNullOrEmpty(r.Enrollment.Student.User.FullName) ? r.Enrollment.Student.User.FullName : null)
                           ?? "",
                AcademicYear = (r.Registration != null && !string.IsNullOrEmpty(r.Registration.AcademicYearRange) ? r.Registration.AcademicYearRange : null)
                            ?? (r.Student != null && !string.IsNullOrEmpty(r.Student.CurrentClassYear) ? r.Student.CurrentClassYear : null)
                            ?? "",
                RegMajor = r.Registration != null ? (r.Registration.Major ?? "") : "",
                StuMajor = r.Student != null ? (r.Student.CurrentMajor ?? "") : "",
                SubMajor = r.Subject != null && r.Subject.Major != null ? (r.Subject.Major.MajorName ?? "") : "",
                SubFaculty = r.Subject != null && r.Subject.Faculty != null 
                    ? r.Subject.Faculty.FacultyName 
                    : (r.Subject != null && r.Subject.Major != null && r.Subject.Major.Faculty != null ? r.Subject.Major.Faculty.FacultyName : ""),
                SemesterId = r.SemesterId ?? (r.Subject != null ? r.Subject.SemesterId : 0),
                SemesterName = r.Semester != null ? r.Semester.SemesterName : (r.Subject != null && r.Subject.Semester != null ? r.Subject.Semester.SemesterName : ""),
                SemesterSequence = r.Semester != null ? r.Semester.Sequence : (r.Subject != null && r.Subject.Semester != null ? r.Subject.Semester.Sequence : 0),
                SubjectId = r.SubjectId ?? 0,
                SubjectCode = r.Subject != null ? (r.Subject.SubjectCode ?? "") : "",
                SubjectName = r.Subject != null ? (r.Subject.SubjectName ?? "") : "",
                CreditUnit = r.Subject != null ? r.Subject.Credit : 3,
                r.MarksObtained,
                Grade = r.Grade ?? "",
                r.IsPass,
                r.IsDisqualified,
                ReexamGrade = r.ReexamGrade ?? "",
                ReexamIsPass = r.ReexamIsPass,
                LastUpdated = r.ModifiedDateTime ?? r.CreatedDateTime ?? DateTime.MinValue
            }).ToListAsync();

            var filtered = flatList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(facultyName) && !facultyName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(r =>
                {
                    if (!string.IsNullOrEmpty(r.SubFaculty) && r.SubFaculty.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.RegMajor) && majorToFaculty.TryGetValue(r.RegMajor, out var f1) && f1.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.StuMajor) && majorToFaculty.TryGetValue(r.StuMajor, out var f2) && f2.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.SubMajor) && majorToFaculty.TryGetValue(r.SubMajor, out var f3) && f3.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (facultyName.Contains("Computing", StringComparison.OrdinalIgnoreCase) && 
                        (r.SubjectCode.StartsWith("CST-", StringComparison.OrdinalIgnoreCase) || r.SubjectCode.StartsWith("CS-", StringComparison.OrdinalIgnoreCase) || r.SubjectCode.StartsWith("CT-", StringComparison.OrdinalIgnoreCase)))
                        return true;
                    return false;
                });
            }

            if (!string.IsNullOrWhiteSpace(majorName) && !majorName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(r =>
                    r.SubMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase) ||
                    r.RegMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase) ||
                    r.StuMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(r.SubMajor));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                filtered = filtered.Where(r =>
                {
                    StudentPersonalInfoNameDto? pi = null;
                    if (r.UserId > 0 && pInfoByUserId.TryGetValue(r.UserId, out var pi1)) pi = pi1;
                    else if (!string.IsNullOrWhiteSpace(r.RollNo) && pInfoByRoll.TryGetValue(r.RollNo.Trim(), out var pi2)) pi = pi2;

                    return (!string.IsNullOrEmpty(r.RollNo) && r.RollNo.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (pi != null && !string.IsNullOrEmpty(pi.student_name_mm) && pi.student_name_mm.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (pi != null && !string.IsNullOrEmpty(pi.student_name_en) && pi.student_name_en.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentName) && r.StudentName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentNameMm) && r.StudentNameMm.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentNameEn) && r.StudentNameEn.Contains(term, StringComparison.OrdinalIgnoreCase));
                });
            }

            // Group by student and subject to ensure 1 unique entry per student per subject (taking the latest updated score)
            var distinctSubjectList = filtered
                .GroupBy(r => new
                {
                    StudentKey = !string.IsNullOrWhiteSpace(r.RollNo) && r.RollNo != "-" 
                        ? r.RollNo.Trim().ToUpperInvariant() 
                        : (r.StudentId > 0 ? $"STU_{r.StudentId}" : $"REG_{r.RegistrationId}"),
                    SubjectKey = r.SubjectId > 0 ? r.SubjectId.ToString() : r.SubjectCode
                })
                .Select(g => g
                    .OrderByDescending(r => r.LastUpdated)
                    .ThenByDescending(r => r.ResultId)
                    .First()
                );

            var rankList = distinctSubjectList.Select(r =>
            {
                StudentPersonalInfoNameDto? pInfo = null;
                if (r.UserId > 0 && pInfoByUserId.TryGetValue(r.UserId, out var pi1)) pInfo = pi1;
                else if (!string.IsNullOrWhiteSpace(r.RollNo) && pInfoByRoll.TryGetValue(r.RollNo.Trim(), out var pi2)) pInfo = pi2;

                var sName = pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_mm)
                    ? pInfo.student_name_mm.Trim()
                    : (pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_en)
                        ? pInfo.student_name_en.Trim()
                        : (!string.IsNullOrEmpty(r.StudentName) 
                            ? r.StudentName 
                            : (!string.IsNullOrEmpty(r.StudentNameMm) 
                                ? r.StudentNameMm 
                                : r.StudentNameEn)));

                var major = !string.IsNullOrEmpty(r.SubMajor) ? r.SubMajor : (!string.IsNullOrEmpty(r.RegMajor) ? r.RegMajor : r.StuMajor);
                
                string faculty = r.SubFaculty;
                if (string.IsNullOrEmpty(faculty) && !string.IsNullOrEmpty(major) && majorToFaculty.TryGetValue(major, out var f))
                {
                    faculty = f;
                }
                if (string.IsNullOrEmpty(faculty)) faculty = "Faculty of Computing";

                string grade = r.Grade;
                decimal gradePoint = 0m;
                string status = "";

                if (!string.IsNullOrEmpty(grade))
                {
                    gradePoint = GradeCalculator.GetGradePoint(grade);
                    status = GradeCalculator.GetGradeStatus(grade);
                }
                else if (r.MarksObtained.HasValue)
                {
                    var info = GradeCalculator.GetGradeInfoFromMarks(r.MarksObtained.Value);
                    grade = info.LetterGrade;
                    gradePoint = info.GradePoint;
                    status = info.Status;
                }

                decimal gpEarned = GradeCalculator.CalculateGradePointsEarned(gradePoint, r.CreditUnit);
                bool isPass = r.IsPass || (grade != "D" && grade != "F" && !string.IsNullOrEmpty(grade));

                var sKey = !string.IsNullOrWhiteSpace(r.RollNo) && r.RollNo != "-" 
                    ? $"ROLL_{r.RollNo.Trim().ToUpperInvariant()}" 
                    : (r.StudentId > 0 ? $"STU_{r.StudentId}" : "");

                int currentSem = (r.SemesterSequence ?? 0) > 0 ? (r.SemesterSequence ?? 0) : r.SemesterId;
                bool isDegreeEligible = true;
                if (!string.IsNullOrEmpty(sKey) && disqMap.TryGetValue(sKey, out int disqSem))
                {
                    if (disqSem == 0 || currentSem > disqSem)
                    {
                        isDegreeEligible = false;
                    }
                }

                return new StudentSubjectRankItemModel
                {
                    StudentId = r.StudentId,
                    RegistrationId = r.RegistrationId,
                    RollNo = r.RollNo,
                    StudentName = sName,
                    FacultyName = faculty,
                    MajorName = major,
                    AcademicYear = r.AcademicYear,
                    SemesterId = r.SemesterId,
                    SemesterName = !string.IsNullOrEmpty(r.SemesterName) ? r.SemesterName : $"Semester {r.SemesterId}",
                    SubjectId = r.SubjectId,
                    SubjectCode = r.SubjectCode,
                    SubjectName = r.SubjectName,
                    CreditUnit = r.CreditUnit,
                    MarksObtained = r.MarksObtained,
                    Grade = grade,
                    GradePoint = gradePoint,
                    GradePointEarned = gpEarned,
                    IsPass = isPass,
                    Status = status,
                    ReexamGrade = r.ReexamGrade,
                    ReexamIsPass = r.ReexamIsPass,
                    IsDegreeEligible = isDegreeEligible,
                    EligibilityStatus = isDegreeEligible ? "Eligible" : "Disqualified"
                };
            })
            .OrderByDescending(x => x.MarksObtained ?? 0)
            .ThenByDescending(x => x.GradePoint)
            .ToList();

            if (!string.IsNullOrWhiteSpace(eligibilityFilter) && (eligibilityFilter.Equals("EligibleOnly", StringComparison.OrdinalIgnoreCase) || eligibilityFilter.Equals("Eligible", StringComparison.OrdinalIgnoreCase)))
            {
                rankList = rankList.Where(x => x.IsDegreeEligible).ToList();
            }

            for (int i = 0; i < rankList.Count; i++)
            {
                rankList[i].Rank = i + 1;
            }

            if (topN > 0 && topN < rankList.Count)
            {
                rankList = rankList.Take(topN).ToList();
            }

            int totalCount = rankList.Count;
            int currentPage = pageNumber > 0 ? pageNumber : 1;
            int currentSize = pageSize > 0 ? pageSize : (totalCount > 0 ? totalCount : 10);

            var pagedItems = rankList
                .Skip((currentPage - 1) * currentSize)
                .Take(currentSize)
                .ToList();

            return Ok(new PagedResultDto<StudentSubjectRankItemModel>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageNumber = currentPage,
                PageSize = currentSize
            });
        }

        /// <summary>
        /// 2. Semester Total Marks & GPA Ranking with Pagination
        /// </summary>
        [HttpGet("semester-ranking")]
        public async Task<IActionResult> GetSemesterRankings(
            [FromQuery] string? facultyName = null,
            [FromQuery] string? majorName = null,
            [FromQuery] string? semesterName = null,
            [FromQuery] int? semesterId = null,
            [FromQuery] string? academicYear = null,
            [FromQuery] string eligibilityFilter = "EligibleOnly",
            [FromQuery] int topN = 0,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var majorToFaculty = await GetMajorToFacultyMapAsync();
            var disqMap = await GetStudentDisqualificationMapAsync();
            var (pInfoByUserId, pInfoByRoll) = await GetStudentPersonalInfoMapAsync();

            var baseQuery = _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.Subject != null && (r.Subject.IsDelete == false || r.Subject.IsDelete == null));

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                baseQuery = baseQuery.Where(r => r.SemesterId == semesterId.Value || (r.Subject != null && r.Subject.SemesterId == semesterId.Value));
            }
            else if (!string.IsNullOrWhiteSpace(semesterName) && !semesterName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var cleanSem = semesterName.Trim();
                baseQuery = baseQuery.Where(r => 
                    (r.Semester != null && r.Semester.SemesterName == cleanSem) ||
                    (r.Subject != null && r.Subject.Semester != null && r.Subject.Semester.SemesterName == cleanSem));
            }

            if (!string.IsNullOrWhiteSpace(academicYear) && !academicYear.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(r => 
                    (r.Registration != null && r.Registration.AcademicYearRange == academicYear) ||
                    (r.Student != null && r.Student.CurrentClassYear == academicYear));
            }

            var flatList = await baseQuery.Select(r => new
            {
                r.ResultId,
                UserId = (r.Student != null ? r.Student.UserId : (r.Registration != null ? r.Registration.UserId : 0)) ?? 0,
                StudentId = r.StudentId ?? (r.Registration != null ? r.Registration.UserId ?? 0 : (r.Enrollment != null ? r.Enrollment.StudentId : 0)),
                r.RegistrationId,
                RollNo = (r.Registration != null && !string.IsNullOrEmpty(r.Registration.RollNo) ? r.Registration.RollNo : null)
                      ?? (r.Student != null && !string.IsNullOrEmpty(r.Student.CurrentRollNo) ? r.Student.CurrentRollNo : null)
                      ?? (r.Student != null && r.Student.User != null && !string.IsNullOrEmpty(r.Student.User.RoleNo) ? r.Student.User.RoleNo : null)
                      ?? (r.Enrollment != null && r.Enrollment.Student != null && !string.IsNullOrEmpty(r.Enrollment.Student.CurrentRollNo) ? r.Enrollment.Student.CurrentRollNo : null)
                      ?? (r.Enrollment != null && r.Enrollment.Student != null && r.Enrollment.Student.User != null && !string.IsNullOrEmpty(r.Enrollment.Student.User.RoleNo) ? r.Enrollment.Student.User.RoleNo : null)
                      ?? "",
                StudentNameEn = (r.Registration != null ? r.Registration.StudentNameEn : "") ?? "",
                StudentNameMm = (r.Registration != null ? r.Registration.StudentNameMm : "") ?? "",
                StudentName = (r.Student != null && !string.IsNullOrEmpty(r.Student.StudentName) ? r.Student.StudentName : null)
                           ?? (r.Student != null && r.Student.User != null && !string.IsNullOrEmpty(r.Student.User.FullName) ? r.Student.User.FullName : null)
                           ?? (r.Enrollment != null && r.Enrollment.Student != null && !string.IsNullOrEmpty(r.Enrollment.Student.StudentName) ? r.Enrollment.Student.StudentName : null)
                           ?? (r.Enrollment != null && r.Enrollment.Student != null && r.Enrollment.Student.User != null && !string.IsNullOrEmpty(r.Enrollment.Student.User.FullName) ? r.Enrollment.Student.User.FullName : null)
                           ?? "",
                AcademicYear = (r.Registration != null && !string.IsNullOrEmpty(r.Registration.AcademicYearRange) ? r.Registration.AcademicYearRange : null)
                            ?? (r.Student != null && !string.IsNullOrEmpty(r.Student.CurrentClassYear) ? r.Student.CurrentClassYear : null)
                            ?? "",
                RegMajor = r.Registration != null ? (r.Registration.Major ?? "") : "",
                StuMajor = r.Student != null ? (r.Student.CurrentMajor ?? "") : "",
                SubMajor = r.Subject != null && r.Subject.Major != null ? (r.Subject.Major.MajorName ?? "") : "",
                SubFaculty = r.Subject != null && r.Subject.Faculty != null 
                    ? r.Subject.Faculty.FacultyName 
                    : (r.Subject != null && r.Subject.Major != null && r.Subject.Major.Faculty != null ? r.Subject.Major.Faculty.FacultyName : ""),
                SemesterId = (r.Subject != null && r.Subject.SemesterId > 0) ? r.Subject.SemesterId : (r.SemesterId ?? 0),
                SemesterName = (r.Subject != null && r.Subject.Semester != null) ? r.Subject.Semester.SemesterName : (r.Semester != null ? r.Semester.SemesterName : ""),
                SemesterSequence = (r.Subject != null && r.Subject.Semester != null) ? r.Subject.Semester.Sequence : (r.Semester != null ? r.Semester.Sequence : 0),
                SubjectId = r.SubjectId ?? 0,
                SubjectCode = r.Subject != null ? (r.Subject.SubjectCode ?? "") : "",
                SubjectName = r.Subject != null ? (r.Subject.SubjectName ?? "") : "",
                CreditUnit = r.Subject != null ? r.Subject.Credit : 3,
                r.MarksObtained,
                Grade = r.Grade ?? "",
                r.IsPass,
                r.IsDisqualified,
                ReexamGrade = r.ReexamGrade ?? "",
                ReexamIsPass = r.ReexamIsPass,
                LastUpdated = r.ModifiedDateTime ?? r.CreatedDateTime ?? DateTime.MinValue
            }).ToListAsync();

            var filtered = flatList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(facultyName) && !facultyName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(r =>
                {
                    if (!string.IsNullOrEmpty(r.SubFaculty) && r.SubFaculty.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.RegMajor) && majorToFaculty.TryGetValue(r.RegMajor, out var f1) && f1.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.StuMajor) && majorToFaculty.TryGetValue(r.StuMajor, out var f2) && f2.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.SubMajor) && majorToFaculty.TryGetValue(r.SubMajor, out var f3) && f3.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (facultyName.Contains("Computing", StringComparison.OrdinalIgnoreCase) && 
                        (r.SubjectCode.StartsWith("CST-", StringComparison.OrdinalIgnoreCase) || r.SubjectCode.StartsWith("CS-", StringComparison.OrdinalIgnoreCase) || r.SubjectCode.StartsWith("CT-", StringComparison.OrdinalIgnoreCase)))
                        return true;
                    return false;
                });
            }

            if (!string.IsNullOrWhiteSpace(majorName) && !majorName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(r =>
                    r.SubMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase) ||
                    r.RegMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase) ||
                    r.StuMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                filtered = filtered.Where(r =>
                {
                    StudentPersonalInfoNameDto? pi = null;
                    if (r.UserId > 0 && pInfoByUserId.TryGetValue(r.UserId, out var pi1)) pi = pi1;
                    else if (!string.IsNullOrWhiteSpace(r.RollNo) && pInfoByRoll.TryGetValue(r.RollNo.Trim(), out var pi2)) pi = pi2;

                    return (!string.IsNullOrEmpty(r.RollNo) && r.RollNo.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (pi != null && !string.IsNullOrEmpty(pi.student_name_mm) && pi.student_name_mm.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (pi != null && !string.IsNullOrEmpty(pi.student_name_en) && pi.student_name_en.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentName) && r.StudentName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentNameMm) && r.StudentNameMm.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentNameEn) && r.StudentNameEn.Contains(term, StringComparison.OrdinalIgnoreCase));
                });
            }

            var grouped = filtered.GroupBy(r => new
            {
                StudentKey = !string.IsNullOrWhiteSpace(r.RollNo) && r.RollNo != "-" 
                    ? r.RollNo.Trim().ToUpperInvariant() 
                    : (r.StudentId > 0 ? $"STU_{r.StudentId}" : $"REG_{r.RegistrationId}"),
                r.SemesterId
            });

            var rankedList = new List<StudentSemesterRankItemModel>();

            foreach (var group in grouped)
            {
                // Ensure distinct subjects per student within this semester (taking latest modified score)
                var distinctRecords = group
                    .GroupBy(r => r.SubjectId > 0 ? r.SubjectId.ToString() : r.SubjectCode)
                    .Select(sg => sg
                        .OrderByDescending(r => r.LastUpdated)
                        .ThenByDescending(r => r.ResultId)
                        .First()
                    )
                    .ToList();

                var records = distinctRecords;
                var first = records.First();

                var roll = records.Select(r => r.RollNo).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.RollNo;
                var userId = records.Select(r => r.UserId).FirstOrDefault(u => u > 0);

                StudentPersonalInfoNameDto? pInfo = null;
                if (userId > 0 && pInfoByUserId.TryGetValue(userId, out var pi1)) pInfo = pi1;
                else if (!string.IsNullOrWhiteSpace(roll) && pInfoByRoll.TryGetValue(roll.Trim(), out var pi2)) pInfo = pi2;

                var sName = pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_mm)
                    ? pInfo.student_name_mm.Trim()
                    : (pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_en)
                        ? pInfo.student_name_en.Trim()
                        : (records.Select(r => !string.IsNullOrEmpty(r.StudentName) ? r.StudentName : (!string.IsNullOrEmpty(r.StudentNameMm) ? r.StudentNameMm : r.StudentNameEn)).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.StudentName));
                var major = records.Select(r => !string.IsNullOrEmpty(r.SubMajor) ? r.SubMajor : (!string.IsNullOrEmpty(r.RegMajor) ? r.RegMajor : r.StuMajor)).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.RegMajor;
                var acadYear = records.Select(r => r.AcademicYear).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.AcademicYear;
                
                string faculty = first.SubFaculty;
                if (string.IsNullOrEmpty(faculty) && !string.IsNullOrEmpty(major) && majorToFaculty.TryGetValue(major, out var f))
                {
                    faculty = f;
                }
                if (string.IsNullOrEmpty(faculty)) faculty = "Faculty of Computing";

                decimal totalMarks = 0m;
                decimal totalGradePointsEarned = 0m;
                int totalCredits = 0;
                int failedSubjectsCount = 0;
                var subjectDetails = new List<SubjectGradeDetailDto>();

                foreach (var rec in records)
                {
                    decimal marks = rec.MarksObtained ?? 0m;
                    totalMarks += marks;

                    string grade = rec.Grade;
                    decimal gp = 0m;
                    string status = "";

                    if (!string.IsNullOrEmpty(grade))
                    {
                        gp = GradeCalculator.GetGradePoint(grade);
                        status = GradeCalculator.GetGradeStatus(grade);
                    }
                    else if (rec.MarksObtained.HasValue)
                    {
                        var gInfo = GradeCalculator.GetGradeInfoFromMarks(rec.MarksObtained.Value);
                        grade = gInfo.LetterGrade;
                        gp = gInfo.GradePoint;
                        status = gInfo.Status;
                    }

                    int cred = rec.CreditUnit > 0 ? rec.CreditUnit : 3;
                    totalCredits += cred;
                    decimal gpEarned = GradeCalculator.CalculateGradePointsEarned(gp, cred);
                    totalGradePointsEarned += gpEarned;

                    bool isPass = rec.IsPass || (grade != "D" && grade != "F" && !string.IsNullOrEmpty(grade));
                    if (!isPass)
                    {
                        failedSubjectsCount++;
                    }

                    subjectDetails.Add(new SubjectGradeDetailDto
                    {
                        SubjectId = rec.SubjectId,
                        SubjectCode = rec.SubjectCode,
                        SubjectName = rec.SubjectName,
                        CreditUnit = cred,
                        MarksObtained = rec.MarksObtained,
                        Grade = grade,
                        GradePoint = gp,
                        GradePointEarned = gpEarned,
                        IsPass = isPass,
                        Status = status,
                        ReexamGrade = rec.ReexamGrade
                    });
                }

                decimal gpa = totalCredits > 0 ? Math.Round(totalGradePointsEarned / totalCredits, 2) : 0m;
                decimal avgMarks = records.Count > 0 ? Math.Round(totalMarks / records.Count, 2) : 0m;

                var sKey = !string.IsNullOrWhiteSpace(roll) && roll != "-" 
                    ? $"ROLL_{roll.Trim().ToUpperInvariant()}" 
                    : (first.StudentId > 0 ? $"STU_{first.StudentId}" : "");

                int currentSem = (first.SemesterSequence ?? 0) > 0 ? (first.SemesterSequence ?? 0) : first.SemesterId;
                bool isDegreeEligible = true;
                if (!string.IsNullOrEmpty(sKey) && disqMap.TryGetValue(sKey, out int disqSem))
                {
                    if (disqSem == 0 || currentSem > disqSem)
                    {
                        isDegreeEligible = false;
                    }
                }

                rankedList.Add(new StudentSemesterRankItemModel
                {
                    StudentId = first.StudentId,
                    RegistrationId = first.RegistrationId,
                    RollNo = first.RollNo,
                    StudentName = sName,
                    FacultyName = faculty,
                    MajorName = major,
                    AcademicYear = first.AcademicYear,
                    SemesterId = first.SemesterId,
                    SemesterName = !string.IsNullOrEmpty(first.SemesterName) ? first.SemesterName : $"Semester {first.SemesterId}",
                    TotalSubjectsCount = records.Count,
                    TotalCredits = totalCredits,
                    TotalMarks = Math.Round(totalMarks, 2),
                    AverageMarks = avgMarks,
                    TotalGradePointsEarned = totalGradePointsEarned,
                    SemesterGPA = gpa,
                    IsPassAll = failedSubjectsCount == 0 && records.Count > 0,
                    FailedSubjectsCount = failedSubjectsCount,
                    IsDegreeEligible = isDegreeEligible,
                    EligibilityStatus = isDegreeEligible ? "Eligible" : "Disqualified",
                    SubjectDetails = subjectDetails
                });
            }

            if (!string.IsNullOrWhiteSpace(eligibilityFilter) && (eligibilityFilter.Equals("EligibleOnly", StringComparison.OrdinalIgnoreCase) || eligibilityFilter.Equals("Eligible", StringComparison.OrdinalIgnoreCase)))
            {
                rankedList = rankedList.Where(x => x.IsDegreeEligible).ToList();
            }

            rankedList = rankedList
                .OrderByDescending(x => x.TotalMarks)
                .ThenByDescending(x => x.SemesterGPA)
                .ToList();

            for (int i = 0; i < rankedList.Count; i++)
            {
                rankedList[i].Rank = i + 1;
            }

            if (topN > 0 && topN < rankedList.Count)
            {
                rankedList = rankedList.Take(topN).ToList();
            }

            int totalCount = rankedList.Count;
            int currentPage = pageNumber > 0 ? pageNumber : 1;
            int currentSize = pageSize > 0 ? pageSize : (totalCount > 0 ? totalCount : 10);

            var pagedItems = rankedList
                .Skip((currentPage - 1) * currentSize)
                .Take(currentSize)
                .ToList();

            return Ok(new PagedResultDto<StudentSemesterRankItemModel>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageNumber = currentPage,
                PageSize = currentSize
            });
        }

        /// <summary>
        /// 3. Master Degree Eligibility (မဟာဘွဲ့ အရည်အချင်းစစ် - CGPA >= 3.00)
        /// Filter by Faculty, Major, Academic Year, StatusFilter ("All", "Eligible", "NonEligible"), Top-N and Pagination.
        /// </summary>
        [HttpGet("master-eligibility")]
        public async Task<IActionResult> GetMasterEligibility(
            [FromQuery] string? facultyName = null,
            [FromQuery] string? majorName = null,
            [FromQuery] string? academicYear = null,
            [FromQuery] string statusFilter = "All",
            [FromQuery] int topN = 0,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var majorToFaculty = await GetMajorToFacultyMapAsync();
            var (pInfoByUserId, pInfoByRoll) = await GetStudentPersonalInfoMapAsync();

            var baseQuery = _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.Subject != null && (r.Subject.IsDelete == false || r.Subject.IsDelete == null));

            if (!string.IsNullOrWhiteSpace(academicYear) && !academicYear.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = baseQuery.Where(r => 
                    (r.Registration != null && r.Registration.AcademicYearRange == academicYear) ||
                    (r.Student != null && r.Student.CurrentClassYear == academicYear));
            }

            var flatList = await baseQuery.Select(r => new
            {
                r.ResultId,
                UserId = (r.Registration != null ? r.Registration.UserId : (int?)null)
                      ?? (r.Student != null ? r.Student.UserId : (int?)null)
                      ?? (r.Enrollment != null && r.Enrollment.Student != null ? r.Enrollment.Student.UserId : (int?)null),
                StudentId = r.StudentId ?? (r.Registration != null ? r.Registration.UserId ?? 0 : (r.Enrollment != null ? r.Enrollment.StudentId : 0)),
                r.RegistrationId,
                RollNo = (r.Registration != null && !string.IsNullOrEmpty(r.Registration.RollNo) ? r.Registration.RollNo : null)
                      ?? (r.Student != null && !string.IsNullOrEmpty(r.Student.CurrentRollNo) ? r.Student.CurrentRollNo : null)
                      ?? (r.Student != null && r.Student.User != null && !string.IsNullOrEmpty(r.Student.User.RoleNo) ? r.Student.User.RoleNo : null)
                      ?? (r.Enrollment != null && r.Enrollment.Student != null && !string.IsNullOrEmpty(r.Enrollment.Student.CurrentRollNo) ? r.Enrollment.Student.CurrentRollNo : null)
                      ?? (r.Enrollment != null && r.Enrollment.Student != null && r.Enrollment.Student.User != null && !string.IsNullOrEmpty(r.Enrollment.Student.User.RoleNo) ? r.Enrollment.Student.User.RoleNo : null)
                      ?? "",
                StudentNameEn = (r.Registration != null ? r.Registration.StudentNameEn : "") ?? "",
                StudentNameMm = (r.Registration != null ? r.Registration.StudentNameMm : "") ?? "",
                StudentName = (r.Student != null && !string.IsNullOrEmpty(r.Student.StudentName) ? r.Student.StudentName : null)
                           ?? (r.Student != null && r.Student.User != null && !string.IsNullOrEmpty(r.Student.User.FullName) ? r.Student.User.FullName : null)
                           ?? (r.Enrollment != null && r.Enrollment.Student != null && !string.IsNullOrEmpty(r.Enrollment.Student.StudentName) ? r.Enrollment.Student.StudentName : null)
                           ?? (r.Enrollment != null && r.Enrollment.Student != null && r.Enrollment.Student.User != null && !string.IsNullOrEmpty(r.Enrollment.Student.User.FullName) ? r.Enrollment.Student.User.FullName : null)
                           ?? "",
                AcademicYear = (r.Registration != null && !string.IsNullOrEmpty(r.Registration.AcademicYearRange) ? r.Registration.AcademicYearRange : null)
                            ?? (r.Student != null && !string.IsNullOrEmpty(r.Student.CurrentClassYear) ? r.Student.CurrentClassYear : null)
                            ?? "",
                RegMajor = r.Registration != null ? (r.Registration.Major ?? "") : "",
                StuMajor = r.Student != null ? (r.Student.CurrentMajor ?? "") : "",
                SubMajor = r.Subject != null && r.Subject.Major != null ? (r.Subject.Major.MajorName ?? "") : "",
                SubFaculty = r.Subject != null && r.Subject.Faculty != null 
                    ? r.Subject.Faculty.FacultyName 
                    : (r.Subject != null && r.Subject.Major != null && r.Subject.Major.Faculty != null ? r.Subject.Major.Faculty.FacultyName : ""),
                SemesterId = (r.Subject != null && r.Subject.SemesterId > 0) ? r.Subject.SemesterId : (r.SemesterId ?? 0),
                SemesterName = (r.Subject != null && r.Subject.Semester != null) ? r.Subject.Semester.SemesterName : (r.Semester != null ? r.Semester.SemesterName : ""),
                SemesterSequence = (r.Subject != null && r.Subject.Semester != null) ? r.Subject.Semester.Sequence : (r.Semester != null ? r.Semester.Sequence : 0),
                SubjectId = r.SubjectId ?? 0,
                SubjectCode = r.Subject != null ? (r.Subject.SubjectCode ?? "") : "",
                SubjectName = r.Subject != null ? (r.Subject.SubjectName ?? "") : "",
                CreditUnit = r.Subject != null ? r.Subject.Credit : 3,
                r.MarksObtained,
                Grade = r.Grade ?? "",
                r.IsPass,
                r.IsDisqualified,
                ReexamGrade = r.ReexamGrade ?? "",
                r.ReexamIsPass,
                LastUpdated = r.ModifiedDateTime ?? r.CreatedDateTime ?? DateTime.MinValue
            }).ToListAsync();

            var filtered = flatList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(facultyName) && !facultyName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(r =>
                {
                    if (!string.IsNullOrEmpty(r.SubFaculty) && r.SubFaculty.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.RegMajor) && majorToFaculty.TryGetValue(r.RegMajor, out var f1) && f1.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.StuMajor) && majorToFaculty.TryGetValue(r.StuMajor, out var f2) && f2.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (!string.IsNullOrEmpty(r.SubMajor) && majorToFaculty.TryGetValue(r.SubMajor, out var f3) && f3.Equals(facultyName, StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (facultyName.Contains("Computing", StringComparison.OrdinalIgnoreCase) && 
                        (r.SubjectCode.StartsWith("CST-", StringComparison.OrdinalIgnoreCase) || r.SubjectCode.StartsWith("CS-", StringComparison.OrdinalIgnoreCase) || r.SubjectCode.StartsWith("CT-", StringComparison.OrdinalIgnoreCase)))
                        return true;
                    return false;
                });
            }

            if (!string.IsNullOrWhiteSpace(majorName) && !majorName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(r =>
                    r.SubMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase) ||
                    r.RegMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase) ||
                    r.StuMajor.Equals(majorName, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                filtered = filtered.Where(r =>
                {
                    StudentPersonalInfoNameDto? pi = null;
                    if (r.UserId.HasValue && r.UserId.Value > 0 && pInfoByUserId.TryGetValue(r.UserId.Value, out var pi1)) pi = pi1;
                    else if (!string.IsNullOrWhiteSpace(r.RollNo) && pInfoByRoll.TryGetValue(r.RollNo.Trim(), out var pi2)) pi = pi2;

                    return (!string.IsNullOrEmpty(r.RollNo) && r.RollNo.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (pi != null && !string.IsNullOrEmpty(pi.student_name_mm) && pi.student_name_mm.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (pi != null && !string.IsNullOrEmpty(pi.student_name_en) && pi.student_name_en.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentName) && r.StudentName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentNameMm) && r.StudentNameMm.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(r.StudentNameEn) && r.StudentNameEn.Contains(term, StringComparison.OrdinalIgnoreCase));
                });
            }

            // Preload student entities by ID and RollNo for status and semester pass resolution
            var allStudentsInfo = await _context.Students
                .AsNoTracking()
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .Select(s => new
                {
                    s.StudentId,
                    s.UserId,
                    s.CurrentRollNo,
                    s.Status,
                    s.Sem1_Result, s.Sem2_Result, s.Sem3_Result, s.Sem4_Result,
                    s.Sem5_Result, s.Sem6_Result, s.Sem7_Result, s.Sem8_Result, s.Sem9_Result
                })
                .ToListAsync();

            var studentById = allStudentsInfo.ToDictionary(s => s.StudentId);
            var studentByRoll = allStudentsInfo
                .Where(s => !string.IsNullOrWhiteSpace(s.CurrentRollNo))
                .GroupBy(s => s.CurrentRollNo!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var studentGroups = filtered.GroupBy(r => 
                !string.IsNullOrWhiteSpace(r.RollNo) && r.RollNo != "-" 
                    ? $"ROLL_{r.RollNo.Trim().ToUpperInvariant()}" 
                    : (r.StudentId > 0 ? $"STU_{r.StudentId}" : $"REG_{r.RegistrationId}")
            );

            var eligibilityList = new List<StudentMasterEligibilityItemModel>();

            foreach (var studentGroup in studentGroups)
            {
                var records = studentGroup.ToList();
                var first = records.First();

                var roll = records.Select(r => r.RollNo).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.RollNo;
                var userId = records.Select(r => r.UserId ?? 0).FirstOrDefault(u => u > 0);

                StudentPersonalInfoNameDto? pInfo = null;
                if (userId > 0 && pInfoByUserId.TryGetValue(userId, out var pi1)) pInfo = pi1;
                else if (!string.IsNullOrWhiteSpace(roll) && pInfoByRoll.TryGetValue(roll.Trim(), out var pi2)) pInfo = pi2;

                var sName = pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_mm)
                    ? pInfo.student_name_mm.Trim()
                    : (pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_en)
                        ? pInfo.student_name_en.Trim()
                        : (records.Select(r => !string.IsNullOrEmpty(r.StudentName) ? r.StudentName : (!string.IsNullOrEmpty(r.StudentNameMm) ? r.StudentNameMm : r.StudentNameEn)).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.StudentName));
                var major = records.Select(r => !string.IsNullOrEmpty(r.SubMajor) ? r.SubMajor : (!string.IsNullOrEmpty(r.RegMajor) ? r.RegMajor : r.StuMajor)).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.RegMajor;
                var acadYear = records.Select(r => r.AcademicYear).FirstOrDefault(s => !string.IsNullOrEmpty(s)) ?? first.AcademicYear;
                
                string faculty = first.SubFaculty;
                if (string.IsNullOrEmpty(faculty) && !string.IsNullOrEmpty(major) && majorToFaculty.TryGetValue(major, out var f))
                {
                    faculty = f;
                }
                if (string.IsNullOrEmpty(faculty)) faculty = "Faculty of Computing";

                var matchedStudent = (first.StudentId > 0 && studentById.TryGetValue(first.StudentId, out var st1)) ? st1
                    : (!string.IsNullOrWhiteSpace(roll) && studentByRoll.TryGetValue(roll.Trim(), out var st2)) ? st2
                    : null;

                int totalRequiredSemesters = (major != null && (major.Contains("Civil", StringComparison.OrdinalIgnoreCase) ||
                                                                major.Contains("Electronic", StringComparison.OrdinalIgnoreCase) ||
                                                                major.Contains("Electrical", StringComparison.OrdinalIgnoreCase) ||
                                                                major.Contains("Mechanical", StringComparison.OrdinalIgnoreCase) ||
                                                                major.Contains("Engineering", StringComparison.OrdinalIgnoreCase))) ? 9 : 8;

                var subjectGroups = records
                    .Where(r => r.SubjectId > 0)
                    .GroupBy(r => r.SubjectId);

                int sem1To7Credits = 0;
                int sem8Credits = 0;
                int passedSubjectsCount = 0;
                int pendingFailedCount = 0;
                bool hasDisqualifiedSubject = false;

                foreach (var group in subjectGroups)
                {
                    bool isDisq = group.Any(r => r.IsDisqualified);
                    if (isDisq) hasDisqualifiedSubject = true;

                    bool isPassed = group.Any(r => r.IsPass || (r.ReexamIsPass == true) || (r.Grade != "F" && r.Grade != "D" && !string.IsNullOrWhiteSpace(r.Grade)));
                    var sample = group.First();
                    int credit = sample.CreditUnit > 0 ? sample.CreditUnit : 3;

                    if (isPassed)
                    {
                        passedSubjectsCount++;
                        bool isSem8 = sample.SemesterId == 9 || (sample.SemesterSequence == totalRequiredSemesters) || (!string.IsNullOrEmpty(sample.SubjectName) && sample.SubjectName.Contains("internship", StringComparison.OrdinalIgnoreCase));
                        if (isSem8)
                        {
                            sem8Credits += (credit >= 12 ? credit : 12);
                        }
                        else
                        {
                            sem1To7Credits += credit;
                        }
                    }
                    else
                    {
                        pendingFailedCount++;
                    }
                }

                var distinctResultSemesters = records
                    .Where(r => r.SemesterId > 0)
                    .Select(r => r.SemesterId)
                    .Distinct()
                    .ToList();

                int completedSemesters = distinctResultSemesters.Count;
                int totalEarnedCredits = sem1To7Credits + sem8Credits;
                int targetGraduationCredits = 155;

                // Semester-wise GPA Breakdown
                // Step 1: Deduplicate subjects per student, keeping the latest attempt
                var latestSubjectRecords = records
                    .Where(r => r.SubjectId > 0)
                    .GroupBy(r => r.SubjectId)
                    .Select(g => g
                        .OrderByDescending(r => r.LastUpdated)
                        .ThenByDescending(r => r.ResultId)
                        .First()
                    )
                    .ToList();

                // Step 2: Group by Subject's original Curriculum SemesterId
                var semGrouped = latestSubjectRecords
                    .Where(r => r.SemesterId > 0)
                    .GroupBy(r => r.SemesterId)
                    .OrderBy(g => g.Key);

                var semesterHistory = new List<SemesterGpaSummaryDto>();
                decimal sumAllGradePoints = 0.0m;
                int sumAllCredits = 0;

                foreach (var sg in semGrouped)
                {
                    int semId = sg.Key;
                    var firstRecord = sg.First();
                    string semName = !string.IsNullOrEmpty(firstRecord.SemesterName) ? firstRecord.SemesterName : $"Semester {semId}";

                    int semCredits = 0;
                    decimal semPoints = 0.0m;
                    decimal semMarks = 0.0m;
                    int semFailed = 0;

                    foreach (var r in sg)
                    {
                        int cred = r.CreditUnit > 0 ? r.CreditUnit : 3;
                        bool isSem8Sub = r.SemesterId == 9 || (r.SemesterSequence == totalRequiredSemesters) || (!string.IsNullOrEmpty(r.SubjectName) && r.SubjectName.Contains("internship", StringComparison.OrdinalIgnoreCase));
                        if (isSem8Sub && cred < 12) cred = 12;

                        string gLetter = (!string.IsNullOrWhiteSpace(r.ReexamGrade) && (r.ReexamIsPass == true || (r.ReexamGrade != "D" && r.ReexamGrade != "F")))
                            ? r.ReexamGrade
                            : r.Grade;

                        decimal gp = 0.0m;
                        if (!string.IsNullOrWhiteSpace(gLetter))
                        {
                            gp = GradeCalculator.GetGradePoint(gLetter);
                        }
                        else if (r.MarksObtained.HasValue)
                        {
                            var info = GradeCalculator.GetGradeInfoFromMarks(r.MarksObtained.Value);
                            gp = info.GradePoint;
                            if (string.IsNullOrWhiteSpace(gLetter)) gLetter = info.LetterGrade;
                        }

                        decimal earned = GradeCalculator.CalculateGradePointsEarned(gp, cred);
                        semCredits += cred;
                        semPoints += earned;
                        semMarks += (r.MarksObtained ?? 0m);

                        bool isItemPass = r.IsPass || (r.ReexamIsPass == true) || (gLetter != "D" && gLetter != "F" && gLetter != "Fail" && !string.IsNullOrEmpty(gLetter));
                        if (!isItemPass) semFailed++;
                    }

                    decimal semGpa = GradeCalculator.CalculateSemesterGPA(semPoints, semCredits);
                    semesterHistory.Add(new SemesterGpaSummaryDto
                    {
                        SemesterId = semId,
                        SemesterName = semName,
                        Credits = semCredits,
                        TotalMarks = semMarks,
                        SemesterGPA = semGpa,
                        IsPassAll = semFailed == 0
                    });

                    sumAllCredits += semCredits;
                    sumAllGradePoints += semPoints;
                }

                decimal cgpa = semesterHistory.Any()
                    ? GradeCalculator.CalculateCumulativeGPAFromSemesterGpas(semesterHistory.Select(s => s.SemesterGPA))
                    : 0.0m;

                bool isDisqualified = hasDisqualifiedSubject || (matchedStudent != null && string.Equals(matchedStudent.Status, "Disqualified", StringComparison.OrdinalIgnoreCase));
                bool isCurriculumFinished = completedSemesters >= totalRequiredSemesters && totalEarnedCredits >= targetGraduationCredits && pendingFailedCount == 0;
                bool isGraduated = !isDisqualified && isCurriculumFinished && cgpa >= 2.00m;

                bool isMasterEligible = isGraduated && cgpa >= 3.00m;
                bool isBachelorOnly = isGraduated && cgpa >= 2.00m && cgpa < 3.00m;
                bool isCgpaIneligible = !isDisqualified && isCurriculumFinished && cgpa < 2.00m;

                string statusKey;
                string statusText;
                string badgeClass;
                string badgeTextMm;
                string badgeTextEn;

                if (isDisqualified)
                {
                    statusKey = "Disqualified";
                    statusText = "Disqualified (Retake စည်းကမ်းမပြည့်မီသဖြင့် ဘွဲ့မရရှိနိုင်သူ)";
                    badgeClass = "badge-disqualified";
                    badgeTextMm = "⛔ Retake စည်းကမ်းမပြည့်မီ";
                    badgeTextEn = "⛔ Disqualified (Retake)";
                }
                else if (isMasterEligible)
                {
                    statusKey = "MasterEligible";
                    statusText = "Master Degree Eligible (မဟာဘွဲ့ ရရှိခွင့်ရ)";
                    badgeClass = "badge-master-eligible";
                    badgeTextMm = "⭐ မဟာဘွဲ့ ရရှိခွင့်ရ";
                    badgeTextEn = "⭐ Master Eligible";
                }
                else if (isBachelorOnly)
                {
                    statusKey = "BachelorOnly";
                    statusText = "Bachelor Degree Only (ဘွဲ့ကြို ရိုးရိုးဘွဲ့ ရရှိသူ)";
                    badgeClass = "badge-bachelor-only";
                    badgeTextMm = "🎓 ဘွဲ့ကြိုသာ ရရှိသူ";
                    badgeTextEn = "🎓 Bachelor Only";
                }
                else if (isCgpaIneligible)
                {
                    statusKey = "CgpaIneligible";
                    statusText = "Ineligible for Degree (CGPA < 2.00 မပြည့်မီပါ)";
                    badgeClass = "badge-cgpa-ineligible";
                    badgeTextMm = "❌ CGPA မပြည့်မီ (< 2.00)";
                    badgeTextEn = "❌ Ineligible (CGPA < 2.00)";
                }
                else
                {
                    statusKey = "Studying";
                    statusText = "Studying / In Progress (ပညာသင်ယူဆဲ)";
                    badgeClass = "badge-studying";
                    badgeTextMm = "⏳ ပညာသင်ယူဆဲ";
                    badgeTextEn = "⏳ In Progress";
                }

                eligibilityList.Add(new StudentMasterEligibilityItemModel
                {
                    StudentId = matchedStudent?.StudentId ?? (first.StudentId > 0 ? first.StudentId : 0),
                    RegistrationId = first.RegistrationId,
                    RollNo = roll,
                    StudentName = sName,
                    FacultyName = faculty,
                    MajorName = major,
                    AcademicYear = acadYear,
                    CompletedSemestersCount = completedSemesters,
                    TotalCompletedCredits = totalEarnedCredits,
                    TotalCumulativeMarks = semesterHistory.Sum(sh => sh.TotalMarks),
                    CumulativeGPA = cgpa,
                    IsMasterEligible = isMasterEligible,
                    IsGraduated = isGraduated,
                    IsDisqualified = isDisqualified,
                    MasterEligibilityStatus = statusKey,
                    StatusBadgeClass = badgeClass,
                    StatusBadgeTextMm = badgeTextMm,
                    StatusBadgeTextEn = badgeTextEn,
                    SemesterHistory = semesterHistory
                });
            }

            if (statusFilter.Equals("Eligible", StringComparison.OrdinalIgnoreCase) || statusFilter.Equals("Master", StringComparison.OrdinalIgnoreCase) || statusFilter.Equals("MasterEligible", StringComparison.OrdinalIgnoreCase))
            {
                eligibilityList = eligibilityList.Where(x => x.MasterEligibilityStatus == "MasterEligible").ToList();
            }
            else if (statusFilter.Equals("BachelorOnly", StringComparison.OrdinalIgnoreCase) || statusFilter.Equals("Bachelor", StringComparison.OrdinalIgnoreCase))
            {
                eligibilityList = eligibilityList.Where(x => x.MasterEligibilityStatus == "BachelorOnly").ToList();
            }
            else if (statusFilter.Equals("Studying", StringComparison.OrdinalIgnoreCase) || statusFilter.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
            {
                eligibilityList = eligibilityList.Where(x => x.MasterEligibilityStatus == "Studying").ToList();
            }
            else if (statusFilter.Equals("CgpaIneligible", StringComparison.OrdinalIgnoreCase))
            {
                eligibilityList = eligibilityList.Where(x => x.MasterEligibilityStatus == "CgpaIneligible").ToList();
            }
            else if (statusFilter.Equals("Disqualified", StringComparison.OrdinalIgnoreCase))
            {
                eligibilityList = eligibilityList.Where(x => x.MasterEligibilityStatus == "Disqualified").ToList();
            }
            else if (statusFilter.Equals("NonEligible", StringComparison.OrdinalIgnoreCase))
            {
                eligibilityList = eligibilityList.Where(x => x.MasterEligibilityStatus != "MasterEligible").ToList();
            }

            eligibilityList = eligibilityList
                .OrderByDescending(x => x.CumulativeGPA)
                .ThenByDescending(x => x.TotalCumulativeMarks)
                .ToList();

            for (int i = 0; i < eligibilityList.Count; i++)
            {
                eligibilityList[i].Rank = i + 1;
            }

            if (topN > 0 && topN < eligibilityList.Count)
            {
                eligibilityList = eligibilityList.Take(topN).ToList();
            }

            int totalCount = eligibilityList.Count;
            int currentPage = pageNumber > 0 ? pageNumber : 1;
            int currentSize = pageSize > 0 ? pageSize : (totalCount > 0 ? totalCount : 10);

            var pagedItems = eligibilityList
                .Skip((currentPage - 1) * currentSize)
                .Take(currentSize)
                .ToList();

            return Ok(new PagedResultDto<StudentMasterEligibilityItemModel>
            {
                Items = pagedItems,
                TotalCount = totalCount,
                PageNumber = currentPage,
                PageSize = currentSize
            });
        }

        /// <summary>
        /// 4. Executive Summary KPI Stats for Dashboard Header
        /// </summary>
        [HttpGet("summary-stats")]
        public async Task<IActionResult> GetSummaryStats(
            [FromQuery] string? facultyName = null,
            [FromQuery] string? majorName = null,
            [FromQuery] string? academicYear = null)
        {
            var eligibilityResult = await GetMasterEligibility(facultyName, majorName, academicYear, "All", 0, null, 1, 100000);
            if (eligibilityResult is OkObjectResult okResult && okResult.Value is PagedResultDto<StudentMasterEligibilityItemModel> paged)
            {
                var list = paged.Items;
                int total = paged.TotalCount;
                int masterEligible = list.Count(x => x.IsMasterEligible);
                int nonEligible = total - masterEligible;
                decimal percentage = total > 0 ? Math.Round((decimal)masterEligible / total * 100m, 1) : 0m;
                decimal highestCgpa = list.Any() ? list.Max(x => x.CumulativeGPA) : 0m;
                decimal avgCgpa = list.Any() ? Math.Round(list.Average(x => x.CumulativeGPA), 2) : 0m;
                var topStudent = list.FirstOrDefault();

                int distinctionCount = list.Count(x => x.CumulativeGPA >= 3.67m);

                return Ok(new StudentRankingSummaryStatsDto
                {
                    TotalStudentsEvaluated = total,
                    MasterEligibleCount = masterEligible,
                    MasterEligiblePercentage = percentage,
                    NonEligibleCount = nonEligible,
                    HighestCGPA = highestCgpa,
                    TopStudentName = topStudent?.StudentName,
                    TopStudentRollNo = topStudent?.RollNo,
                    FacultyAverageCGPA = avgCgpa,
                    TotalDistinctionStudents = distinctionCount
                });
            }

            return Ok(new StudentRankingSummaryStatsDto());
        }

        /// <summary>
        /// 5. Filter Dropdown Options
        /// </summary>
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var faculties = await _context.Faculties
                .AsNoTracking()
                .Where(f => f.IsDelete == false || f.IsDelete == null)
                .OrderBy(f => f.FacultyName)
                .Select(f => f.FacultyName)
                .Distinct()
                .ToListAsync();

            var majors = await _context.Majors
                .AsNoTracking()
                .Include(m => m.Faculty)
                .Where(m => m.IsDelete == false || m.IsDelete == null)
                .OrderBy(m => m.MajorName)
                .Select(m => new MajorDropdownItemDto
                {
                    MajorId = m.MajorId,
                    MajorName = m.MajorName,
                    FacultyName = m.Faculty != null ? m.Faculty.FacultyName : null
                })
                .ToListAsync();

            var academicYears = await _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => !string.IsNullOrEmpty(r.AcademicYearRange))
                .Select(r => r.AcademicYearRange!)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            var semesters = await _context.Semesters
                .AsNoTracking()
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .OrderBy(s => s.Sequence)
                .Select(s => new SemesterDropdownItemDto
                {
                    SemesterId = s.SemesterId,
                    SemesterName = s.SemesterName,
                    Sequence = s.Sequence
                })
                .ToListAsync();

            var subjects = await _context.Subjects
                .AsNoTracking()
                .Include(s => s.Faculty)
                .Include(s => s.Major)
                    .ThenInclude(m => m.Faculty)
                .Include(s => s.Semester)
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .OrderBy(s => s.SemesterId)
                .ThenBy(s => s.SubjectCode)
                .Select(s => new SubjectDropdownItemDto
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    FacultyName = s.Faculty != null ? s.Faculty.FacultyName : (s.Major != null && s.Major.Faculty != null ? s.Major.Faculty.FacultyName : null),
                    MajorName = s.Major != null ? s.Major.MajorName : null,
                    SemesterName = s.Semester != null ? s.Semester.SemesterName : null,
                    SemesterId = s.SemesterId
                })
                .ToListAsync();

            return Ok(new StudentRankingFilterOptionsDto
            {
                Faculties = faculties,
                Majors = majors,
                AcademicYears = academicYears,
                Semesters = semesters,
                Subjects = subjects
            });
        }
    }
}
