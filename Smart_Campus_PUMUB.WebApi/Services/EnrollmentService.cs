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
            var results = await _context.StudentSubjectResults
                .Include(r => r.Student)
                .Include(r => r.Semester)
                .Include(r => r.Subject)
                .Include(r => r.Registration)
                .OrderByDescending(r => r.ResultId)
                .Select(r => new StudentEnrollmentResultModel
                {
                    EnrollmentId = r.EnrollmentId ?? r.ResultId,
                    StudentId = r.StudentId ?? 0,
                    StudentName = r.Registration != null && !string.IsNullOrEmpty(r.Registration.StudentNameEn) ? r.Registration.StudentNameEn : (r.Student != null ? r.Student.StudentName : string.Empty),
                    RollNo = r.Registration != null && !string.IsNullOrEmpty(r.Registration.RollNo) ? r.Registration.RollNo : (r.Student != null ? r.Student.CurrentRollNo : string.Empty),
                    SemesterId = r.SemesterId ?? 0,
                    SemesterName = r.Registration != null && !string.IsNullOrEmpty(r.Registration.AcademicYearLevel) ? r.Registration.AcademicYearLevel : (r.Semester != null ? r.Semester.SemesterName : string.Empty),
                    SubjectId = r.SubjectId ?? 0,
                    SubjectCode = r.Subject != null ? r.Subject.SubjectCode : string.Empty,
                    SubjectName = r.Subject != null ? r.Subject.SubjectName : string.Empty,
                    EnrollmentDate = r.CreatedDateTime ?? DateTime.Now,
                    MaxMarks = r.MaxMarks,
                    MarksObtained = r.MarksObtained,
                    Grade = r.Grade ?? string.Empty,
                    IsPass = r.IsPass
                })
                .ToListAsync();

            return results;
        }
    }
}
