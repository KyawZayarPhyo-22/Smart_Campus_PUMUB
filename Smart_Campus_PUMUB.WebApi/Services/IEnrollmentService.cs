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
    }
}
