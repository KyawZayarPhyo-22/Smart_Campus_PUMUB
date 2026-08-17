using Smart_Campus_PUMUB.WebApi.Models;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.WebApi.Services;

public interface IGradeService
{
    Task<List<GradeModel>> GetGrades();
    Task<GradeModel?> GetGrade(int id);
    Task<ActionResponseModel> CreateGrade(GradeRequestModel reqModel);
    Task<ActionResponseModel> UpdateGrade(int id, GradeRequestModel reqModel);
    Task<ActionResponseModel> DeleteGrade(int id);
}
