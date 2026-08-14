using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;

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
            var enrollments = await _context.StudentSubjectEnrollments
                .Include(e => e.Semester)
                .Include(e => e.Subject)
                .Include(e => e.Result)
                .Include(e => e.Student)
                .Where(e => e.StudentId == studentId && (e.IsDelete == false || e.IsDelete == null))
                .OrderByDescending(e => e.EnrollmentDate)
                .Select(e => new StudentEnrollmentResultModel
                {
                    EnrollmentId = e.EnrollmentId,
                    StudentId = e.StudentId,
                    StudentName = e.Student != null ? e.Student.StudentName : string.Empty,
                    RollNo = e.Student != null ? e.Student.CurrentRollNo : string.Empty,
                    SemesterId = e.SemesterId,
                    SemesterName = e.Semester != null ? e.Semester.SemesterName : string.Empty,
                    SubjectId = e.SubjectId,
                    SubjectCode = e.Subject != null ? e.Subject.SubjectCode : string.Empty,
                    SubjectName = e.Subject != null ? e.Subject.SubjectName : string.Empty,
                    EnrollmentDate = e.EnrollmentDate,
                    MaxMarks = e.Result != null ? e.Result.MaxMarks : null,
                    MarksObtained = e.Result != null ? e.Result.MarksObtained : null,
                    Grade = e.Result != null ? e.Result.Grade : string.Empty,
                    IsPass = e.Result != null && e.Result.IsPass
                })
                .ToListAsync();

            return enrollments;
        }

        public async Task<List<StudentEnrollmentResultModel>> GetAllEnrollmentsWithResultsAsync()
        {
            var registrations = await _context.StudentRegistrations
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.IsDelete == false || r.IsDelete == null)
                .OrderByDescending(r => r.RegistrationId)
                .ToListAsync();

            var results = registrations.Select(r => new StudentEnrollmentResultModel
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
            }).ToList();

            return results;
        }

        public async Task<StudentEnrollmentDetailResponseModel?> GetEnrollmentDetailsAsync(int registrationId)
        {
            var reg = await _context.StudentRegistrations
                .AsNoTracking()
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId && (r.IsDelete == false || r.IsDelete == null));

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

            // 5. Fetch existing StudentSubjectResults for this registration
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

            var subjectItems = subjects.Select(s =>
            {
                var prereqList = prerequisites
                    .Where(p => p.SubjectId == s.SubjectId && p.PrerequisiteSubject != null)
                    .Select(p => $"{p.PrerequisiteSubject!.SubjectCode} ({p.PrerequisiteSubject.SubjectName})")
                    .ToList();

                var prereqInfo = prereqList.Any() ? string.Join(", ", prereqList) : "-";
                var resultRecord = existingResults.FirstOrDefault(r => r.SubjectId == s.SubjectId);

                return new StudentSubjectGradeItemModel
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    SubjectType = s.SubjectType,
                    PrerequisiteInfo = prereqInfo,
                    Grade = resultRecord?.Grade,
                    ResultId = resultRecord?.ResultId,
                    IsPass = resultRecord?.IsPass ?? false
                };
            }).ToList();

            return new StudentEnrollmentDetailResponseModel
            {
                RegistrationId = registrationId,
                StudentId = null,
                StudentName = !string.IsNullOrEmpty(reg.StudentNameEn) ? reg.StudentNameEn : (reg.StudentNameMm ?? string.Empty),
                RollNo = reg.RollNo ?? string.Empty,
                SemesterId = semesterId,
                SemesterName = semesterName,
                MajorName = majorName,
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

                foreach (var item in request.Grades)
                {
                    var gradeStr = string.IsNullOrWhiteSpace(item.Grade) ? null : item.Grade.Trim();
                    bool isPass = IsGradePass(gradeStr);

                    var existing = await _context.StudentSubjectResults
                        .FirstOrDefaultAsync(r => r.RegistrationId == request.RegistrationId && r.SubjectId == item.SubjectId);

                    if (existing != null)
                    {
                        existing.Grade = gradeStr;
                        existing.IsPass = isPass;
                        existing.SemesterId = request.SemesterId;
                        existing.ModifiedDateTime = DateTime.Now;
                        existing.ModifiedBy = "System";
                    }
                    else if (!string.IsNullOrWhiteSpace(gradeStr))
                    {
                        var newResult = new StudentSubjectResult
                        {
                            RegistrationId = request.RegistrationId,
                            StudentId = request.StudentId,
                            SubjectId = item.SubjectId,
                            SemesterId = request.SemesterId,
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
                    .Where(r => r.RegistrationId == request.RegistrationId && r.SubjectId.HasValue && r.SubjectId > 0)
                    .ToListAsync();

                if (allRegistrationResults.Any())
                {
                    int totalCount = allRegistrationResults.Count;
                    int passCount = allRegistrationResults.Count(r => r.IsPass);

                    // ဥပမာ - ၇ ဘာသာတွင် ၄ ဘာသာ Pass ဖြစ်လျှင် (4 > 3.5) အောင်မည်
                    // ၄ ဘာသာ Fail ဖြစ်လျှင် (3 > 3.5 is False) ကျမည်
                    bool isSemesterPass = totalCount > 0 && (passCount > (totalCount / 2.0));
                    string semesterResultStatus = isSemesterPass ? "Pass" : "Fail";

                    var reg = await _context.StudentRegistrations
                        .FirstOrDefaultAsync(r => r.RegistrationId == request.RegistrationId);

                    var sem = await _context.Semesters
                        .FirstOrDefaultAsync(s => s.SemesterId == request.SemesterId);

                    int semSeq = sem?.Sequence ?? 1;

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
                    SubjectType = s.SubjectType,
                    SemesterId = s.SemesterId,
                    SemesterName = s.Semester?.SemesterName,
                    MajorId = s.MajorId,
                    MajorName = s.Major?.MajorName,
                    IsRetake = false
                }).ToList();

            // =========================================================================
            // Retake Subjects Integration:
            // Query any failed subjects (IsPass == false or Grade in D/F) from previous semesters
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
                    var retakeResults = await _context.StudentSubjectResults
                        .AsNoTracking()
                        .Include(r => r.Subject)
                            .ThenInclude(sub => sub.Semester)
                        .Include(r => r.Subject)
                            .ThenInclude(sub => sub.Major)
                        .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) &&
                                    r.SubjectId.HasValue && r.SemesterId != semesterId &&
                                    (r.IsPass == false || r.Grade == "D" || r.Grade == "F"))
                        .ToListAsync();

                    var currentSubjectIds = subjectList.Select(s => s.SubjectId).ToHashSet();
                    var retakeSubjects = retakeResults
                        .Where(r => r.Subject != null && !currentSubjectIds.Contains(r.SubjectId!.Value))
                        .GroupBy(r => r.SubjectId!.Value)
                        .Select(g => g.First())
                        .Select(r => new SubjectModel
                        {
                            SubjectId = r.Subject!.SubjectId,
                            SubjectName = r.Subject.SubjectName,
                            SubjectCode = r.Subject.SubjectCode,
                            SubjectType = r.Subject.SubjectType,
                            SemesterId = r.Subject.SemesterId,
                            SemesterName = r.Subject.Semester?.SemesterName,
                            MajorId = r.Subject.MajorId,
                            MajorName = r.Subject.Major?.MajorName,
                            IsRetake = true
                        })
                        .ToList();

                    subjectList.AddRange(retakeSubjects);
                }
            }

            // =========================================================================
            // Prerequisites & Elective Eligibility Validation:
            // Check if student passed prerequisite subjects with Grade A+ to C (IsPass == true)
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
                        .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue)
                        .ToListAsync();
                }
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
                    bool allPassed = true;
                    List<string> missingList = new();

                    foreach (var req in reqs)
                    {
                        var pastResult = studentPastResults.FirstOrDefault(r => r.SubjectId == req.PrerequisiteSubjectId);
                        bool isPassed = pastResult != null && IsGradePass(pastResult.Grade);
                        if (!isPassed)
                        {
                            allPassed = false;
                            missingList.Add(req.PrerequisiteSubject!.SubjectCode);
                        }
                    }

                    sub.IsPrerequisiteSatisfied = allPassed;
                    if (allPassed)
                    {
                        sub.PrerequisiteStatusMessage = "Pre-Requisite ပြည့်မီပါသည်";
                    }
                    else
                    {
                        sub.PrerequisiteStatusMessage = $"Pre-Requisite မအောင်မြင်ပါ ({string.Join(", ", missingList)} ကို A+ မှ C ရရှိထားရပါမည်)";
                    }
                }
                else
                {
                    sub.PrerequisiteInfo = "-";
                    sub.IsPrerequisiteSatisfied = true;
                    sub.PrerequisiteStatusMessage = "Pre-Requisite မလိုအပ်ပါ";
                }

                // Core & Retake are selected by default
                if (sub.SubjectType != Database.AppDbContext.EnumSubjectType.Elective || sub.IsRetake)
                {
                    sub.IsSelected = true;
                }
                else
                {
                    sub.IsSelected = false;
                }
            }

            return subjectList;
        }

        public async Task<List<StudentSubjectGradeItemModel>> GetPreviousSemesterGradesAsync(int? userId, int? newStudentAccId, string? rollNo, int semesterId, string? major)
        {
            var sem = await _context.Semesters.AsNoTracking().FirstOrDefaultAsync(s => s.SemesterId == semesterId);
            var semName = sem?.SemesterName;

            var subjects = await GetSemesterSubjectsByMajorAsync(semesterId, major);
            if (!subjects.Any())
            {
                return new List<StudentSubjectGradeItemModel>();
            }

            var subjectIds = subjects.Select(s => s.SubjectId).ToList();

            // Find all matching registration IDs for this user
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
                return subjects.Select(s => new StudentSubjectGradeItemModel
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

            var regIds = await regQuery.Select(r => r.RegistrationId).ToListAsync();

            var results = await _context.StudentSubjectResults
                .AsNoTracking()
                .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) && r.SubjectId.HasValue && subjectIds.Contains(r.SubjectId.Value))
                .ToListAsync();

            var list = subjects.Select(s =>
            {
                var resultRecord = results.FirstOrDefault(r => r.SubjectId == s.SubjectId);
                return new StudentSubjectGradeItemModel
                {
                    SubjectId = s.SubjectId,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    SemesterName = semName,
                    SubjectType = s.SubjectType,
                    Grade = resultRecord?.Grade ?? "",
                    ResultId = resultRecord?.ResultId,
                    IsPass = resultRecord?.IsPass ?? false,
                    IsRetake = false
                };
            }).ToList();

            // =========================================================================
            // ALSO query any failed retake subjects from older semesters (e.g. Sem 1, Sem 2)
            // =========================================================================
            if (regIds.Any())
            {
                var retakeResults = await _context.StudentSubjectResults
                    .AsNoTracking()
                    .Include(r => r.Subject)
                        .ThenInclude(sub => sub.Semester)
                    .Where(r => r.RegistrationId.HasValue && regIds.Contains(r.RegistrationId.Value) &&
                                r.SubjectId.HasValue && r.SemesterId != semesterId &&
                                (r.IsPass == false || r.Grade == "D" || r.Grade == "F"))
                    .ToListAsync();

                var currentIds = list.Select(l => l.SubjectId).ToHashSet();

                var olderRetakes = retakeResults
                    .Where(r => r.Subject != null && !currentIds.Contains(r.SubjectId!.Value))
                    .GroupBy(r => r.SubjectId!.Value)
                    .Select(g => g.First())
                    .Select(r => new StudentSubjectGradeItemModel
                    {
                        SubjectId = r.Subject!.SubjectId,
                        SubjectCode = r.Subject.SubjectCode,
                        SubjectName = r.Subject.SubjectName,
                        SemesterName = r.Subject.Semester?.SemesterName,
                        SubjectType = r.Subject.SubjectType,
                        Grade = r.Grade ?? "",
                        ResultId = r.ResultId,
                        IsPass = r.IsPass,
                        IsRetake = true
                    })
                    .ToList();

                list.AddRange(olderRetakes);
            }

            return list;
        }
    }
}
