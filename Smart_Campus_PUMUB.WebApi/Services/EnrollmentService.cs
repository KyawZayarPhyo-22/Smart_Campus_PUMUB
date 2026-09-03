using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.Database.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Services
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly SmartCampusDbContext _context;

        public EnrollmentService(SmartCampusDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, List<int> MissingPrerequisites)> EnrollStudentAsync(int studentId, int newSubjectId, int semesterId)
        {
            // 1. Get the prerequisite subjects for the new subject
            var prerequisiteSubjectIds = await _context.SubjectPrerequisites
                .Where(sp => sp.SubjectId == newSubjectId)
                .Select(sp => sp.PrerequisiteSubjectId)
                .ToListAsync();

            var missingPrerequisites = new List<int>();

            // 2. Check if the student has passed all prerequisite subjects
            if (prerequisiteSubjectIds.Any())
            {
                var passedSubjectIds = await _context.StudentSubjectResults
                    .Where(r => r.StudentId == studentId && r.IsPass && r.SubjectId.HasValue)
                    .Select(r => r.SubjectId.Value)
                    .ToListAsync();

                missingPrerequisites = prerequisiteSubjectIds.Except(passedSubjectIds).ToList();

                if (missingPrerequisites.Any())
                {
                    return (false, "Student has not passed all prerequisite subjects.", missingPrerequisites);
                }
            }

            // 3. Check if the student is already enrolled in the same semester
            var existingEnrollment = await _context.StudentSubjectEnrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.SubjectId == newSubjectId && e.SemesterId == semesterId);

            if (existingEnrollment != null)
            {
                return (false, "Student is already enrolled in this subject for the given semester.", new List<int>());
            }

            // 4. Enroll the student
            var enrollment = new StudentSubjectEnrollment
            {
                StudentId = studentId,
                SubjectId = newSubjectId,
                SemesterId = semesterId,
                EnrollmentDate = DateTime.Now,
                Status = 1, // Active
                CreatedDateTime = DateTime.Now,
                CreatedBy = "System" // Could be updated to take the current user's ID/name
            };

            _context.StudentSubjectEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            return (true, "Enrollment successful.", new List<int>());
        }

        public async Task<List<StudentEnrollmentResultModel>> GetStudentEnrollmentsWithResultsAsync(int studentId)
        {
            var student = await _context.Students
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId || s.UserId == studentId);

            int actualStudentId = student?.StudentId ?? studentId;
            int? actualUserId = student?.UserId ?? (studentId > 0 ? studentId : null);

            var regIds = await _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => (r.IsDelete == false || r.IsDelete == null) &&
                            (r.Status == "Approved" || r.Status == "Confirmed") &&
                            r.RegistrationPayments.Any(p => (p.IsDelete == false || p.IsDelete == null) && p.Status == "Approved") &&
                            ((actualUserId.HasValue && r.UserId == actualUserId.Value) || 
                             (student != null && !string.IsNullOrEmpty(student.CurrentRollNo) && r.RollNo == student.CurrentRollNo)))
                .Select(r => r.RegistrationId)
                .ToListAsync();

            if (!regIds.Any())
            {
                return new List<StudentEnrollmentResultModel>();
            }

            var resultsList = await _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value))
                .OrderByDescending(r => r.ResultId)
                .Select(r => new StudentEnrollmentResultModel
                {
                    EnrollmentId = r.ResultId,
                    RegistrationId = r.RegistrationId,
                    StudentId = r.StudentId ?? actualStudentId,
                    StudentName = r.Registration != null ? (!string.IsNullOrEmpty(r.Registration.StudentNameEn) ? r.Registration.StudentNameEn : r.Registration.StudentNameMm) : (student != null ? student.StudentName : string.Empty),
                    RollNo = r.Registration != null ? r.Registration.RollNo : (student != null ? student.CurrentRollNo : string.Empty),
                    SemesterId = r.SemesterId ?? (r.Subject != null ? r.Subject.SemesterId : 0),
                    SemesterName = r.Semester != null ? r.Semester.SemesterName : (r.Subject != null && r.Subject.Semester != null ? r.Subject.Semester.SemesterName : $"Semester {r.SemesterId}"),
                    SubjectId = r.SubjectId ?? 0,
                    SubjectCode = r.Subject != null ? r.Subject.SubjectCode : string.Empty,
                    SubjectName = r.Subject != null ? r.Subject.SubjectName : string.Empty,
                    EnrollmentDate = r.ResultDate ?? r.CreatedDateTime ?? DateTime.Now,
                    MaxMarks = r.MaxMarks,
                    MarksObtained = r.MarksObtained,
                    Grade = r.Grade ?? string.Empty,
                    IsPass = r.IsPass
                })
                .ToListAsync();

            return resultsList;
        }

        public async Task<List<StudentEnrollmentResultModel>> GetAllEnrollmentsWithResultsAsync()
        {
            var results = await _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => (r.IsDelete == false || r.IsDelete == null) &&
                            (r.Status == "Approved" || r.Status == "Confirmed") &&
                            r.RegistrationPayments.Any(p => (p.IsDelete == false || p.IsDelete == null) && p.Status == "Approved"))
                .OrderByDescending(r => r.RegistrationId)
                .Select(r => new StudentEnrollmentResultModel
                {
                    EnrollmentId = r.RegistrationId,
                    RegistrationId = r.RegistrationId,
                    StudentId = r.UserId ?? 0,
                    StudentName = !string.IsNullOrEmpty(r.StudentNameEn) ? r.StudentNameEn : (r.StudentNameMm ?? string.Empty),
                    RollNo = r.RollNo ?? string.Empty,
                    Major = r.Major ?? string.Empty,
                    SemesterId = 0,
                    SemesterName = r.AcademicYearLevel ?? string.Empty,
                    SubjectId = 0,
                    SubjectCode = string.Empty,
                    SubjectName = string.Empty,
                    EnrollmentDate = r.CreatedDatetime ?? r.ApplicationDate ?? DateTime.Now,
                    MaxMarks = null,
                    MarksObtained = null,
                    Grade = string.Empty,
                    IsPass = true
                })
                .ToListAsync();

            return results;
        }

        public async Task<StudentEnrollmentDetailResponseModel?> GetEnrollmentDetailsAsync(int registrationId)
        {
            var reg = await _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => r.RegistrationId == registrationId && 
                            (r.IsDelete == false || r.IsDelete == null) &&
                            (r.Status == "Approved" || r.Status == "Confirmed") &&
                            r.RegistrationPayments.Any(p => (p.IsDelete == false || p.IsDelete == null) && p.Status == "Approved"))
                .Select(r => new
                {
                    r.RegistrationId,
                    r.UserId,
                    r.NewStudentAccId,
                    r.RollNo,
                    r.StudentNameEn,
                    r.StudentNameMm,
                    r.Major,
                    r.AcademicYearLevel
                })
                .FirstOrDefaultAsync();

            if (reg == null)
            {
                return null;
            }

            // 1. Resolve Semester directly from registration
            var yearLevelStr = (reg.AcademicYearLevel ?? "").Trim();
            var semesters = await _context.Semesters
                .AsNoTracking()
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .OrderBy(s => s.Sequence)
                .ToListAsync();

            Semester? matchedSemester = semesters.FirstOrDefault(s =>
                !string.IsNullOrEmpty(yearLevelStr) &&
                string.Equals(s.SemesterName.Trim(), yearLevelStr, StringComparison.OrdinalIgnoreCase)
            );

            if (matchedSemester == null && !string.IsNullOrEmpty(yearLevelStr))
            {
                matchedSemester = semesters.FirstOrDefault(s =>
                    yearLevelStr.Contains(s.SemesterName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    s.SemesterName.Trim().Contains(yearLevelStr, StringComparison.OrdinalIgnoreCase)
                );
            }

            if (matchedSemester == null && !string.IsNullOrEmpty(yearLevelStr))
            {
                var lower = yearLevelStr.ToLower();
                int? seq = null;
                if (lower.Contains("first year") && lower.Contains("first sem")) seq = 1;
                else if (lower.Contains("first year") && lower.Contains("second sem")) seq = 2;
                else if (lower.Contains("second year") && lower.Contains("first sem")) seq = 3;
                else if (lower.Contains("second year") && lower.Contains("second sem")) seq = 4;
                else if (lower.Contains("third year") && lower.Contains("first sem")) seq = 5;
                else if (lower.Contains("third year") && lower.Contains("second sem")) seq = 6;
                else if (lower.Contains("fourth year") && lower.Contains("first sem")) seq = 7;
                else if (lower.Contains("fourth year") && lower.Contains("second sem")) seq = 8;
                else if (lower.Contains("fifth year") && lower.Contains("first sem")) seq = 9;
                else if (lower.Contains("sem 1") || lower.Contains("sem-1") || lower.Contains("semester 1")) seq = 1;
                else if (lower.Contains("sem 2") || lower.Contains("sem-2") || lower.Contains("semester 2")) seq = 2;
                else if (lower.Contains("sem 3") || lower.Contains("sem-3") || lower.Contains("semester 3")) seq = 3;
                else if (lower.Contains("sem 4") || lower.Contains("sem-4") || lower.Contains("semester 4")) seq = 4;
                else if (lower.Contains("sem 5") || lower.Contains("sem-5") || lower.Contains("semester 5")) seq = 5;
                else if (lower.Contains("sem 6") || lower.Contains("sem-6") || lower.Contains("semester 6")) seq = 6;
                else if (lower.Contains("sem 7") || lower.Contains("sem-7") || lower.Contains("semester 7")) seq = 7;
                else if (lower.Contains("sem 8") || lower.Contains("sem-8") || lower.Contains("semester 8")) seq = 8;
                else if (lower.Contains("sem 9") || lower.Contains("sem-9") || lower.Contains("semester 9")) seq = 9;

                if (seq.HasValue)
                {
                    matchedSemester = semesters.FirstOrDefault(s => s.Sequence == seq.Value);
                }
            }

            if (matchedSemester == null && semesters.Any())
            {
                matchedSemester = semesters.First();
            }

            var semesterId = matchedSemester?.SemesterId ?? 0;
            var semesterName = matchedSemester?.SemesterName ?? reg.AcademicYearLevel ?? string.Empty;
            var semesterSeq = matchedSemester?.Sequence ?? 1;

            // 2. Resolve Major
            var majorStr = (reg.Major ?? "").Trim();
            var majors = await _context.Majors
                .AsNoTracking()
                .Where(m => m.IsDelete == false || m.IsDelete == null)
                .ToListAsync();

            var matchedMajor = majors.FirstOrDefault(m =>
                !string.IsNullOrEmpty(majorStr) && (
                    string.Equals(m.MajorName.Trim(), majorStr, StringComparison.OrdinalIgnoreCase) ||
                    majorStr.Contains(m.MajorName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    m.MajorName.Trim().Contains(majorStr, StringComparison.OrdinalIgnoreCase)
                )
            );

            int? majorId = matchedMajor?.MajorId;
            var majorName = matchedMajor?.MajorName ?? reg.Major ?? string.Empty;
            bool isCS = majorName.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) || majorStr.Equals("CS", StringComparison.OrdinalIgnoreCase);
            bool isCT = majorName.Contains("Computer Technology", StringComparison.OrdinalIgnoreCase) || majorStr.Equals("CT", StringComparison.OrdinalIgnoreCase);

            // 3. Query Subjects for this Semester & Major based on Semester Condition
            var allSemesterSubjects = await _context.Subjects
                .AsNoTracking()
                .Include(s => s.Major)
                .Include(s => s.Semester)
                .Where(s => (s.IsDelete == false || s.IsDelete == null) && s.SemesterId == semesterId)
                .ToListAsync();

            List<Subject> filteredSubjects;

            // Condition 1: Semester I, II, III (Seq 1, 2, 3) -> All subjects are common for all students
            if (semesterSeq <= 3)
            {
                filteredSubjects = allSemesterSubjects;
            }
            // Condition 2: Semester IV and beyond -> Filter based on student Major
            else
            {
                filteredSubjects = allSemesterSubjects.Where(s =>
                {
                    var code = (s.SubjectCode ?? "").Trim().ToUpper();
                    var subMajorName = s.Major?.MajorName ?? "";

                    bool isCommon = code.StartsWith("CST-") || code.StartsWith("E-") || code.StartsWith("P-") || code.StartsWith("M-") ||
                                    s.MajorId == null || subMajorName == "Information Technology";

                    if (isCS)
                    {
                        return isCommon || code.StartsWith("CS-") || subMajorName == "Computer Science" || (majorId.HasValue && s.MajorId == majorId.Value);
                    }
                    else if (isCT)
                    {
                        return isCommon || code.StartsWith("CT-") || subMajorName == "Computer Technology" || (majorId.HasValue && s.MajorId == majorId.Value);
                    }
                    else if (majorId.HasValue)
                    {
                        return isCommon || s.MajorId == majorId.Value || subMajorName.Equals(majorName, StringComparison.OrdinalIgnoreCase);
                    }

                    return true;
                }).ToList();
            }

            var subjects = filteredSubjects
                .OrderBy(s => s.SubjectType)
                .ThenBy(s => s.SubjectCode)
                .ToList();

            var subjectIds = subjects.Select(s => s.SubjectId).ToList();

            // 5. Fetch existing StudentSubjectResults for this registration (Lightweight)
            var existingResults = await _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.RegistrationId == registrationId && r.SubjectId.HasValue)
                .ToListAsync();

            var existingSubjectIds = existingResults.Select(r => r.SubjectId!.Value).ToHashSet();
            if (existingSubjectIds.Any())
            {
                var registeredSubjects = await _context.Subjects
                    .AsNoTracking()
                    .Include(s => s.Major)
                    .Include(s => s.Semester)
                    .Where(s => existingSubjectIds.Contains(s.SubjectId))
                    .ToListAsync();

                if (registeredSubjects.Any())
                {
                    subjects = registeredSubjects.OrderBy(s => s.SubjectType).ThenBy(s => s.SubjectCode).ToList();
                    subjectIds = subjects.Select(s => s.SubjectId).ToList();
                }
            }

            // 4. Fetch Prerequisites for these subjects
            var prerequisites = await _context.SubjectPrerequisites
                .AsNoTracking()
                .Include(sp => sp.PrerequisiteSubject)
                .Where(sp => subjectIds.Contains(sp.SubjectId))
                .ToListAsync();

            // Fetch prior failed and passed subjects for this student (Lightweight)
            var studentPriorFailedSubjectIds = new HashSet<int>();
            var studentPriorPassedSubjectIds = new HashSet<int>();

            var priorRegQuery = _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => r.RegistrationId != registrationId && (r.IsDelete == false || r.IsDelete == null));

            if (reg.UserId.HasValue && reg.UserId.Value > 0)
                priorRegQuery = priorRegQuery.Where(r => r.UserId == reg.UserId.Value);
            else if (reg.NewStudentAccId.HasValue && reg.NewStudentAccId.Value > 0)
                priorRegQuery = priorRegQuery.Where(r => r.NewStudentAccId == reg.NewStudentAccId.Value);
            else if (!string.IsNullOrWhiteSpace(reg.RollNo))
                priorRegQuery = priorRegQuery.Where(r => r.RollNo == reg.RollNo.Trim());

            var priorRegIds = await priorRegQuery.Select(r => r.RegistrationId).ToListAsync();
            if (priorRegIds.Any())
            {
                var priorResultsLightweight = await _context.StudentSubjectResults
                    .AsNoTracking()
                    .Where(r => r.RegistrationId.HasValue && priorRegIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue)
                    .Select(r => new
                    {
                        SubjectId = r.SubjectId!.Value,
                        r.Grade,
                        r.IsPass,
                        r.ReexamGrade,
                        r.ReexamIsPass
                    })
                    .ToListAsync();

                foreach (var r in priorResultsLightweight)
                {
                    bool isPassed = r.IsPass || IsGradePass(r.Grade) || r.ReexamIsPass == true || IsGradePass(r.ReexamGrade);
                    if (isPassed)
                    {
                        studentPriorPassedSubjectIds.Add(r.SubjectId);
                    }
                    else if (!string.IsNullOrEmpty(r.Grade) && (r.Grade == "D" || r.Grade == "F" || r.Grade == "Fail"))
                    {
                        studentPriorFailedSubjectIds.Add(r.SubjectId);
                    }
                }
            }

            var subjectItems = subjects.Select(s =>
            {
                var prereqList = prerequisites
                    .Where(p => p.SubjectId == s.SubjectId && p.PrerequisiteSubject != null)
                    .Select(p => $"{p.PrerequisiteSubject!.SubjectCode} ({p.PrerequisiteSubject.SubjectName})")
                    .ToList();

                var prereqInfo = prereqList.Any() ? string.Join(", ", prereqList) : "-";
                var resultRecord = existingResults.FirstOrDefault(r => r.SubjectId == s.SubjectId);
                var subSemesterName = s.Semester?.SemesterName ?? (s.SemesterId != semesterId ? $"Semester {s.SemesterId}" : semesterName);

                bool isFromPastSemester = s.SemesterId != semesterId || (s.Semester != null && s.Semester.Sequence < semesterSeq);
                bool isPassed = (resultRecord != null && (resultRecord.IsPass || IsGradePass(resultRecord.Grade) || resultRecord.ReexamIsPass == true || IsGradePass(resultRecord.ReexamGrade))) ||
                                studentPriorPassedSubjectIds.Contains(s.SubjectId);

                bool isRetake = isFromPastSemester && studentPriorFailedSubjectIds.Contains(s.SubjectId);
                bool isCarryOver = isFromPastSemester && !isRetake;

                int credit = s.Credit > 0 ? s.Credit : 3;
                decimal? marks = resultRecord?.MarksObtained;
                string? gradeLetter = resultRecord?.Grade;
                decimal gradePoint = 0.0m;
                string status = string.Empty;

                if (!string.IsNullOrWhiteSpace(gradeLetter))
                {
                    gradePoint = GradeCalculator.GetGradePoint(gradeLetter);
                    status = GradeCalculator.GetGradeStatus(gradeLetter);
                }
                else if (marks.HasValue)
                {
                    var info = GradeCalculator.GetGradeInfoFromMarks(marks.Value);
                    gradeLetter = info.LetterGrade;
                    gradePoint = info.GradePoint;
                    status = info.Status;
                }

                decimal gradePointEarned = GradeCalculator.CalculateGradePointsEarned(gradePoint, credit);

                return new StudentSubjectGradeItemModel
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    SemesterName = subSemesterName,
                    SubjectType = s.SubjectType,
                    PrerequisiteInfo = prereqInfo,
                    CreditUnit = credit,
                    MarksObtained = marks,
                    Grade = gradeLetter,
                    GradePoint = gradePoint,
                    GradePointEarned = gradePointEarned,
                    Status = status,
                    ResultId = resultRecord?.ResultId,
                    IsPass = resultRecord?.IsPass ?? false,
                    ReexamGrade = resultRecord?.ReexamGrade,
                    ReexamMarksObtained = resultRecord?.MarksObtained,
                    ReexamIsPass = resultRecord?.ReexamIsPass,
                    IsSubjectDisqualified = resultRecord?.IsDisqualified ?? false,
                    AttemptNumber = resultRecord?.AttemptNumber ?? 1,
                    IsRetake = isRetake,
                    IsCarriedOver = isCarryOver
                };
            }).ToList();

            // Calculate prior semester GPAs for this student (Lightweight: 0 images loaded)
            var priorSemesterGpas = new List<decimal>();
            if (semesterSeq > 1 && priorRegIds.Any())
            {
                var allSemesters = await _context.Semesters.AsNoTracking().ToListAsync();
                var allSubjectsMap = await _context.Subjects.AsNoTracking().ToDictionaryAsync(s => s.SubjectId);

                var priorResults = await _context.StudentSubjectResults
                    .AsNoTracking()
                    .Where(r => r.RegistrationId.HasValue && priorRegIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue)
                    .Select(r => new
                    {
                        r.SemesterId,
                        r.SubjectId,
                        r.Grade,
                        r.ReexamGrade,
                        r.ReexamIsPass,
                        r.IsPass
                    })
                    .ToListAsync();

                var groupedBySem = priorResults
                    .Where(r =>
                    {
                        int sId = r.SemesterId ?? (r.SubjectId.HasValue && allSubjectsMap.TryGetValue(r.SubjectId.Value, out var sObj) ? sObj.SemesterId : 0);
                        if (sId <= 0 || sId == semesterId) return false;
                        var semObj = allSemesters.FirstOrDefault(s => s.SemesterId == sId);
                        return semObj != null && semObj.Sequence < semesterSeq;
                    })
                    .GroupBy(r => r.SemesterId ?? (r.SubjectId.HasValue && allSubjectsMap.TryGetValue(r.SubjectId.Value, out var sObj) ? sObj.SemesterId : 0))
                    .Where(g => g.Key > 0);

                foreach (var grp in groupedBySem.OrderBy(g => allSemesters.FirstOrDefault(s => s.SemesterId == g.Key)?.Sequence ?? g.Key))
                {
                    int totalCredits = 0;
                    decimal totalPoints = 0.0m;
                    foreach (var res in grp)
                    {
                        int credit = (res.SubjectId.HasValue && allSubjectsMap.TryGetValue(res.SubjectId.Value, out var sObj) && sObj.Credit > 0) ? sObj.Credit : 3;
                        string effectiveGrade = !string.IsNullOrEmpty(res.ReexamGrade) && (res.ReexamIsPass == true || IsGradePass(res.ReexamGrade))
                            ? res.ReexamGrade
                            : (res.Grade ?? string.Empty);

                        decimal pt = GradeCalculator.GetGradePoint(effectiveGrade);
                        totalCredits += credit;
                        totalPoints += GradeCalculator.CalculateGradePointsEarned(pt, credit);
                    }

                    if (totalCredits > 0)
                    {
                        var semGpa = GradeCalculator.CalculateSemesterGPA(totalPoints, totalCredits);
                        if (semGpa > 0) priorSemesterGpas.Add(semGpa);
                    }
                }
            }

            return new StudentEnrollmentDetailResponseModel
            {
                RegistrationId = registrationId,
                StudentId = null,
                StudentName = !string.IsNullOrEmpty(reg.StudentNameEn) ? reg.StudentNameEn : (reg.StudentNameMm ?? string.Empty),
                RollNo = reg.RollNo ?? string.Empty,
                SemesterId = semesterId,
                SemesterName = semesterName,
                MajorName = majorName,
                PriorSemesterGPAs = priorSemesterGpas,
                Subjects = subjectItems
            };
        }

        private static bool IsGradePass(string? grade)
        {
            if (string.IsNullOrWhiteSpace(grade)) return false;
            var g = grade.Trim().ToUpper();
            // A+, A, A-, B+, B, B-, C+, C are PASS (True)
            // D, F are FAIL (False)
            return g == "A+" || g == "A" || g == "A-" || g == "B+" || g == "B" || g == "B-" || g == "C+" || g == "C";
        }

        public async Task<ActionResponseModel> SaveStudentGradesAsync(SaveStudentGradesRequestModel request)
        {
            try
            {
                if (request.RegistrationId <= 0)
                {
                    return new ActionResponseModel { IsSuccess = false, Message = "Invalid Registration ID." };
                }

                var allSubjectIds = request.Grades.Select(g => g.SubjectId).Distinct().ToList();
                var subjectLookup = await _context.Subjects.AsNoTracking()
                    .Where(s => allSubjectIds.Contains(s.SubjectId))
                    .ToDictionaryAsync(s => s.SubjectId, s => s.SemesterId);

                foreach (var item in request.Grades)
                {
                    var gradeStr = string.IsNullOrWhiteSpace(item.Grade) ? null : item.Grade.Trim();
                    decimal? marks = item.MarksObtained;

                    if (string.IsNullOrWhiteSpace(gradeStr) && marks.HasValue)
                    {
                        var info = GradeCalculator.GetGradeInfoFromMarks(marks.Value);
                        gradeStr = info.LetterGrade;
                    }

                    bool isPass = IsGradePass(gradeStr);
                    int itemSemesterId = subjectLookup.TryGetValue(item.SubjectId, out int subSemesterId) ? subSemesterId : request.SemesterId;

                    var allMatchingQuery = _context.StudentSubjectResults.Where(r => r.SubjectId == item.SubjectId);
                    if (request.RegistrationId > 0)
                    {
                        allMatchingQuery = allMatchingQuery.Where(r => r.RegistrationId == request.RegistrationId);
                    }
                    else if (request.StudentId.HasValue && request.StudentId.Value > 0)
                    {
                        allMatchingQuery = allMatchingQuery.Where(r => r.StudentId == request.StudentId.Value && r.SemesterId == itemSemesterId);
                    }

                    var allMatching = await allMatchingQuery.ToListAsync();

                    if (allMatching.Any())
                    {
                        var existing = allMatching.First();
                        existing.MarksObtained = marks;
                        existing.Grade = gradeStr;
                        existing.IsPass = isPass;
                        existing.SemesterId = itemSemesterId;
                        existing.ModifiedDateTime = DateTime.Now;
                        existing.ModifiedBy = "System";

                        if (allMatching.Count > 1)
                        {
                            _context.StudentSubjectResults.RemoveRange(allMatching.Skip(1));
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(gradeStr) || marks.HasValue)
                    {
                        var newResult = new StudentSubjectResult
                        {
                            RegistrationId = request.RegistrationId,
                            StudentId = request.StudentId,
                            SubjectId = item.SubjectId,
                            SemesterId = itemSemesterId,
                            MarksObtained = marks,
                            Grade = gradeStr,
                            IsPass = isPass,
                            CreatedDateTime = DateTime.Now,
                            CreatedBy = "System"
                        };
                        _context.StudentSubjectResults.Add(newResult);
                    }
                }

                await _context.SaveChangesAsync();

                // =========================================================================
                // Business Logic: 50% Rule for Semester Progression
                // Clean up any dummy results with null SubjectId
                // If more than 50% passed (> total/2.0) -> Semester Pass
                // If 50% or less passed (<= total/2.0) -> Semester Fail
                // NOTE: Retake subjects from older semesters are EXCLUDED from current semester pass/fail calculation!
                // Only subjects belonging to the current semester (Subject.SemesterId == request.SemesterId) are counted.
                // =========================================================================
                var dummyRows = await _context.StudentSubjectResults
                    .Where(r => r.RegistrationId == request.RegistrationId && (!r.SubjectId.HasValue || r.SubjectId <= 0))
                    .ToListAsync();
                if (dummyRows.Any())
                {
                    _context.StudentSubjectResults.RemoveRange(dummyRows);
                    await _context.SaveChangesAsync();
                }

                var allRegistrationResults = await _context.StudentSubjectResults
                    .Include(r => r.Subject)
                    .Where(r => r.RegistrationId == request.RegistrationId && r.SubjectId.HasValue && r.SubjectId > 0)
                    .ToListAsync();

                if (allRegistrationResults.Any())
                {
                    var reg = await _context.StudentRegistrations
                        .FirstOrDefaultAsync(r => r.RegistrationId == request.RegistrationId);

                    int targetSemesterId = request.SemesterId;
                    int semSeq = 1;
                    if (targetSemesterId > 0)
                    {
                        var sem = await _context.Semesters.FirstOrDefaultAsync(s => s.SemesterId == targetSemesterId);
                        if (sem != null) semSeq = sem.Sequence ?? 1;
                    }
                    else if (reg != null && !string.IsNullOrWhiteSpace(reg.AcademicYearLevel))
                    {
                        var allSems = await _context.Semesters.AsNoTracking().ToListAsync();
                        var matchedSem = allSems.FirstOrDefault(s => string.Equals(s.SemesterName.Trim(), reg.AcademicYearLevel.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (matchedSem != null)
                        {
                            targetSemesterId = matchedSem.SemesterId;
                            semSeq = matchedSem.Sequence ?? 1;
                        }
                    }

                    // Filter strictly to current semester's curriculum subjects (exclude Retake subjects from previous semesters)
                    var currentSemesterResults = allRegistrationResults
                        .Where(r => r.Subject != null && targetSemesterId > 0 && r.Subject.SemesterId == targetSemesterId)
                        .ToList();

                    // Fallback to allRegistrationResults only if no subject matched specifically
                    if (!currentSemesterResults.Any())
                    {
                        currentSemesterResults = allRegistrationResults;
                    }

                    int totalCount = currentSemesterResults.Count;
                    int passCount = currentSemesterResults.Count(r => r.IsPass);

                    bool isSemesterPass = totalCount > 0 && (passCount > (totalCount / 2.0));
                    string semesterResultStatus = isSemesterPass ? "Pass" : "Fail";

                    if (reg != null)
                    {
                        var student = await _context.Students
                            .FirstOrDefaultAsync(s => (reg.UserId.HasValue && s.UserId == reg.UserId.Value) || 
                                                      (request.StudentId.HasValue && s.StudentId == request.StudentId.Value));

                        if (student != null)
                        {
                            switch (semSeq)
                            {
                                case 1: student.Sem1_Result = semesterResultStatus; break;
                                case 2: student.Sem2_Result = semesterResultStatus; break;
                                case 3: student.Sem3_Result = semesterResultStatus; break;
                                case 4: student.Sem4_Result = semesterResultStatus; break;
                                case 5: student.Sem5_Result = semesterResultStatus; break;
                                case 6: student.Sem6_Result = semesterResultStatus; break;
                                case 7: student.Sem7_Result = semesterResultStatus; break;
                                case 8: student.Sem8_Result = semesterResultStatus; break;
                                case 9: student.Sem9_Result = semesterResultStatus; break;
                            }
                            student.ModifiedDateTime = DateTime.Now;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                return new ActionResponseModel { IsSuccess = true, Message = "Grades saved successfully." };
            }
            catch (Exception ex)
            {
                return new ActionResponseModel { IsSuccess = false, Message = $"Error saving grades: {ex.Message}" };
            }
        }

        public async Task<ActionResponseModel> SaveReexamGradesAsync(SaveReexamGradesRequestModel request)
        {
            try
            {
                if (request.RegistrationId <= 0)
                {
                    return new ActionResponseModel { IsSuccess = false, Message = "Invalid Registration ID." };
                }

                var reg = await _context.StudentRegistrations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RegistrationId == request.RegistrationId);

                if (reg == null)
                {
                    return new ActionResponseModel { IsSuccess = false, Message = "Registration record not found." };
                }

                // Fetch all results for this registration
                var results = await _context.StudentSubjectResults
                    .Include(r => r.Subject)
                    .Where(r => r.RegistrationId == request.RegistrationId)
                    .ToListAsync();

                // Validate: If the student failed the entire semester (<=50% passed in current curriculum subjects), Re-exam is barred!
                var currentSemesterResults = results
                    .Where(r => (r.Subject != null && r.Subject.SemesterId == request.SemesterId) || (r.SemesterId == request.SemesterId))
                    .ToList();

                if (!currentSemesterResults.Any())
                {
                    currentSemesterResults = results;
                }

                int totalCount = currentSemesterResults.Count;
                int passCount = currentSemesterResults.Count(r => r.IsPass);

                if (totalCount > 0 && (passCount <= (totalCount / 2.0)))
                {
                    return new ActionResponseModel
                    {
                        IsSuccess = false,
                        Message = "ကျောင်းသားသည် စာသင်နှစ်ဝက်တစ်ခုလုံး ကျရှုံးထားသဖြင့် (Semester Fail) Re-exam ဖြေဆိုခွင့်/ထည့်သွင်းခွင့် မရှိပါခင်ဗျာ။"
                    };
                }

                // Fetch all past registrations for this student to calculate prior attempts accurately
                var allStudentRegIds = await _context.StudentRegistrations
                    .AsNoTracking()
                    .Where(r => (reg.UserId.HasValue && r.UserId == reg.UserId.Value) ||
                                (reg.NewStudentAccId.HasValue && r.NewStudentAccId == reg.NewStudentAccId.Value) ||
                                (!string.IsNullOrEmpty(reg.RollNo) && r.RollNo == reg.RollNo))
                    .Select(r => r.RegistrationId)
                    .ToListAsync();

                var allStudentResults = await _context.StudentSubjectResults
                    .Where(r => r.RegistrationId.HasValue && allStudentRegIds.Contains(r.RegistrationId.Value))
                    .ToListAsync();

                foreach (var item in request.ReexamGrades)
                {
                    var res = results.FirstOrDefault(r => r.SubjectId == item.SubjectId);
                    if (res == null) continue;

                    var reexamGradeStr = string.IsNullOrWhiteSpace(item.ReexamGrade) ? null : item.ReexamGrade.Trim();
                    decimal? marks = item.ReexamMarksObtained;

                    if (string.IsNullOrWhiteSpace(reexamGradeStr) && marks.HasValue)
                    {
                        var info = GradeCalculator.GetGradeInfoFromMarks(marks.Value);
                        reexamGradeStr = info.LetterGrade;
                    }

                    bool? isReexamPass = reexamGradeStr != null ? IsGradePass(reexamGradeStr) : null;

                    res.ReexamGrade = reexamGradeStr;
                    res.ReexamIsPass = isReexamPass;
                    res.ModifiedDateTime = DateTime.Now;
                    res.ModifiedBy = "Teacher/Admin (Reexam)";

                    if (isReexamPass == true)
                    {
                        // Re-exam Passed -> Subject is Cleared / Passed!
                        res.IsPass = true;
                        res.IsDisqualified = false;
                    }
                    else if (isReexamPass == false)
                    {
                        // Re-exam Failed -> Check if the student previously took and FAILED a Re-exam for this same subject in an earlier registration
                        int priorReexamFailedCount = allStudentResults
                            .Where(r => r.SubjectId == item.SubjectId &&
                                        r.RegistrationId != request.RegistrationId &&
                                        !r.IsPass &&
                                        (r.ReexamIsPass == false || (r.ReexamGrade == "D" || r.ReexamGrade == "F")))
                            .Select(r => r.RegistrationId)
                            .Distinct()
                            .Count();

                        // Only Disqualify if the student had already taken and failed a Re-exam on this subject previously (meaning this is the 2nd Re-exam failure after a Retake)
                        if (priorReexamFailedCount >= 1)
                        {
                            res.IsDisqualified = true;
                            res.IsPass = false;
                        }
                        else
                        {
                            res.IsDisqualified = false;
                            res.IsPass = false;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return new ActionResponseModel { IsSuccess = true, Message = "Re-exam grades saved successfully." };
            }
            catch (Exception ex)
            {
                return new ActionResponseModel { IsSuccess = false, Message = $"Error saving re-exam grades: {ex.Message}" };
            }
        }

        public async Task<List<SubjectModel>> GetSemesterSubjectsByMajorAsync(int semesterId, string? major, int? userId = null, int? newStudentAccId = null, string? rollNo = null)
        {
            var semester = await _context.Semesters
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SemesterId == semesterId && (s.IsDelete == false || s.IsDelete == null));

            int semesterSeq = semester?.Sequence ?? 1;
            var majorName = (major ?? "").Trim();
            bool isCS = majorName.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CS", StringComparison.OrdinalIgnoreCase);
            bool isCT = majorName.Contains("Computer Technology", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CT", StringComparison.OrdinalIgnoreCase);

            var query = _context.Subjects
                .AsNoTracking()
                .Include(s => s.Major)
                .Include(s => s.Semester)
                .Where(s => s.SemesterId == semesterId && (s.IsDelete == false || s.IsDelete == null));

            var allSubjects = await query.ToListAsync();
            List<Subject> filtered;

            if (semesterSeq <= 3)
            {
                filtered = allSubjects;
            }
            else
            {
                filtered = allSubjects.Where(s =>
                {
                    var code = (s.SubjectCode ?? "").Trim().ToUpper();
                    var subMajorName = s.Major?.MajorName ?? "";
                    bool isCommon = code.StartsWith("CST-") || code.StartsWith("E-") || code.StartsWith("P-") || code.StartsWith("M-") ||
                                    s.MajorId == null || subMajorName == "Information Technology";

                    if (isCS)
                    {
                        return isCommon || code.StartsWith("CS-") || subMajorName == "Computer Science";
                    }
                    else if (isCT)
                    {
                        return isCommon || code.StartsWith("CT-") || subMajorName == "Computer Technology";
                    }
                    else if (!string.IsNullOrEmpty(majorName))
                    {
                        return isCommon || subMajorName.Equals(majorName, StringComparison.OrdinalIgnoreCase);
                    }

                    return true;
                }).ToList();
            }

            var subjectList = filtered
                .OrderBy(s => s.SubjectType)
                .ThenBy(s => s.SubjectCode)
                .Select(s => new SubjectModel
                {
                    SubjectId = s.SubjectId,
                    SubjectName = s.SubjectName,
                    SubjectCode = s.SubjectCode,
                    Credit = s.Credit,
                    SubjectType = s.SubjectType,
                    SemesterId = s.SemesterId,
                    SemesterName = s.Semester?.SemesterName,
                    MajorId = s.MajorId,
                    MajorName = s.Major?.MajorName,
                    IsRetake = false
                }).ToList();

            var currentSubjectIds = subjectList.Select(s => s.SubjectId).ToHashSet();

            // =========================================================================
            // Retake Subjects from Previous Semesters:
            // Include failed subjects from previous semesters as Retake subjects
            // =========================================================================
            if ((userId.HasValue && userId.Value > 0) || (newStudentAccId.HasValue && newStudentAccId.Value > 0) || !string.IsNullOrWhiteSpace(rollNo))
            {
                var regQuery = _context.StudentRegistrations
                    .AsNoTracking()
                    .Where(r => r.IsDelete == false || r.IsDelete == null);

                if (userId.HasValue && userId.Value > 0)
                    regQuery = regQuery.Where(r => r.UserId == userId.Value);
                else if (newStudentAccId.HasValue && newStudentAccId.Value > 0)
                    regQuery = regQuery.Where(r => r.NewStudentAccId == newStudentAccId.Value);
                else if (!string.IsNullOrWhiteSpace(rollNo))
                    regQuery = regQuery.Where(r => r.RollNo == rollNo.Trim());

                var regIds = await regQuery.Select(r => r.RegistrationId).ToListAsync();

                if (regIds.Any())
                {
                    var allPastResults = await _context.StudentSubjectResults
                        .AsNoTracking()
                        .Include(r => r.Subject)
                            .ThenInclude(sub => sub.Semester)
                        .Include(r => r.Subject)
                            .ThenInclude(sub => sub.Major)
                        .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) &&
                                    r.SubjectId.HasValue)
                        .OrderByDescending(r => r.ResultId)
                        .ToListAsync();

                    // If student already selected/enrolled in electives for this current semester in a past attempt (repeating semester):
                    // remove fresh elective options from subjectList so unchosen electives do not appear;
                    // the failed ones will be added below as Retake subjects.
                    bool alreadyChoseElectivesForThisSem = allPastResults.Any(r =>
                        r.Subject != null &&
                        (r.Subject.SemesterId == semesterId || r.SemesterId == semesterId) &&
                        r.Subject.SubjectType == EnumSubjectType.Elective);

                    if (alreadyChoseElectivesForThisSem)
                    {
                        subjectList.RemoveAll(s => s.SubjectType == EnumSubjectType.Elective);
                        currentSubjectIds = subjectList.Select(s => s.SubjectId).ToHashSet();
                    }

                    var latestResultsPerSubject = allPastResults
                        .Where(r => r.Subject != null)
                        .GroupBy(r => r.SubjectId!.Value)
                        .Select(g => g.First()) // latest result per subject
                        .ToList();

                    var retakeSubjects = new List<SubjectModel>();

                    foreach (var r in latestResultsPerSubject)
                    {
                        var existing = subjectList.FirstOrDefault(s => s.SubjectId == r.SubjectId!.Value);

                        // 1. Permanent Disqualification (failed 2nd Re-exam)
                        if (r.IsDisqualified)
                        {
                            if (existing != null)
                            {
                                existing.IsSubjectDisqualified = true;
                                existing.IsRetake = false;
                                existing.IsSelected = false;
                                existing.IsPrerequisiteSatisfied = false;
                                existing.PrerequisiteStatusMessage = "၂ ကြိမ်မြောက် Re-exam ကျရှုံးခဲ့သဖြင့် ဤဘာသာရပ်အား အပြီးတိုင် Retake ယူခွင့်ပိတ်သိမ်းထားပါသည် (Credit ရရှိမည်မဟုတ်ပါ)";
                            }
                            else
                            {
                                subjectList.Add(new SubjectModel
                                {
                                    SubjectId = r.Subject!.SubjectId,
                                    SubjectName = r.Subject.SubjectName,
                                    SubjectCode = r.Subject.SubjectCode,
                                    Credit = r.Subject.Credit,
                                    SubjectType = r.Subject.SubjectType,
                                    SemesterId = r.Subject.SemesterId,
                                    SemesterName = r.Subject.Semester?.SemesterName,
                                    MajorId = r.Subject.MajorId,
                                    MajorName = r.Subject.Major?.MajorName,
                                    IsRetake = false,
                                    IsSubjectDisqualified = true,
                                    IsSelected = false,
                                    IsPrerequisiteSatisfied = false,
                                    PrerequisiteStatusMessage = "၂ ကြိမ်မြောက် Re-exam ကျရှုံးခဲ့သဖြင့် ဤဘာသာရပ်အား အပြီးတိုင် Retake ယူခွင့်ပိတ်သိမ်းထားပါသည် (Credit ရရှိမည်မဟုတ်ပါ)"
                                });
                                currentSubjectIds.Add(r.SubjectId!.Value);
                            }
                            continue;
                        }

                        // Check if regular exam was failed
                        bool regularFailed = !string.IsNullOrEmpty(r.Grade) && (r.Grade == "D" || r.Grade == "F" || r.Grade == "Fail") && !r.IsPass;
                        if (!regularFailed)
                        {
                            // Subject was passed in past attempt
                            // If this subject is in the current semester's subjectList and was already passed in a previous attempt,
                            // remove it so student doesn't have to retake an already passed subject.
                            if (existing != null && (r.IsPass || IsGradePass(r.Grade) || r.ReexamIsPass == true || IsGradePass(r.ReexamGrade)))
                            {
                                subjectList.Remove(existing);
                                currentSubjectIds.Remove(r.SubjectId!.Value);
                            }
                            continue;
                        }

                        // 2. Check Re-exam Status
                        bool reexamPassed = r.ReexamIsPass == true || IsGradePass(r.ReexamGrade);
                        if (reexamPassed)
                        {
                            // Subject Passed through Re-exam! Not a retake.
                            if (existing != null)
                            {
                                subjectList.Remove(existing);
                                currentSubjectIds.Remove(r.SubjectId!.Value);
                            }
                            continue;
                        }

                        bool reexamFailed = r.ReexamIsPass == false || (r.ReexamGrade == "D" || r.ReexamGrade == "F");
                        if (reexamFailed)
                        {
                            // Re-exam was taken and failed -> Now officially a Retake!
                            if (existing != null)
                            {
                                existing.IsRetake = true;
                                existing.IsSelected = true;
                            }
                            else
                            {
                                retakeSubjects.Add(new SubjectModel
                                {
                                    SubjectId = r.Subject!.SubjectId,
                                    SubjectName = r.Subject.SubjectName,
                                    SubjectCode = r.Subject.SubjectCode,
                                    Credit = r.Subject.Credit,
                                    SubjectType = r.Subject.SubjectType,
                                    SemesterId = r.Subject.SemesterId,
                                    SemesterName = r.Subject.Semester?.SemesterName,
                                    MajorId = r.Subject.MajorId,
                                    MajorName = r.Subject.Major?.MajorName,
                                    IsRetake = true,
                                    IsSelected = true,
                                    IsPrerequisiteSatisfied = true
                                });
                            }
                        }
                    }

                    subjectList.AddRange(retakeSubjects);
                    foreach (var retake in retakeSubjects)
                    {
                        currentSubjectIds.Add(retake.SubjectId);
                    }

                    // =========================================================================
                    // Missed / Carried-Over Subjects from Previous Semesters:
                    // When a subject in a past semester was NOT taken/enrolled because its prerequisite
                    // was not met at that time (e.g. English II in Semester II when English I was failed).
                    // Now that the prerequisite is passed (e.g. in Semester III), carry over the missed
                    // subject (e.g. English II) into the current semester curriculum.
                    // =========================================================================
                    var currentSemesterObj = await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.SemesterId == semesterId);
                    int currentSemesterSeq = currentSemesterObj?.Sequence ?? 1;

                    var pastSemesters = await _context.Semesters
                        .AsNoTracking()
                        .Where(s => s.Sequence < currentSemesterSeq && (s.IsDelete == false || s.IsDelete == null))
                        .OrderBy(s => s.Sequence)
                        .ToListAsync();

                    var allDbPrerequisites = await _context.SubjectPrerequisites
                        .AsNoTracking()
                        .Include(sp => sp.PrerequisiteSubject)
                        .ToListAsync();

                    foreach (var pastSem in pastSemesters)
                    {
                        var pastSemSubjects = await _context.Subjects
                            .AsNoTracking()
                            .Include(s => s.Semester)
                            .Include(s => s.Major)
                            .Where(s => s.SemesterId == pastSem.SemesterId && (s.IsDelete == false || s.IsDelete == null))
                            .ToListAsync();

                        if (!pastSemSubjects.Any()) continue;

                        var filteredPastSubs = pastSemSubjects.Where(s =>
                        {
                            var code = (s.SubjectCode ?? "").Trim().ToUpper();
                            var subMajorName = s.Major?.MajorName ?? "";
                            bool isCommon = code.StartsWith("CST-") || code.StartsWith("E-") || code.StartsWith("P-") || code.StartsWith("M-") ||
                                            s.MajorId == null || subMajorName == "Information Technology";

                            if (isCS)
                            {
                                return isCommon || code.StartsWith("CS-") || subMajorName == "Computer Science";
                            }
                            else if (isCT)
                            {
                                return isCommon || code.StartsWith("CT-") || subMajorName == "Computer Technology";
                            }
                            else if (!string.IsNullOrEmpty(majorName))
                            {
                                return isCommon || subMajorName.Equals(majorName, StringComparison.OrdinalIgnoreCase);
                            }
                            return true;
                        }).ToList();

                        // 1. Carried-over Core / Mandatory subjects
                        var missedCoreSubs = filteredPastSubs
                            .Where(s => s.SubjectType != EnumSubjectType.Elective && !currentSubjectIds.Contains(s.SubjectId))
                            .ToList();

                        foreach (var pastCore in missedCoreSubs)
                        {
                            bool alreadyPassed = allPastResults.Any(r => r.SubjectId == pastCore.SubjectId && (r.IsPass || IsGradePass(r.Grade)));
                            if (alreadyPassed) continue;

                            // Check if all prerequisites for this past core subject are NOW passed
                            var coreReqs = allDbPrerequisites.Where(p => p.SubjectId == pastCore.SubjectId).ToList();
                            bool allPrereqsMet = true;
                            foreach (var req in coreReqs)
                            {
                                bool prereqPassed = allPastResults.Any(r => r.SubjectId == req.PrerequisiteSubjectId && (r.IsPass || IsGradePass(r.Grade)));
                                if (!prereqPassed)
                                {
                                    allPrereqsMet = false;
                                    break;
                                }
                            }

                            if (allPrereqsMet)
                            {
                                subjectList.Add(new SubjectModel
                                {
                                    SubjectId = pastCore.SubjectId,
                                    SubjectName = pastCore.SubjectName,
                                    SubjectCode = pastCore.SubjectCode,
                                    Credit = pastCore.Credit,
                                    SubjectType = pastCore.SubjectType,
                                    SemesterId = pastCore.SemesterId,
                                    SemesterName = pastCore.Semester?.SemesterName,
                                    MajorId = pastCore.MajorId,
                                    MajorName = pastCore.Major?.MajorName,
                                    IsRetake = false,
                                    IsCarriedOver = true,
                                    IsSelected = false,
                                    IsPrerequisiteSatisfied = true
                                });
                                currentSubjectIds.Add(pastCore.SubjectId);
                            }
                        }

                        // 2. Carried-over Electives (only if student never took any elective in that past semester)
                        bool hasEverEnrolledElectiveInPastSem = allPastResults
                            .Any(r => r.Subject != null &&
                                      (r.Subject.SemesterId == pastSem.SemesterId || r.SemesterId == pastSem.SemesterId) &&
                                      r.Subject.SubjectType == EnumSubjectType.Elective);

                        if (!hasEverEnrolledElectiveInPastSem)
                        {
                            var missedElectives = filteredPastSubs
                                .Where(s => s.SubjectType == EnumSubjectType.Elective && !currentSubjectIds.Contains(s.SubjectId))
                                .Select(s => new SubjectModel
                                {
                                    SubjectId = s.SubjectId,
                                    SubjectName = s.SubjectName,
                                    SubjectCode = s.SubjectCode,
                                    Credit = s.Credit,
                                    SubjectType = s.SubjectType,
                                    SemesterId = s.SemesterId,
                                    SemesterName = s.Semester?.SemesterName,
                                    MajorId = s.MajorId,
                                    MajorName = s.Major?.MajorName,
                                    IsRetake = false,
                                    IsCarriedOver = true,
                                    IsSelected = false
                                })
                                .ToList();

                            subjectList.AddRange(missedElectives);
                            foreach (var m in missedElectives)
                            {
                                currentSubjectIds.Add(m.SubjectId);
                            }
                        }
                    }
                }
            }

            // =========================================================================
            // Prerequisites & Subject Eligibility Validation:
            // Check if student passed prerequisite subjects with Grade A+ to C (IsPass == true)
            // If a Core subject has an unsatisfied prerequisite, student CANNOT take it in this semester.
            // =========================================================================
            var allSubjectIds = subjectList.Select(s => s.SubjectId).ToList();
            var allPrerequisites = await _context.SubjectPrerequisites
                .AsNoTracking()
                .Include(sp => sp.PrerequisiteSubject)
                .Where(sp => allSubjectIds.Contains(sp.SubjectId))
                .ToListAsync();

            List<StudentSubjectResult> studentPastResults = new();
            if ((userId.HasValue && userId.Value > 0) || (newStudentAccId.HasValue && newStudentAccId.Value > 0) || !string.IsNullOrWhiteSpace(rollNo))
            {
                var regQuery = _context.StudentRegistrations
                    .AsNoTracking()
                    .Where(r => r.IsDelete == false || r.IsDelete == null);

                if (userId.HasValue && userId.Value > 0)
                    regQuery = regQuery.Where(r => r.UserId == userId.Value);
                else if (newStudentAccId.HasValue && newStudentAccId.Value > 0)
                    regQuery = regQuery.Where(r => r.NewStudentAccId == newStudentAccId.Value);
                else if (!string.IsNullOrWhiteSpace(rollNo))
                    regQuery = regQuery.Where(r => r.RollNo == rollNo.Trim());

                var regIds = await regQuery.Select(r => r.RegistrationId).ToListAsync();
                if (regIds.Any())
                {
                    studentPastResults = await _context.StudentSubjectResults
                        .AsNoTracking()
                        .Include(r => r.Subject)
                            .ThenInclude(sub => sub.Semester)
                        .Include(r => r.Subject)
                            .ThenInclude(sub => sub.Major)
                        .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue)
                        .OrderByDescending(r => r.ResultId)
                        .ToListAsync();
                }
            }

            // Filter out Electives whose Prerequisite is an Elective from a past semester
            // that the student NEVER chose
            if (studentPastResults.Any())
            {
                subjectList.RemoveAll(sub =>
                {
                    if (sub.SubjectType != EnumSubjectType.Elective || sub.IsRetake) return false;

                    var reqs = allPrerequisites.Where(p => p.SubjectId == sub.SubjectId && p.PrerequisiteSubject != null).ToList();
                    foreach (var req in reqs)
                    {
                        var prereqSub = req.PrerequisiteSubject;
                        if (prereqSub != null && prereqSub.SubjectType == EnumSubjectType.Elective)
                        {
                            bool tookElectivesInThatSem = studentPastResults.Any(r =>
                                r.Subject != null &&
                                (r.Subject.SemesterId == prereqSub.SemesterId || r.SemesterId == prereqSub.SemesterId) &&
                                r.Subject.SubjectType == EnumSubjectType.Elective);

                            if (tookElectivesInThatSem)
                            {
                                bool enrolledInThisPrereq = studentPastResults.Any(r => r.SubjectId == req.PrerequisiteSubjectId);
                                if (!enrolledInThisPrereq)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                });
            }

            foreach (var sub in subjectList)
            {
                var reqs = allPrerequisites.Where(p => p.SubjectId == sub.SubjectId && p.PrerequisiteSubject != null).ToList();
                if (reqs.Any())
                {
                    sub.PrerequisiteSubjectIds = reqs.Select(p => p.PrerequisiteSubjectId).ToList();
                    var reqNames = reqs.Select(p => $"{p.PrerequisiteSubject!.SubjectCode} ({p.PrerequisiteSubject.SubjectName})").ToList();
                    sub.PrerequisiteInfo = string.Join(", ", reqNames);

                    // A+ to C is Pass; D/F is Fail
                    // Student must have passed the prerequisite subject in ANY past attempt (e.g. initial or retake)
                    bool allPassed = true;
                    List<string> missingList = new();

                    foreach (var req in reqs)
                    {
                        var pastAttempts = studentPastResults.Where(r => r.SubjectId == req.PrerequisiteSubjectId).ToList();
                        bool isPassed = pastAttempts.Any(r => r.IsPass || IsGradePass(r.Grade));
                        if (!isPassed)
                        {
                            allPassed = false;
                            missingList.Add(req.PrerequisiteSubject != null ? $"{req.PrerequisiteSubject.SubjectCode} ({req.PrerequisiteSubject.SubjectName})" : $"Subject #{req.PrerequisiteSubjectId}");
                        }
                    }

                    sub.IsPrerequisiteSatisfied = allPassed;
                    if (allPassed)
                    {
                        sub.PrerequisiteStatusMessage = "Pre-Requisite ပြည့်မီပါသည်";
                    }
                    else
                    {
                        sub.PrerequisiteStatusMessage = $"Pre-Requisite မပြည့်မီပါ ({string.Join(", ", missingList)} ကို အရင် အောင်မြင်ထားရပါမည်)";
                    }
                }
                else
                {
                    sub.PrerequisiteInfo = "-";
                    sub.IsPrerequisiteSatisfied = true;
                    sub.PrerequisiteStatusMessage = "Pre-Requisite မလိုအပ်ပါ";
                }

                // Initial selection state:
                // Retake subjects are prioritized and mandatory (IsSelected = true)
                // Other subjects (Core and Electives) are chosen manually by the student (IsSelected = false)
                if (sub.IsRetake)
                {
                    sub.IsSelected = true;
                }
                else
                {
                    sub.IsSelected = false;
                }
            }

            // Prioritize: Retakes at the top, then Carried-over missed subjects, then Core, then Electives
            return subjectList
                .OrderByDescending(s => s.IsRetake)
                .ThenByDescending(s => s.IsCarriedOver)
                .ThenBy(s => s.SubjectType == Database.AppDbContext.EnumSubjectType.Elective)
                .ThenBy(s => s.SemesterId)
                .ThenBy(s => s.SubjectCode)
                .ToList();
        }

        public async Task<List<StudentSubjectGradeItemModel>> GetPreviousSemesterGradesAsync(int? userId, int? newStudentAccId, string? rollNo, int semesterId, string? major)
        {
            var sem = await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.SemesterId == semesterId);
            var semName = sem?.SemesterName;
            int semSeq = sem?.Sequence ?? 1;

            var standardSubjects = await GetSemesterSubjectsByMajorAsync(semesterId, major);

            // Find all matching registration records for this user
            var regQuery = _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => r.IsDelete == false || r.IsDelete == null);

            if (userId.HasValue && userId.Value > 0)
            {
                regQuery = regQuery.Where(r => r.UserId == userId.Value);
            }
            else if (newStudentAccId.HasValue && newStudentAccId.Value > 0)
            {
                regQuery = regQuery.Where(r => r.NewStudentAccId == newStudentAccId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(rollNo))
            {
                regQuery = regQuery.Where(r => r.RollNo == rollNo.Trim());
            }
            else
            {
                return standardSubjects.Select(s => new StudentSubjectGradeItemModel
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    SemesterName = semName,
                    SubjectType = s.SubjectType,
                    Grade = "",
                    IsRetake = false
                }).ToList();
            }

            var studentRegistrations = await regQuery
                .Select(r => new { r.RegistrationId, r.AcademicYearLevel })
                .ToListAsync();
            var allRegIds = studentRegistrations.Select(r => r.RegistrationId).ToList();

            if (!allRegIds.Any())
            {
                return standardSubjects.Select(s => new StudentSubjectGradeItemModel
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    SemesterName = semName,
                    SubjectType = s.SubjectType,
                    Grade = "",
                    IsRetake = false
                }).ToList();
            }

            var allSemesters = await _context.Semesters.AsNoTracking().Where(s => s.IsDelete == false || s.IsDelete == null).ToListAsync();

            // Target specifically the registrations for this previous semester
            var targetSemesterRegs = studentRegistrations.Where(r =>
            {
                var level = (r.AcademicYearLevel ?? "").Trim();
                if (string.Equals(level, semName, StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrEmpty(semName) && level.Contains(semName, StringComparison.OrdinalIgnoreCase)) return true;
                var semObj = ResolveSemester(level, allSemesters);
                if (semObj != null && (semObj.SemesterId == semesterId || semObj.Sequence == semSeq)) return true;
                return false;
            }).ToList();

            var targetRegIds = targetSemesterRegs.Select(r => r.RegistrationId).ToList();

            // Fetch all student subject results across all registrations to check for prior failures
            var allStudentResults = await _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.RegistrationId.HasValue && allRegIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue)
                .ToListAsync();

            // Fetch all StudentSubjectResults recorded during this semester (including Retakes and Carried-overs)
            var previousResultsQuery = _context.StudentSubjectResults
                .AsNoTracking()
                .Include(r => r.Subject)
                    .ThenInclude(s => s.Semester)
                .Include(r => r.Subject)
                    .ThenInclude(s => s.Major)
                .Where(r => r.SubjectId.HasValue);

            if (targetRegIds.Any())
            {
                previousResultsQuery = previousResultsQuery.Where(r => r.RegistrationId.HasValue && targetRegIds.Contains(r.RegistrationId.Value));
            }
            else
            {
                previousResultsQuery = previousResultsQuery.Where(r => r.RegistrationId.HasValue && allRegIds.Contains(r.RegistrationId.Value) && (r.SemesterId == semesterId || (r.Subject != null && r.Subject.SemesterId == semesterId)));
            }

            var previousResults = await previousResultsQuery
                .OrderByDescending(r => r.ResultId)
                .ToListAsync();

            var latestResultsPerSubject = previousResults
                .Where(r => r.Subject != null)
                .GroupBy(r => r.SubjectId!.Value)
                .Select(g => g.First())
                .ToList();

            var list = new List<StudentSubjectGradeItemModel>();
            var addedSubjectIds = new HashSet<int>();

            // 1. Process all subjects with results recorded in this semester (including Retakes & Carried-overs)
            foreach (var res in latestResultsPerSubject)
            {
                var s = res.Subject!;
                bool isFromPastSemester = s.SemesterId != semesterId || (s.Semester != null && s.Semester.Sequence < semSeq);

                bool hadPriorFailure = isFromPastSemester && allStudentResults.Any(r =>
                    r.RegistrationId.HasValue &&
                    res.RegistrationId.HasValue &&
                    r.RegistrationId.Value < res.RegistrationId.Value &&
                    r.SubjectId == s.SubjectId &&
                    !string.IsNullOrEmpty(r.Grade) &&
                    (r.Grade == "D" || r.Grade == "F" || r.Grade == "Fail" || !r.IsPass));

                bool hasReexam = !string.IsNullOrWhiteSpace(res.ReexamGrade);
                string finalGrade = hasReexam ? res.ReexamGrade!.Trim() : (res.Grade ?? "");
                bool isFinalPass = hasReexam ? (res.ReexamIsPass == true) : (res.IsPass || IsGradePass(res.Grade));

                bool isRetake = hadPriorFailure && !isFinalPass;
                bool isCarryOver = isFromPastSemester && !hadPriorFailure && !isFinalPass;
                string itemSemName = (isRetake || isCarryOver)
                    ? (s.Semester?.SemesterName ?? $"Semester {s.SemesterId}")
                    : (s.Semester?.SemesterName ?? semName ?? "");

                list.Add(new StudentSubjectGradeItemModel
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    SemesterName = itemSemName,
                    SubjectType = s.SubjectType,
                    Grade = finalGrade,
                    ReexamGrade = res.ReexamGrade,
                    ReexamIsPass = res.ReexamIsPass,
                    ResultId = res.ResultId,
                    IsPass = isFinalPass,
                    IsRetake = isRetake,
                    IsCarriedOver = isCarryOver,
                    IsReexam = hasReexam
                });
                addedSubjectIds.Add(s.SubjectId);
            }

            // 2. If any standard Core subjects for this semester were not recorded, add them with empty grade ("မဖြေဆိုထားပါ")
            foreach (var s in standardSubjects)
            {
                if (!addedSubjectIds.Contains(s.SubjectId) && s.SubjectType != EnumSubjectType.Elective)
                {
                    list.Add(new StudentSubjectGradeItemModel
                    {
                        SubjectId = s.SubjectId,
                        SubjectCode = s.SubjectCode,
                        SubjectName = s.SubjectName,
                        SemesterName = semName,
                        SubjectType = s.SubjectType,
                        Grade = "",
                        ResultId = null,
                        IsPass = false,
                        IsRetake = false,
                        IsCarriedOver = false
                    });
                    addedSubjectIds.Add(s.SubjectId);
                }
            }

            return list.OrderBy(x => x.IsRetake ? 0 : (x.IsCarriedOver ? 1 : 2)).ThenBy(x => x.SubjectType).ThenBy(x => x.SubjectCode).ToList();
        }

        public async Task<int> GetMaxRetakeLimitAsync()
        {
            var setting = await _context.SystemSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SettingKey == "MaxRetakeLimit");

            if (setting != null && int.TryParse(setting.SettingValue, out int limit) && limit > 0)
            {
                return limit;
            }
            return 25; // Default fallback
        }

        public async Task<bool> UpdateMaxRetakeLimitAsync(int newLimit, string? updatedBy = "Admin")
        {
            if (newLimit <= 0) return false;

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == "MaxRetakeLimit");

            if (setting != null)
            {
                setting.SettingValue = newLimit.ToString();
                setting.UpdatedDateTime = DateTime.Now;
                setting.UpdatedBy = updatedBy;
            }
            else
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    SettingKey = "MaxRetakeLimit",
                    SettingValue = newLimit.ToString(),
                    Description = "Maximum allowed retakes across all semesters",
                    UpdatedDateTime = DateTime.Now,
                    UpdatedBy = updatedBy
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<StudentRetakeStatusModel> GetStudentRetakeStatusAsync(int? userId, int? studentId = null, string? rollNo = null)
        {
            int maxLimit = await GetMaxRetakeLimitAsync();

            var regQuery = _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => r.IsDelete == false || r.IsDelete == null);

            if (userId.HasValue && userId.Value > 0)
                regQuery = regQuery.Where(r => r.UserId == userId.Value);
            else if (studentId.HasValue && studentId.Value > 0)
            {
                var st = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
                if (st?.UserId != null)
                    regQuery = regQuery.Where(r => r.UserId == st.UserId);
                else if (!string.IsNullOrWhiteSpace(st?.CurrentRollNo))
                    regQuery = regQuery.Where(r => r.RollNo == st.CurrentRollNo.Trim());
            }
            else if (!string.IsNullOrWhiteSpace(rollNo))
                regQuery = regQuery.Where(r => r.RollNo == rollNo.Trim());

            var regList = await regQuery
                .OrderBy(r => r.RegistrationId)
                .Select(r => new { r.RegistrationId, r.AcademicYearLevel })
                .ToListAsync();

            if (!regList.Any())
            {
                return new StudentRetakeStatusModel
                {
                    TotalRetakesTaken = 0,
                    MaxRetakeLimit = maxLimit,
                    RemainingRetakes = maxLimit,
                    UsagePercentage = 0,
                    DangerLevel = "Safe",
                    DangerStatusText = "သတ်မှတ်ချက်အတွင်း ရှိနေပါသည် (အခြေအနေ ကောင်းမွန်ပါသည်)",
                    BadgeColor = "#10b981",
                    IsDisqualified = false,
                    FailedSubjectsCount = 0
                };
            }

            var regIds = regList.Select(r => r.RegistrationId).ToList();
            var regSemLevelMap = regList.ToDictionary(r => r.RegistrationId, r => r.AcademicYearLevel);

            // Fast lightweight projection: 0 image columns loaded!
            var allResults = await _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue)
                .OrderBy(r => r.ResultId)
                .Select(r => new
                {
                    r.ResultId,
                    r.SubjectId,
                    r.SemesterId,
                    r.RegistrationId,
                    r.IsPass,
                    r.ReexamIsPass,
                    r.Grade,
                    r.ReexamGrade,
                    r.IsDisqualified
                })
                .ToListAsync();

            var allSemesters = await _context.Semesters
                .AsNoTracking()
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .ToListAsync();

            var allSubjects = await _context.Subjects
                .AsNoTracking()
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .Select(s => new
                {
                    s.SubjectId,
                    s.SubjectName,
                    s.SubjectCode,
                    s.SemesterId,
                    Sequence = s.Semester != null ? s.Semester.Sequence : 0
                })
                .ToListAsync();

            var subMap = allSubjects.ToDictionary(s => s.SubjectId);

            int retakesCount = 0;
            var retakeHistory = new List<string>();

            // Group by subject to track Retake attempts (only counted if student had a prior unresolved failure)
            var subjectAttempts = allResults
                .Where(r => r.SubjectId.HasValue && subMap.ContainsKey(r.SubjectId.Value))
                .GroupBy(r => r.SubjectId!.Value);

            foreach (var group in subjectAttempts)
            {
                var attempts = group.OrderBy(r => r.ResultId).ToList();
                var sub = subMap[group.Key];

                int retakeAttemptsCount = 0;
                bool hadPriorFailure = false;

                foreach (var att in attempts)
                {
                    if (hadPriorFailure)
                    {
                        retakeAttemptsCount++;
                    }

                    bool isAttemptPassed = att.IsPass || (att.ReexamIsPass == true) || IsGradePass(att.Grade);
                    if (!isAttemptPassed && !string.IsNullOrEmpty(att.Grade) && (att.Grade == "D" || att.Grade == "F" || att.Grade == "Fail" || att.ReexamGrade == "D" || att.ReexamGrade == "F"))
                    {
                        hadPriorFailure = true;
                    }
                    else if (isAttemptPassed)
                    {
                        hadPriorFailure = false;
                    }
                }

                if (retakeAttemptsCount > 0)
                {
                    retakesCount += retakeAttemptsCount;
                    retakeHistory.Add($"{sub.SubjectName} ({sub.SubjectCode}) - {retakeAttemptsCount} ကြိမ် Retake ဖြင့် ဖြေဆိုခဲ့သည်");
                }
            }

            var latestPerSubject = allResults
                .Where(r => r.SubjectId.HasValue)
                .GroupBy(r => r.SubjectId!.Value)
                .Select(g => g.Last())
                .ToList();

            int currentFailedCount = latestPerSubject.Count(r =>
            {
                bool isPassed = r.IsPass || (r.ReexamIsPass == true) || IsGradePass(r.ReexamGrade) || IsGradePass(r.Grade);
                return !isPassed;
            });

            int remaining = Math.Max(0, maxLimit - retakesCount);
            double usage = maxLimit > 0 ? ((double)retakesCount / maxLimit) * 100.0 : 0;
            bool isDisqualified = retakesCount >= maxLimit;

            string dangerLevel = "Safe";
            string badgeColor = "#10b981";
            string dangerText = "သတ်မှတ်ချက်အတွင်း ရှိနေပါသည် (အခြေအနေ ကောင်းမွန်ပါသည်)";

            if (isDisqualified)
            {
                dangerLevel = "Disqualified";
                badgeColor = "#ef4444";
                dangerText = $"ကျောင်းတက်ရောက်ခွင့် အရည်အချင်းမပြည့်မီတော့ပါ (သတ်မှတ်ထားသော အများဆုံး Retake {maxLimit} ကြိမ် ပြည့်သွားပါပြီ)";
            }
            else if (usage >= 80.0)
            {
                dangerLevel = "HighDanger";
                badgeColor = "#f97316";
                dangerText = "အလွန်အန္တရာယ်ရှိသောအဆင့် (နောက်ထပ် Retake ယူရပါက ကျောင်းထုတ်ခံရနိုင်ပါသည်)";
            }
            else if (usage >= 60.0)
            {
                dangerLevel = "Warning";
                badgeColor = "#f59e0b";
                dangerText = "သတိပြုရန်အဆင့် (Retake အကြိမ်အရေအတွက် များပြားလာနေပါသည်)";
            }

            return new StudentRetakeStatusModel
            {
                TotalRetakesTaken = retakesCount,
                MaxRetakeLimit = maxLimit,
                RemainingRetakes = remaining,
                UsagePercentage = Math.Round(usage, 1),
                DangerLevel = dangerLevel,
                DangerStatusText = dangerText,
                BadgeColor = badgeColor,
                IsDisqualified = isDisqualified,
                FailedSubjectsCount = currentFailedCount,
                RetakeHistoryList = retakeHistory
            };
        }

        public async Task<StudentSubjectSelectionResponseModel> GetStudentSubjectSelectionsAsync(int? userId, int? newStudentAccId = null)
        {
            var response = new StudentSubjectSelectionResponseModel
            {
                UserId = userId ?? 0,
                NewStudentAccId = newStudentAccId
            };

            // 1. Resolve student info
            Student? student = null;
            if (userId.HasValue && userId.Value > 0)
            {
                student = await _context.Students
                    .AsNoTracking()
                    .Include(s => s.User)
                    .Include(s => s.Faculty)
                    .FirstOrDefaultAsync(s => s.UserId == userId.Value || s.StudentId == userId.Value);
            }

            if (student == null && newStudentAccId.HasValue && newStudentAccId.Value > 0)
            {
                var regWithAcc = await _context.StudentRegistrations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.NewStudentAccId == newStudentAccId.Value && (r.IsDelete == false || r.IsDelete == null));
                if (regWithAcc?.UserId != null)
                {
                    student = await _context.Students
                        .AsNoTracking()
                        .Include(s => s.User)
                        .Include(s => s.Faculty)
                        .FirstOrDefaultAsync(s => s.UserId == regWithAcc.UserId.Value);
                }
            }

            int? actualStudentId = student?.StudentId;
            int? actualUserId = student?.UserId ?? (userId.HasValue && userId.Value > 0 ? userId.Value : null);
            int? actualAccId = newStudentAccId.HasValue && newStudentAccId.Value > 0 ? newStudentAccId.Value : null;

            var personalInfo = await _context.StudentPersonalInfos
                .AsNoTracking()
                .Where(p => (actualUserId.HasValue && p.UserId == actualUserId.Value) ||
                            (actualAccId.HasValue && p.NewStudentAccId == actualAccId.Value))
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.roll_no,
                    p.student_name_mm,
                    p.student_name_en,
                    p.major
                })
                .FirstOrDefaultAsync();

            var realMubRoll = !string.IsNullOrWhiteSpace(student?.User?.RoleNo)
                ? student.User.RoleNo
                : (!string.IsNullOrWhiteSpace(personalInfo?.roll_no)
                    ? personalInfo.roll_no
                    : (!string.IsNullOrWhiteSpace(student?.CurrentRollNo) ? student.CurrentRollNo : string.Empty));

                response.StudentId = actualStudentId;
                response.StudentNameMm = !string.IsNullOrWhiteSpace(personalInfo?.student_name_mm) 
                    ? personalInfo.student_name_mm 
                    : (student?.User?.FullName ?? student?.StudentName ?? string.Empty);
                response.StudentNameEn = !string.IsNullOrWhiteSpace(personalInfo?.student_name_en)
                    ? personalInfo.student_name_en
                    : (student?.StudentName ?? string.Empty);
                response.RollNo = realMubRoll;
                response.FacultyName = student?.Faculty?.FacultyName ?? student?.User?.Faculty?.FacultyName ?? "Faculty of Computing";

                // 2. Query all registrations for this student (Lightweight: 0 images loaded)
                var regQuery = _context.StudentRegistrations
                    .AsNoTracking()
                    .Where(r => r.IsDelete == false || r.IsDelete == null);

                if (actualUserId.HasValue && actualUserId.Value > 0)
                {
                    regQuery = regQuery.Where(r => r.UserId == actualUserId.Value || (actualAccId.HasValue && r.NewStudentAccId == actualAccId.Value));
                }
                else if (actualAccId.HasValue && actualAccId.Value > 0)
                {
                    regQuery = regQuery.Where(r => r.NewStudentAccId == actualAccId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(response.RollNo))
                {
                    regQuery = regQuery.Where(r => r.RollNo == response.RollNo.Trim());
                }

                var registrations = await regQuery
                    .OrderByDescending(r => r.RegistrationId)
                    .Select(r => new
                    {
                        r.RegistrationId,
                        r.UserId,
                        r.RollNo,
                        r.StudentNameMm,
                        r.StudentNameEn,
                        r.Major,
                        r.AcademicYearLevel,
                        r.ApplicationDate,
                        r.CreatedDatetime,
                        r.Status
                    })
                    .ToListAsync();

                if (registrations.Any())
                {
                    var latest = registrations.First();
                    if (string.IsNullOrEmpty(response.StudentNameMm)) response.StudentNameMm = latest.StudentNameMm;
                    if (string.IsNullOrEmpty(response.StudentNameEn)) response.StudentNameEn = latest.StudentNameEn;
                    if (string.IsNullOrEmpty(response.RollNo)) response.RollNo = latest.RollNo ?? string.Empty;

                    response.CurrentMajor = !string.IsNullOrWhiteSpace(personalInfo?.major) && personalInfo.major != "N/A"
                        ? personalInfo.major
                        : (!string.IsNullOrWhiteSpace(latest.Major) && latest.Major != "N/A"
                            ? latest.Major
                            : (student?.CurrentMajor ?? string.Empty));
                }
                else if (personalInfo != null && !string.IsNullOrWhiteSpace(personalInfo.major))
                {
                    response.CurrentMajor = personalInfo.major;
                }
                else if (student != null && !string.IsNullOrWhiteSpace(student.CurrentMajor))
                {
                    response.CurrentMajor = student.CurrentMajor;
                }

                var allSemesters = await _context.Semesters
                    .AsNoTracking()
                    .Where(s => s.IsDelete == false || s.IsDelete == null)
                    .OrderBy(s => s.Sequence)
                    .ToListAsync();

                var allCurriculumSubjects = await _context.Subjects
                    .AsNoTracking()
                    .Include(s => s.Major)
                    .Include(s => s.Semester)
                    .Where(s => s.IsDelete == false || s.IsDelete == null)
                    .ToListAsync();

                var subMap = allCurriculumSubjects.ToDictionary(s => s.SubjectId);
                var regLevelMap = registrations.ToDictionary(r => r.RegistrationId, r => r.AcademicYearLevel ?? string.Empty);

                List<SubjectModel> GetFilteredCurriculumSubjectsInMemory(int semId, string? major)
                {
                    var matchedSem = allSemesters.FirstOrDefault(s => s.SemesterId == semId);
                    int semesterSeq = matchedSem?.Sequence ?? 1;
                    var majorName = (major ?? "").Trim();
                    bool isCS = majorName.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CS", StringComparison.OrdinalIgnoreCase);
                    bool isCT = majorName.Contains("Computer Technology", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CT", StringComparison.OrdinalIgnoreCase);

                    var semSubs = allCurriculumSubjects.Where(s => s.SemesterId == semId).ToList();
                    List<Subject> filtered;

                    if (semesterSeq <= 3)
                    {
                        filtered = semSubs;
                    }
                    else
                    {
                        filtered = semSubs.Where(s =>
                        {
                            var code = (s.SubjectCode ?? "").Trim().ToUpper();
                            var subMajorName = s.Major?.MajorName ?? "";
                            bool isCommon = code.StartsWith("CST-") || code.StartsWith("E-") || code.StartsWith("P-") || code.StartsWith("M-") ||
                                            s.MajorId == null || subMajorName == "Information Technology";

                            if (isCS)
                            {
                                return isCommon || code.StartsWith("CS-") || subMajorName == "Computer Science";
                            }
                            else if (isCT)
                            {
                                return isCommon || code.StartsWith("CT-") || subMajorName == "Computer Technology";
                            }
                            else if (!string.IsNullOrEmpty(majorName))
                            {
                                return isCommon || subMajorName.Equals(majorName, StringComparison.OrdinalIgnoreCase);
                            }

                            return true;
                        }).ToList();
                    }

                    return filtered
                        .OrderBy(s => s.SubjectType)
                        .ThenBy(s => s.SubjectCode)
                        .Select(s => new SubjectModel
                        {
                            SubjectId = s.SubjectId,
                            SubjectName = s.SubjectName,
                            SubjectCode = s.SubjectCode,
                            Credit = s.Credit,
                            SubjectType = s.SubjectType,
                            SemesterId = s.SemesterId,
                            SemesterName = s.Semester?.SemesterName,
                            MajorId = s.MajorId,
                            MajorName = s.Major?.MajorName,
                            IsRetake = false
                        }).ToList();
                }

                var regIds = registrations.Select(r => r.RegistrationId).ToList();

                // 3. Query all StudentSubjectResults (Lightweight: 0 images loaded)
                var allResults = await _context.StudentSubjectResults
                    .AsNoTracking()
                    .Where(r => (r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value)) ||
                                (actualStudentId.HasValue && r.StudentId == actualStudentId.Value))
                    .Select(r => new
                    {
                        r.ResultId,
                        r.SubjectId,
                        r.SemesterId,
                        r.RegistrationId,
                        r.StudentId,
                        r.IsPass,
                        r.ReexamIsPass,
                        r.Grade,
                        r.ReexamGrade,
                        r.MarksObtained,
                        r.IsDisqualified
                    })
                    .ToListAsync();

                // Process each registration
                foreach (var reg in registrations)
                {
                    var semesterSelection = new StudentSemesterSubjectSelectionModel
                    {
                        RegistrationId = reg.RegistrationId,
                        AcademicYearLevel = reg.AcademicYearLevel ?? string.Empty,
                        Major = reg.Major ?? string.Empty,
                        FacultyName = response.FacultyName,
                        RollNo = reg.RollNo ?? response.RollNo,
                        RegistrationStatus = reg.Status ?? "Confirmed",
                        RegistrationDate = reg.CreatedDatetime ?? reg.ApplicationDate
                    };

                    // Resolve Semester accurately
                    Semester? matchedSemester = ResolveSemester(reg.AcademicYearLevel, allSemesters) ?? allSemesters.FirstOrDefault();

                    int semesterId = matchedSemester?.SemesterId ?? 0;
                    semesterSelection.SemesterId = semesterId;
                    semesterSelection.SemesterName = matchedSemester?.SemesterName ?? reg.AcademicYearLevel ?? string.Empty;

                    // 4. Query available curriculum subjects strictly for THIS Semester and Major (in-memory fast lookup)
                    var availableSubjects = GetFilteredCurriculumSubjectsInMemory(semesterId, reg.Major);

                    // 5. Get Selected Results for this registration
                    var regResults = allResults
                        .Where(r => r.RegistrationId == reg.RegistrationId && r.SubjectId.HasValue && subMap.ContainsKey(r.SubjectId.Value))
                        .ToList();

                    if (!regResults.Any() && actualStudentId.HasValue)
                    {
                        regResults = allResults
                            .Where(r => r.StudentId == actualStudentId.Value && r.SemesterId == semesterId && r.SubjectId.HasValue && subMap.ContainsKey(r.SubjectId.Value))
                            .ToList();
                    }

                    var selectedSubjectIds = new HashSet<int>();

                    foreach (var res in regResults)
                    {
                        var sub = subMap[res.SubjectId!.Value];
                        selectedSubjectIds.Add(sub.SubjectId);

                        int subOriginalSemSeq = sub.Semester?.Sequence ?? 0;
                        if (subOriginalSemSeq == 0 && sub.SemesterId > 0)
                        {
                            var origSem = allSemesters.FirstOrDefault(s => s.SemesterId == sub.SemesterId);
                            subOriginalSemSeq = origSem?.Sequence ?? 0;
                        }
                        int currentSemSeq = matchedSemester?.Sequence ?? 0;

                        bool isFromPastSemester = subOriginalSemSeq > 0 && currentSemSeq > 0 && subOriginalSemSeq < currentSemSeq;

                        // Did the student take this past-semester subject in a prior registration and fail it?
                        bool hadPriorFailure = isFromPastSemester && allResults.Any(r =>
                            r.RegistrationId.HasValue && r.RegistrationId.Value != reg.RegistrationId &&
                            r.SubjectId == sub.SubjectId &&
                            !string.IsNullOrEmpty(r.Grade) && (r.Grade == "D" || r.Grade == "F" || r.Grade == "Fail") &&
                            !r.IsPass);

                        bool isPassedInThisOrPrior = res.IsPass || IsGradePass(res.Grade) || res.ReexamIsPass == true || IsGradePass(res.ReexamGrade) ||
                            allResults.Any(r => r.SubjectId == sub.SubjectId && (r.IsPass || IsGradePass(r.Grade) || r.ReexamIsPass == true || IsGradePass(r.ReexamGrade)));

                        bool isRetake = hadPriorFailure && !isPassedInThisOrPrior;
                        bool isCarryOver = isFromPastSemester && !hadPriorFailure && !isPassedInThisOrPrior;

                        string typeLabel = sub.SubjectType == EnumSubjectType.Elective ? "Elective" : "Core";

                        bool isDisqualified = res.IsDisqualified;
                        string status = res.IsPass ? "Passed" : (isDisqualified ? "Disqualified" : (!string.IsNullOrEmpty(res.Grade) ? "Failed" : "Enrolled"));

                        string resSemName = semesterSelection.SemesterName;
                        if (res.SemesterId.HasValue && res.SemesterId.Value > 0)
                        {
                            var matchingSem = allSemesters.FirstOrDefault(s => s.SemesterId == res.SemesterId.Value);
                            if (matchingSem != null) resSemName = matchingSem.SemesterName;
                        }

                        semesterSelection.SelectedSubjects.Add(new StudentSubjectItemModel
                        {
                            SubjectId = sub.SubjectId,
                            SubjectCode = sub.SubjectCode ?? string.Empty,
                            SubjectName = sub.SubjectName ?? string.Empty,
                            Credit = sub.Credit > 0 ? sub.Credit : 3,
                            SubjectType = typeLabel,
                            SemesterName = resSemName,
                            MajorName = sub.Major?.MajorName ?? reg.Major,
                            Grade = res.Grade,
                            IsPass = res.IsPass,
                            ReexamGrade = res.ReexamGrade,
                            ReexamIsPass = res.ReexamIsPass,
                            IsSubjectDisqualified = isDisqualified,
                            IsRetake = isRetake,
                            IsCarriedOver = isCarryOver,
                            Status = status
                        });
                    }

                    // 6. Calculate Not Selected Subjects
                    foreach (var avail in availableSubjects)
                    {
                        if (!selectedSubjectIds.Contains(avail.SubjectId))
                        {
                            bool isElective = avail.SubjectType == EnumSubjectType.Elective;
                            int currentRegSeq = matchedSemester?.Sequence ?? 0;

                            var laterEnrollment = allResults.FirstOrDefault(r =>
                                r.SubjectId == avail.SubjectId &&
                                r.RegistrationId.HasValue &&
                                r.RegistrationId.Value != reg.RegistrationId &&
                                regLevelMap.TryGetValue(r.RegistrationId.Value, out var lvl) &&
                                (ResolveSemester(lvl, allSemesters)?.Sequence ?? 0) > currentRegSeq);

                            bool isEnrolledLater = laterEnrollment != null;
                            string? laterSemesterName = null;

                            if (isEnrolledLater && laterEnrollment != null && laterEnrollment.RegistrationId.HasValue)
                            {
                                regLevelMap.TryGetValue(laterEnrollment.RegistrationId.Value, out var laterLvl);
                                var laterMatchedSem = ResolveSemester(laterLvl, allSemesters);
                                laterSemesterName = laterMatchedSem?.SemesterName ?? laterLvl;
                            }

                            string reason;
                            bool isRequired;

                            if (isElective)
                            {
                                reason = "စိတ်ကြိုက်ရွေးချယ်ခွင့် (ရွေးချယ်ရန်မလိုပါ)";
                                isRequired = false;
                            }
                            else if (isEnrolledLater)
                            {
                                reason = $"{laterSemesterName} တွင် ရွေးချယ်ပြီး";
                                isRequired = false;
                            }
                            else
                            {
                                reason = "မဖြစ်မနေ ရွေးချယ်ရန် လိုအပ်သည်";
                                isRequired = true;
                            }

                            semesterSelection.UnselectedSubjects.Add(new StudentSubjectItemModel
                            {
                                SubjectId = avail.SubjectId,
                                SubjectCode = avail.SubjectCode ?? string.Empty,
                                SubjectName = avail.SubjectName ?? string.Empty,
                                Credit = avail.Credit > 0 ? avail.Credit : 3,
                                SubjectType = avail.SubjectType == EnumSubjectType.Elective ? "Elective" : "Core",
                                SemesterName = semesterSelection.SemesterName,
                                MajorName = avail.MajorName ?? reg.Major,
                                Status = "Not Enrolled",
                                Reason = reason,
                                IsRequired = isRequired,
                                IsEnrolledLater = isEnrolledLater,
                                EnrolledLaterSemester = laterSemesterName
                            });
                        }
                    }

                    semesterSelection.TotalSelectedCredits = semesterSelection.SelectedSubjects.Sum(s => s.Credit);
                    semesterSelection.TotalEarnedCredits = semesterSelection.SelectedSubjects.Where(s => s.IsPass == true || s.ReexamIsPass == true).Sum(s => s.Credit);
                    semesterSelection.TotalUnselectedCredits = semesterSelection.UnselectedSubjects.Sum(s => s.Credit);

                    response.SemesterSelections.Add(semesterSelection);
                }

                // Distinct passed subject credits
                var distinctPassedCredits = new Dictionary<int, int>();
                foreach (var sem in response.SemesterSelections)
                {
                    foreach (var s in sem.SelectedSubjects)
                    {
                        if ((s.IsPass == true || s.ReexamIsPass == true) && s.SubjectId > 0 && !distinctPassedCredits.ContainsKey(s.SubjectId))
                        {
                            distinctPassedCredits[s.SubjectId] = s.Credit;
                        }
                    }
                }
                response.TotalEarnedCredits = distinctPassedCredits.Values.Sum();

                // Distinct unselected core subjects
                var pendingRequiredCoreSubs = response.SemesterSelections
                    .SelectMany(s => s.UnselectedSubjects)
                    .Where(u => u.IsRequired && !u.IsEnrolledLater)
                    .GroupBy(u => u.SubjectId)
                    .Select(g => g.First())
                    .ToList();

                response.TotalPendingRequiredSubjects = pendingRequiredCoreSubs.Count;
                response.TotalPendingRequiredCredits = pendingRequiredCoreSubs.Sum(s => s.Credit);

                return response;
            }

        public async Task<StudentGraduationStatusModel> GetStudentGraduationStatusAsync(int? userId, int? studentId = null, string? rollNo = null, int? newStudentAccId = null)
        {
            var graduationModel = new StudentGraduationStatusModel
            {
                TargetGraduationCredits = 155,
                TargetSem1To7Credits = 143,
                TargetSem8Credits = 12
            };

            var regQuery = _context.StudentRegistrations
                .AsNoTracking()
                .Where(r => r.IsDelete == false || r.IsDelete == null);

            if (!string.IsNullOrWhiteSpace(rollNo))
            {
                var cleanRoll = rollNo.Trim();
                if (userId.HasValue && userId.Value > 0)
                {
                    regQuery = regQuery.Where(r => r.RollNo == cleanRoll || r.UserId == userId.Value);
                }
                else
                {
                    regQuery = regQuery.Where(r => r.RollNo == cleanRoll);
                }
            }
            else if (userId.HasValue && userId.Value > 0)
                regQuery = regQuery.Where(r => r.UserId == userId.Value);
            else if (newStudentAccId.HasValue && newStudentAccId.Value > 0)
                regQuery = regQuery.Where(r => r.NewStudentAccId == newStudentAccId.Value);
            else if (studentId.HasValue && studentId.Value > 0)
            {
                var st = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == studentId.Value);
                if (st?.UserId != null && st.UserId > 0)
                    regQuery = regQuery.Where(r => r.UserId == st.UserId || (!string.IsNullOrWhiteSpace(st.CurrentRollNo) && r.RollNo == st.CurrentRollNo.Trim()));
                else if (!string.IsNullOrWhiteSpace(st?.CurrentRollNo))
                    regQuery = regQuery.Where(r => r.RollNo == st.CurrentRollNo.Trim());
            }

            var registrations = await regQuery
                .OrderBy(r => r.RegistrationId)
                .Select(r => new
                {
                    r.RegistrationId,
                    r.UserId,
                    r.NewStudentAccId,
                    r.RollNo,
                    r.StudentNameEn,
                    r.StudentNameMm,
                    r.Major,
                    r.AcademicYearLevel,
                    r.Status,
                    r.CreatedDatetime,
                    r.ApplicationDate
                })
                .ToListAsync();

            if (!registrations.Any())
            {
                graduationModel.GraduationStatus = "Studying";
                graduationModel.GraduationStatusText = "ပညာသင်ယူဆဲ (In Progress - 0/155 Credits)";
                return graduationModel;
            }

            var regIds = registrations.Select(r => r.RegistrationId).ToList();
            var firstReg = registrations.First();
            graduationModel.UserId = firstReg.UserId;
            graduationModel.NewStudentAccId = firstReg.NewStudentAccId;
            graduationModel.RollNo = firstReg.RollNo ?? string.Empty;
            graduationModel.MajorName = firstReg.Major ?? string.Empty;

            // Look up updated StudentPersonalInfo and Student master records
            var effectiveUserId = firstReg.UserId ?? (userId.HasValue && userId.Value > 0 ? userId.Value : (int?)null);
            var effectiveRoll = !string.IsNullOrWhiteSpace(firstReg.RollNo) ? firstReg.RollNo.Trim() : (!string.IsNullOrWhiteSpace(rollNo) ? rollNo.Trim() : null);

            var pInfo = (effectiveUserId.HasValue && effectiveUserId.Value > 0)
                ? await _context.StudentPersonalInfos
                    .AsNoTracking()
                    .Where(p => p.UserId == effectiveUserId.Value)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new { p.student_name_en, p.student_name_mm, p.roll_no, p.major })
                    .FirstOrDefaultAsync()
                : (!string.IsNullOrWhiteSpace(effectiveRoll)
                    ? await _context.StudentPersonalInfos
                        .AsNoTracking()
                        .Where(p => p.roll_no == effectiveRoll)
                        .OrderByDescending(p => p.Id)
                        .Select(p => new { p.student_name_en, p.student_name_mm, p.roll_no, p.major })
                        .FirstOrDefaultAsync()
                    : null);

            var stu = (effectiveUserId.HasValue && effectiveUserId.Value > 0)
                ? await _context.Students.AsNoTracking().Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == effectiveUserId.Value && (s.IsDelete == false || s.IsDelete == null))
                : (!string.IsNullOrWhiteSpace(effectiveRoll)
                    ? await _context.Students.AsNoTracking().Include(s => s.User).FirstOrDefaultAsync(s => s.CurrentRollNo == effectiveRoll && (s.IsDelete == false || s.IsDelete == null))
                    : null);

            var resolvedName = (pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_en) ? pInfo.student_name_en.Trim() : null)
                            ?? (pInfo != null && !string.IsNullOrWhiteSpace(pInfo.student_name_mm) ? pInfo.student_name_mm.Trim() : null)
                            ?? (!string.IsNullOrWhiteSpace(stu?.StudentName) ? stu.StudentName.Trim() : null)
                            ?? (!string.IsNullOrWhiteSpace(stu?.User?.FullName) ? stu.User.FullName.Trim() : null)
                            ?? (!string.IsNullOrEmpty(firstReg.StudentNameEn) ? firstReg.StudentNameEn.Trim() : null)
                            ?? (!string.IsNullOrEmpty(firstReg.StudentNameMm) ? firstReg.StudentNameMm.Trim() : "");

            graduationModel.StudentName = resolvedName;
            if (stu != null && !string.IsNullOrWhiteSpace(stu.CurrentRollNo)) graduationModel.RollNo = stu.CurrentRollNo.Trim();
            else if (pInfo != null && !string.IsNullOrWhiteSpace(pInfo.roll_no)) graduationModel.RollNo = pInfo.roll_no.Trim();

            if (stu != null && !string.IsNullOrWhiteSpace(stu.CurrentMajor)) graduationModel.MajorName = stu.CurrentMajor.Trim();
            else if (pInfo != null && !string.IsNullOrWhiteSpace(pInfo.major)) graduationModel.MajorName = pInfo.major.Trim();

            // --- Dynamic Target Credits Calculation based on Curriculum Core Subjects + Max Electives per Semester ---
            string studentMajor = firstReg.Major ?? graduationModel.MajorName ?? string.Empty;
            int totalRequiredSemesters = 8;
            if (studentMajor.Contains("Civil", StringComparison.OrdinalIgnoreCase) ||
                studentMajor.Contains("Electronic", StringComparison.OrdinalIgnoreCase) ||
                studentMajor.Contains("Electrical", StringComparison.OrdinalIgnoreCase) ||
                studentMajor.Contains("Mechanical", StringComparison.OrdinalIgnoreCase) ||
                studentMajor.Contains("Engineering", StringComparison.OrdinalIgnoreCase))
            {
                totalRequiredSemesters = 9;
            }

            bool isCS = studentMajor.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) || studentMajor.Equals("CS", StringComparison.OrdinalIgnoreCase);
            bool isCT = studentMajor.Contains("Computer Technology", StringComparison.OrdinalIgnoreCase) || studentMajor.Equals("CT", StringComparison.OrdinalIgnoreCase);

            var allSemestersList = await _context.Semesters
                .AsNoTracking()
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .OrderBy(s => s.Sequence ?? s.SemesterId)
                .ToListAsync();

            var allCurriculumSubjects = await _context.Subjects
                .AsNoTracking()
                .Include(s => s.Major)
                .Include(s => s.Semester)
                .Where(s => s.IsDelete == false || s.IsDelete == null)
                .ToListAsync();

            List<SubjectModel> GetFilteredCurriculumSubjectsInMemory(int semId, string? major)
            {
                var matchedSem = allSemestersList.FirstOrDefault(s => s.SemesterId == semId);
                int semesterSeq = matchedSem?.Sequence ?? 1;
                var majorName = (major ?? "").Trim();
                bool isCS = majorName.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CS", StringComparison.OrdinalIgnoreCase);
                bool isCT = majorName.Contains("Computer Technology", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CT", StringComparison.OrdinalIgnoreCase);

                var semSubs = allCurriculumSubjects.Where(s => s.SemesterId == semId).ToList();
                List<Subject> filtered;

                if (semesterSeq <= 3)
                {
                    filtered = semSubs;
                }
                else
                {
                    filtered = semSubs.Where(s =>
                    {
                        var code = (s.SubjectCode ?? "").Trim().ToUpper();
                        var subMajorName = s.Major?.MajorName ?? "";
                        bool isCommon = code.StartsWith("CST-") || code.StartsWith("E-") || code.StartsWith("P-") || code.StartsWith("M-") ||
                                        s.MajorId == null || subMajorName == "Information Technology";

                        if (isCS)
                        {
                            return isCommon || code.StartsWith("CS-") || subMajorName == "Computer Science";
                        }
                        else if (isCT)
                        {
                            return isCommon || code.StartsWith("CT-") || subMajorName == "Computer Technology";
                        }
                        else if (!string.IsNullOrEmpty(majorName))
                        {
                            return isCommon || subMajorName.Equals(majorName, StringComparison.OrdinalIgnoreCase);
                        }

                        return true;
                    }).ToList();
                }

                return filtered
                    .OrderBy(s => s.SubjectType)
                    .ThenBy(s => s.SubjectCode)
                    .Select(s => new SubjectModel
                    {
                        SubjectId = s.SubjectId,
                        SubjectName = s.SubjectName,
                        SubjectCode = s.SubjectCode,
                        Credit = s.Credit,
                        SubjectType = s.SubjectType,
                        SemesterId = s.SemesterId,
                        SemesterName = s.Semester?.SemesterName,
                        MajorId = s.MajorId,
                        MajorName = s.Major?.MajorName,
                        IsRetake = false
                    }).ToList();
            }

            var requiredSemesters = allSemestersList
                .Where(s => (s.Sequence ?? s.SemesterId) <= totalRequiredSemesters)
                .ToList();

            string effectiveMajor = studentMajor;
            if (string.IsNullOrEmpty(effectiveMajor) || effectiveMajor.Equals("CST", StringComparison.OrdinalIgnoreCase))
            {
                effectiveMajor = "Computer Science"; // Default reference curriculum track for CST foundation students
            }
            bool isEffCS = effectiveMajor.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) || effectiveMajor.Equals("CS", StringComparison.OrdinalIgnoreCase);
            bool isEffCT = effectiveMajor.Contains("Computer Technology", StringComparison.OrdinalIgnoreCase) || effectiveMajor.Equals("CT", StringComparison.OrdinalIgnoreCase);

            int calcTargetGraduationCredits = 0;
            int calcTargetAcademicCredits = 0;
            int calcTargetFinalCredits = 0;

            foreach (var sem in requiredSemesters)
            {
                int seq = sem.Sequence ?? sem.SemesterId;
                var semSubjects = GetFilteredCurriculumSubjectsInMemory(sem.SemesterId, effectiveMajor);

                var coreSubs = semSubjects.Where(s => s.SubjectType != EnumSubjectType.Elective).ToList();
                var electiveSubs = semSubjects.Where(s => s.SubjectType == EnumSubjectType.Elective).ToList();

                int coreCredits = coreSubs.Sum(s => s.Credit > 0 ? s.Credit : 3);

                int maxElectiveCount = isEffCS ? (sem.MaxElectiveCS ?? 0) : (isEffCT ? (sem.MaxElectiveCT ?? 0) : (sem.MaxElective ?? 0));
                if (maxElectiveCount <= 0 && electiveSubs.Any())
                {
                    maxElectiveCount = 1;
                }

                int electiveCredits = 0;
                if (maxElectiveCount > 0 && electiveSubs.Any())
                {
                    electiveCredits = electiveSubs
                        .OrderByDescending(s => s.Credit)
                        .Take(maxElectiveCount)
                        .Sum(s => s.Credit > 0 ? s.Credit : 3);
                }

                int semCreditSum = coreCredits + electiveCredits;
                if (semCreditSum == 0)
                {
                    semCreditSum = (seq == totalRequiredSemesters) ? 12 : 18;
                }

                calcTargetGraduationCredits += semCreditSum;
                if (seq == totalRequiredSemesters)
                {
                    calcTargetFinalCredits += semCreditSum;
                }
                else
                {
                    calcTargetAcademicCredits += semCreditSum;
                }
            }

            int targetGraduationCredits = calcTargetGraduationCredits > 0 ? calcTargetGraduationCredits : 155;
            int targetAcademicCredits = calcTargetAcademicCredits > 0 ? calcTargetAcademicCredits : 143;
            int targetFinalCredits = calcTargetFinalCredits > 0 ? calcTargetFinalCredits : 12;

            graduationModel.TargetGraduationCredits = targetGraduationCredits;
            graduationModel.TargetSem1To7Credits = targetAcademicCredits;
            graduationModel.TargetSem8Credits = targetFinalCredits;

            var allResults = await _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue)
                .OrderBy(r => r.ResultId)
                .Select(r => new
                {
                    r.ResultId,
                    r.SubjectId,
                    r.SemesterId,
                    r.RegistrationId,
                    r.IsPass,
                    r.ReexamIsPass,
                    r.Grade,
                    r.ReexamGrade,
                    r.MarksObtained,
                    r.IsDisqualified,
                    r.CreatedDateTime,
                    r.ModifiedDateTime
                })
                .ToListAsync();

            var subjectDict = allCurriculumSubjects.ToDictionary(s => s.SubjectId);

            var subjectGroups = allResults
                .Where(r => r.SubjectId.HasValue && subjectDict.ContainsKey(r.SubjectId.Value))
                .GroupBy(r => r.SubjectId!.Value);

            int sem1To7Credits = 0;
            int sem8Credits = 0;
            int passedSubjectsCount = 0;
            int pendingFailedCount = 0;
            bool hasDisqualifiedSubject = false;
            var disqualifiedSubjects = new List<string>();

            foreach (var group in subjectGroups)
            {
                var sub = subjectDict[group.Key];
                var attempts = group.OrderBy(r => r.ResultId).ToList();

                bool isDisqualified = attempts.Any(r => r.IsDisqualified);
                if (isDisqualified)
                {
                    hasDisqualifiedSubject = true;
                    disqualifiedSubjects.Add(sub.SubjectCode ?? sub.SubjectName ?? "Subject");
                }

                bool isPassed = attempts.Any(r => r.IsPass || (r.ReexamIsPass == true) || (r.Grade != "F" && r.Grade != "D" && !string.IsNullOrWhiteSpace(r.Grade)));
                int credit = sub.Credit > 0 ? sub.Credit : 3;

                if (isPassed)
                {
                    passedSubjectsCount++;
                    bool isSem8 = sub.SemesterId == 9 || (sub.Semester?.SemesterName != null && sub.Semester.SemesterName.ToLower().Contains("viii")) || (sub.SubjectName != null && sub.SubjectName.ToLower().Contains("internship"));
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

            var distinctResultSemesters = allResults
                .Where(r => r.SemesterId.HasValue && r.SemesterId.Value > 0)
                .Select(r => r.SemesterId!.Value)
                .Distinct()
                .ToList();

            int completedSemesters = distinctResultSemesters.Count;

            int totalEarnedCredits = sem1To7Credits + sem8Credits;
            double progress = targetGraduationCredits > 0
                ? Math.Round((double)totalEarnedCredits / (double)targetGraduationCredits * 100.0, 1)
                : 0;
            if (progress > 100.0) progress = 100.0;

            bool isGraduated = completedSemesters >= totalRequiredSemesters && totalEarnedCredits >= targetGraduationCredits && !hasDisqualifiedSubject && pendingFailedCount == 0;

            graduationModel.TotalEarnedCredits = totalEarnedCredits;
            graduationModel.Sem1To7Credits = sem1To7Credits;
            graduationModel.Sem8Credits = sem8Credits;
            graduationModel.ProgressPercentage = progress;
            graduationModel.TotalPassedSubjects = passedSubjectsCount;
            graduationModel.PendingFailedSubjects = pendingFailedCount;
            graduationModel.CompletedSemestersCount = completedSemesters;
            graduationModel.AllRetakesCleared = pendingFailedCount == 0;
            graduationModel.AllCurriculumCompleted = totalEarnedCredits >= targetGraduationCredits;
            graduationModel.IsGraduated = isGraduated;
            graduationModel.HasDisqualifiedSubject = hasDisqualifiedSubject;

            // Compute Semester-wise GPA breakdown, Subject Results, and Cumulative GPA (CGPA)
            // KEY LOGIC: Group subjects by their ORIGINAL Curriculum Semester (Subject.SemesterId),
            // NOT by the registration semester where the retake took place.
            // This ensures Carry-over / Retake subjects always contribute GPA to their original Semester!

            // Step 1: Deduplicate per subject, keeping the latest attempt
            var latestResultPerSubject = allResults
                .Where(r => r.SubjectId.HasValue && subjectDict.ContainsKey(r.SubjectId.Value))
                .GroupBy(r => r.SubjectId!.Value)
                .Select(g => g
                    .OrderByDescending(r => r.ModifiedDateTime ?? r.CreatedDateTime ?? DateTime.MinValue)
                    .ThenByDescending(r => r.ResultId)
                    .First()
                )
                .ToList();

            // Step 2: Group by Subject's own Curriculum SemesterId (home semester of that subject)
            var semGrouped = latestResultPerSubject
                .GroupBy(r => {
                    var sub = subjectDict[r.SubjectId!.Value];
                    return sub.SemesterId > 0 ? sub.SemesterId : (r.SemesterId ?? 0);
                })
                .Where(g => g.Key > 0)
                .OrderBy(g => g.Key);

            decimal sumAllGradePoints = 0.0m;
            int sumAllCredits = 0;
            var semGpaList = new List<SemesterGpaBreakdownModel>();

            foreach (var sg in semGrouped)
            {
                int semId = sg.Key;
                string semName = allSemestersList.FirstOrDefault(s => s.SemesterId == semId)?.SemesterName
                    ?? $"Semester {semId}";

                int semCredits = 0;
                decimal semPoints = 0.0m;
                var subjectResults = new List<SemesterSubjectResultItemModel>();

                foreach (var r in sg)
                {
                    var sub = subjectDict[r.SubjectId!.Value];
                    int cred = sub.Credit > 0 ? sub.Credit : 3;
                    decimal gp = 0.0m;
                    string? gLetter = !string.IsNullOrWhiteSpace(r.ReexamGrade) && (r.ReexamIsPass == true || (r.ReexamGrade != "D" && r.ReexamGrade != "F"))
                        ? r.ReexamGrade 
                        : r.Grade;

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

                    subjectResults.Add(new SemesterSubjectResultItemModel
                    {
                        SubjectId = r.SubjectId ?? 0,
                        SubjectCode = sub.SubjectCode ?? string.Empty,
                        SubjectName = sub.SubjectName ?? string.Empty,
                        CreditUnit = cred,
                        MarksObtained = r.MarksObtained,
                        Grade = gLetter ?? string.Empty,
                        GradeScore = gp,
                        GradePointEarned = earned,
                        GradeStatus = GradeCalculator.GetGradeStatus(gLetter),
                        ReexamGrade = r.ReexamGrade,
                        ReexamIsPass = r.ReexamIsPass,
                        IsPass = r.IsPass || (r.ReexamIsPass == true)
                    });
                }

                decimal semGpa = GradeCalculator.CalculateSemesterGPA(semPoints, semCredits);
                string semStatus = GradeCalculator.GetGradeStatus(GradeCalculator.DefaultGradeTiers.FirstOrDefault(t => semGpa >= t.GradePoint)?.LetterGrade ?? "Good");

                var accumulatedGpas = semGpaList.Select(s => s.SemesterGPA).Append(semGpa).ToList();
                decimal cumGpaUpToNow = GradeCalculator.CalculateCumulativeGPAFromSemesterGpas(accumulatedGpas);

                semGpaList.Add(new SemesterGpaBreakdownModel
                {
                    SemesterId = semId,
                    SemesterName = semName,
                    TotalCredits = semCredits,
                    TotalGradePointsEarned = semPoints,
                    SemesterGPA = semGpa,
                    CumulativeGPAUpToThisSemester = cumGpaUpToNow,
                    Status = semStatus,
                    SubjectResults = subjectResults
                });

                sumAllCredits += semCredits;
                sumAllGradePoints += semPoints;
            }

            decimal cgpa = semGpaList.Any()
                ? GradeCalculator.CalculateCumulativeGPAFromSemesterGpas(semGpaList.Select(s => s.SemesterGPA))
                : 0.0m;
            string overallStanding = "Satisfactory";
            if (cgpa >= 4.00m) overallStanding = "Excellent";
            else if (cgpa >= 3.67m) overallStanding = "Very Good";
            else if (cgpa >= 2.67m) overallStanding = "Good";
            else if (cgpa >= 2.00m) overallStanding = "Satisfactory";
            else if (cgpa < 2.00m && cgpa > 0) overallStanding = "Marginal";

            graduationModel.SemesterGpaList = semGpaList;
            graduationModel.TotalGradePointsEarned = sumAllGradePoints;
            graduationModel.CumulativeGPA = cgpa;
            graduationModel.OverallAcademicStanding = overallStanding;

            // CGPA Check for Graduation: Must achieve Cumulative GPA >= 2.00 to graduate
            bool meetsCgpaRequirement = cgpa >= 2.00m;
            isGraduated = isGraduated && meetsCgpaRequirement;
            graduationModel.IsGraduated = isGraduated;

            if (hasDisqualifiedSubject)
            {
                graduationModel.GraduationStatus = "Disqualified";
                graduationModel.GraduationStatusText = $"⛔ ဘာသာရပ် ({string.Join(", ", disqualifiedSubjects)}) အား ၂ ကြိမ်မြောက် Re-exam ကျရှုံးခဲ့သဖြင့် Retake ယူခွင့်ပိတ်သိမ်းခံရပြီး ဘွဲ့ရရှိရန် Credit မပြည့်မီနိုင်တော့ပါ (Ineligible for Graduation)";

                // Sync Student table Status to 'Disqualified'
                if (userId.HasValue && userId.Value > 0)
                {
                    var studentEntity = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                    if (studentEntity != null && studentEntity.Status != "Disqualified")
                    {
                        studentEntity.Status = "Disqualified";
                        studentEntity.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
                        studentEntity.ModifiedBy = "GraduationSystem";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else if (isGraduated)
            {
                graduationModel.GraduationStatus = "Graduated";
                if (cgpa >= 3.00m)
                {
                    graduationModel.GraduationStatusText = $"🎓 မဟာဘွဲ့ တက်ရောက်ခွင့်ရရှိပြီး ဘွဲ့ရရှိပြီး (Graduated with Master Eligibility - CGPA {cgpa:F2})";
                }
                else
                {
                    graduationModel.GraduationStatusText = $"🎓 ဘွဲ့ကြို ရိုးရိုးဘွဲ့ ရရှိပြီး (Graduated - Bachelor Degree - CGPA {cgpa:F2})";
                }
                graduationModel.GraduationDate = DateTime.UtcNow.AddHours(6).AddMinutes(30);

                // Auto-sync Student table Status to 'Graduated'
                if (userId.HasValue && userId.Value > 0)
                {
                    var studentEntity = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                    if (studentEntity != null && studentEntity.Status != "Graduated")
                    {
                        studentEntity.Status = "Graduated";
                        studentEntity.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
                        studentEntity.ModifiedBy = "GraduationSystem";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else if (completedSemesters >= totalRequiredSemesters && totalEarnedCredits >= targetGraduationCredits && !hasDisqualifiedSubject && pendingFailedCount == 0 && cgpa < 2.00m)
            {
                graduationModel.GraduationStatus = "Ineligible";
                graduationModel.GraduationStatusText = $"❌ ဘွဲ့ရရှိရန် သတ်မှတ် CGPA 2.00 မပြည့်မီပါ (Ineligible - CGPA {cgpa:F2} < 2.00)";

                if (userId.HasValue && userId.Value > 0)
                {
                    var studentEntity = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                    if (studentEntity != null && studentEntity.Status == "Graduated")
                    {
                        studentEntity.Status = "Ineligible";
                        studentEntity.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
                        studentEntity.ModifiedBy = "GraduationSystem";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                graduationModel.GraduationStatus = "Studying";
                graduationModel.GraduationStatusText = $"ပညာသင်ယူဆဲ (In Progress - {totalEarnedCredits}/{targetGraduationCredits} Credits)";

                // Auto-sync Student table Status back to 'Active' if previously set to 'Graduated' but credits < targetGraduationCredits
                if (userId.HasValue && userId.Value > 0)
                {
                    var studentEntity = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId.Value);
                    if (studentEntity != null && studentEntity.Status == "Graduated")
                    {
                        studentEntity.Status = "Active";
                        studentEntity.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
                        studentEntity.ModifiedBy = "GraduationSystem";
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return graduationModel;
        }

        private static Semester? ResolveSemester(string? text, IEnumerable<Semester> allSemesters)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var trimmed = text.Trim();

            // 1. Exact match
            var exact = allSemesters.FirstOrDefault(s => string.Equals(s.SemesterName.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // 2. Keyword sequence matching (order matters: check longer Roman numerals first to prevent substring collisions)
            var lower = trimmed.ToLowerInvariant();
            int? seq = null;
            if (lower.Contains("first year") && lower.Contains("first sem")) seq = 1;
            else if (lower.Contains("first year") && lower.Contains("second sem")) seq = 2;
            else if (lower.Contains("second year") && lower.Contains("first sem")) seq = 3;
            else if (lower.Contains("second year") && lower.Contains("second sem")) seq = 4;
            else if (lower.Contains("third year") && lower.Contains("first sem")) seq = 5;
            else if (lower.Contains("third year") && lower.Contains("second sem")) seq = 6;
            else if (lower.Contains("fourth year") && lower.Contains("first sem")) seq = 7;
            else if (lower.Contains("fourth year") && lower.Contains("second sem")) seq = 8;
            else if (lower.Contains("fifth year") && lower.Contains("first sem")) seq = 9;
            else if (lower.Contains("semester viii") || lower.Contains("sem 8") || lower.Contains("semester 8")) seq = 8;
            else if (lower.Contains("semester vii") || lower.Contains("sem 7") || lower.Contains("semester 7")) seq = 7;
            else if (lower.Contains("semester vi") || lower.Contains("sem 6") || lower.Contains("semester 6")) seq = 6;
            else if (lower.Contains("semester v") || lower.Contains("sem 5") || lower.Contains("semester 5")) seq = 5;
            else if (lower.Contains("semester iv") || lower.Contains("sem 4") || lower.Contains("semester 4")) seq = 4;
            else if (lower.Contains("semester iii") || lower.Contains("sem 3") || lower.Contains("semester 3")) seq = 3;
            else if (lower.Contains("semester ii") || lower.Contains("sem 2") || lower.Contains("semester 2")) seq = 2;
            else if (lower.Contains("semester i") || lower.Contains("sem 1") || lower.Contains("semester 1")) seq = 1;

            if (seq.HasValue)
            {
                return allSemesters.FirstOrDefault(s => s.Sequence == seq.Value);
            }

            return null;
        }
    }
}
