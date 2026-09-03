using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Services;

public class GradeService : IGradeService
{
    private readonly SmartCampusDbContext _db;

    public GradeService(SmartCampusDbContext db)
    {
        _db = db;
    }

    public async Task<List<GradeModel>> GetGrades()
    {
        var grades = await _db.Grades
            .AsNoTracking()
            .Where(x => x.IsDelete == false)
            .Select(x => new GradeModel
            {
                GradeId = x.GradeId,
                Name = x.Name,
                GradePoint = x.GradePoint,
                Status = x.Status,
                MinMark = x.MinMark,
                MaxMark = x.MaxMark
            })
            .ToListAsync();

        return grades;
    }

    public async Task<GradeModel?> GetGrade(int id)
    {
        var grade = await _db.Grades
            .AsNoTracking()
            .Where(x => x.IsDelete == false && x.GradeId == id)
            .Select(x => new GradeModel
            {
                GradeId = x.GradeId,
                Name = x.Name,
                GradePoint = x.GradePoint,
                Status = x.Status,
                MinMark = x.MinMark,
                MaxMark = x.MaxMark
            })
            .FirstOrDefaultAsync();

        return grade;
    }

    public async Task<ActionResponseModel> CreateGrade(GradeRequestModel reqModel)
    {
        try
        {
            var grade = new Grade
            {
                Name = reqModel.Name,
                GradePoint = reqModel.GradePoint,
                Status = reqModel.Status,
                MinMark = reqModel.MinMark,
                MaxMark = reqModel.MaxMark,
                CreatedDateTime = DateTime.Now,
                IsDelete = false
            };

            await _db.Grades.AddAsync(grade);
            var result = await _db.SaveChangesAsync();

            return new ActionResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Saving Successful." : "Saving Failed."
            };
        }
        catch (Exception ex)
        {
            return new ActionResponseModel
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ActionResponseModel> UpdateGrade(int id, GradeRequestModel reqModel)
    {
        try
        {
            var item = await _db.Grades.FirstOrDefaultAsync(x => x.GradeId == id && x.IsDelete == false);
            if (item is null)
            {
                return new ActionResponseModel
                {
                    IsSuccess = false,
                    Message = "No data found."
                };
            }

            item.Name = reqModel.Name;
            item.GradePoint = reqModel.GradePoint;
            item.Status = reqModel.Status;
            item.MinMark = reqModel.MinMark;
            item.MaxMark = reqModel.MaxMark;
            item.ModifiedDateTime = DateTime.Now;

            _db.Entry(item).State = EntityState.Modified;
            var result = await _db.SaveChangesAsync();

            return new ActionResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Updating Successful." : "Updating Failed."
            };
        }
        catch (Exception ex)
        {
            return new ActionResponseModel
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ActionResponseModel> DeleteGrade(int id)
    {
        try
        {
            var item = await _db.Grades.FirstOrDefaultAsync(x => x.GradeId == id && x.IsDelete == false);
            if (item is null)
            {
                return new ActionResponseModel
                {
                    IsSuccess = false,
                    Message = "No data found."
                };
            }

            item.IsDelete = true;
            item.ModifiedDateTime = DateTime.Now;

            _db.Entry(item).State = EntityState.Modified;
            var result = await _db.SaveChangesAsync();

            return new ActionResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Deleting Successful." : "Deleting Failed."
            };
        }
        catch (Exception ex)
        {
            return new ActionResponseModel
            {
                IsSuccess = false,
                Message = ex.Message
            };
        }
    }
}
