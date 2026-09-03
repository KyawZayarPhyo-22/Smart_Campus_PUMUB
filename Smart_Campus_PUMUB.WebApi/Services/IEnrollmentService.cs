using System.Collections.Generic;
using System.Threading.Tasks;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Services
{
    public interface IEnrollmentService
    {
        Task<(bool Success, string Message, List<int> MissingPrerequisites)> EnrollStudentAsync(int studentId, int newSubjectId, int semesterId);
        Task<List<StudentEnrollmentResultModel>> GetStudentEnrollmentsWithResultsAsync(int studentId);
        Task<List<StudentEnrollmentResultModel>> GetAllEnrollmentsWithResultsAsync();
        Task<StudentEnrollmentDetailResponseModel?> GetEnrollmentDetailsAsync(int registrationId);
        Task<ActionResponseModel> SaveStudentGradesAsync(SaveStudentGradesRequestModel request);
        Task<ActionResponseModel> SaveReexamGradesAsync(SaveReexamGradesRequestModel request);
        Task<List<StudentSubjectGradeItemModel>> GetPreviousSemesterGradesAsync(int? userId, int? newStudentAccId, string? rollNo, int semesterId, string? major);
        Task<List<SubjectModel>> GetSemesterSubjectsByMajorAsync(int semesterId, string? major, int? userId = null, int? newStudentAccId = null, string? rollNo = null);
        Task<int> GetMaxRetakeLimitAsync();
        Task<bool> UpdateMaxRetakeLimitAsync(int newLimit, string? updatedBy = "Admin");
        Task<StudentRetakeStatusModel> GetStudentRetakeStatusAsync(int? userId, int? studentId = null, string? rollNo = null);
        Task<StudentSubjectSelectionResponseModel> GetStudentSubjectSelectionsAsync(int? userId, int? newStudentAccId = null);
        Task<StudentGraduationStatusModel> GetStudentGraduationStatusAsync(int? userId, int? studentId = null, string? rollNo = null, int? newStudentAccId = null);
    }
}
