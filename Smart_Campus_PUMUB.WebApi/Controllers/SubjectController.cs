using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;

namespace NLADotNetInternshipTraining.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly IFacultyDataScopeService _scopeService;

    public SubjectController(SmartCampusDbContext db, IFacultyDataScopeService scopeService)
    {
        _db = db;
        _scopeService = scopeService;
    }

    // GET: api/subject
    [HttpGet]
    [Permission("Subject.View")]
    public IActionResult GetAll()
    {
        var query = _db.Subjects
                           .Include(s => s.Semester)
                           .Include(s => s.Faculty)
                           .Where(x => x.IsDelete == false || x.IsDelete == null);

        // Hierarchical RBAC Data Scoping:
        if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
        {
            var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
            if (scopedFacultyId.HasValue)
                query = query.Where(x => x.FacultyId == scopedFacultyId.Value);
        }

        var subjects = query
                           .OrderByDescending(s => s.SubjectId)
                           .Select(s => new SubjectModel
                           {
                               SubjectId   = s.SubjectId,
                               SubjectName = s.SubjectName,
                               SubjectCode = s.SubjectCode,
                               SemesterId  = s.SemesterId,
                               SemesterName = s.Semester.SemesterName,
                               FacultyId   = s.FacultyId,
                               FacultyName = s.Faculty != null ? s.Faculty.FacultyName : null
                           }).ToList();
        return Ok(subjects);
    }

    // GET: api/subject/{id}
    [HttpGet("{id}")]
    [Permission("Subject.View")]
    public IActionResult GetSubject(int id)
    {
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.Subjects
                       .Include(s => s.Semester)
                       .Include(s => s.Faculty)
                       .FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "တောင်းဆိုထားသော ဘာသာရပ်ဒေတာ ရှာမတွေ့ပါ။" });

        return Ok(new SubjectModel
        {
            SubjectId    = item.SubjectId,
            SubjectName  = item.SubjectName,
            SubjectCode  = item.SubjectCode,
            SemesterId   = item.SemesterId,
            SemesterName = item.Semester?.SemesterName,
            FacultyId    = item.FacultyId,
            FacultyName  = item.Faculty?.FacultyName
        });
    }

    // GET: api/subject/paginate
    [HttpGet("paginate")]
    [Permission("Subject.View")]
    public IActionResult GetPaginated([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
    {
        var query = _db.Subjects
                        .Include(s => s.Semester)
                        .Include(s => s.Faculty)
                        .Where(x => x.IsDelete == false || x.IsDelete == null)
                        .AsQueryable();

        // Hierarchical RBAC Data Scoping:
        if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
        {
            var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
            if (scopedFacultyId.HasValue)
                query = query.Where(x => x.FacultyId == scopedFacultyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.SubjectName.ToLower().Contains(term) ||
                x.SubjectCode.ToLower().Contains(term) ||
                (x.Semester != null && x.Semester.SemesterName.ToLower().Contains(term)) ||
                (x.Faculty != null && x.Faculty.FacultyName.ToLower().Contains(term)));
        }

        int total = query.Count();

        var items = query
            .OrderByDescending(s => s.SubjectId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubjectModel
            {
                SubjectId    = s.SubjectId,
                SubjectName  = s.SubjectName,
                SubjectCode  = s.SubjectCode,
                SemesterId   = s.SemesterId,
                SemesterName = s.Semester.SemesterName,
                FacultyId    = s.FacultyId,
                FacultyName  = s.Faculty != null ? s.Faculty.FacultyName : null
            }).ToList();

        return Ok(new PagedResult<SubjectModel>
        {
            Items      = items,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize   = pageSize
        });
    }

    // POST: api/subject
    [HttpPost]
    [Permission("Subject.Create")]
    public IActionResult CreateSubject([FromBody] SubjectCreateRequestModel request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var semester = _db.Semesters.FirstOrDefault(s => s.SemesterId == request.SemesterId);
        if (semester is null || semester.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Semester ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });

        // Faculty FK validation (if provided)
        if (request.FacultyId.HasValue && request.FacultyId > 0)
        {
            var faculty = _db.Faculties.FirstOrDefault(f => f.FacultyId == request.FacultyId && (f.IsDelete == false || f.IsDelete == null));
            if (faculty is null)
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Faculty ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });
        }

        var isDuplicate = _db.Subjects.Any(x => x.SemesterId == request.SemesterId
                                          && (x.SubjectName.Trim().ToLower() == request.SubjectName!.Trim().ToLower()
                                              || x.SubjectCode.Trim().ToLower() == request.SubjectCode!.Trim().ToLower())
                                          && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicate)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ဤ Semester အောက်တွင် ဘာသာရပ်အမည် သို့မဟုတ် ဘာသာရပ်ကုဒ် (Subject Code) ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });

        _db.Subjects.Add(new Subject
        {
            SemesterId  = request.SemesterId,
            FacultyId   = request.FacultyId,
            SubjectName = request.SubjectName!.Trim(),
            SubjectCode = request.SubjectCode!.Trim().ToUpper(),
            CreatedDateTime = DateTime.Now,
            CreatedBy   = request.CreatedBy,
            IsDelete    = false
        });

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle   = "Subject Added",
            Description     = $"Subject '{request.SubjectName}' was added to the system.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy       = "Admin"
        });
        _db.SaveChanges();

        return StatusCode(201, new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Saving Successful" : "Saving Failed" });
    }

    // PUT: api/subject/{id}
    [HttpPut("{id}")]
    [Permission("Subject.Edit")]
    public IActionResult UpdateSubject(int id, [FromBody] SubjectUpdateRequestModel request)
    {
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        if (!ModelState.IsValid) return BadRequest(ModelState);

        var item = _db.Subjects.FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် ဘာသာရပ်ဒေတာ ရှာမတွေ့ပါ။" });

        var semester = _db.Semesters.FirstOrDefault(s => s.SemesterId == request.SemesterId);
        if (semester is null || semester.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Semester ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });

        // Faculty FK validation (if provided)
        if (request.FacultyId.HasValue && request.FacultyId > 0)
        {
            var faculty = _db.Faculties.FirstOrDefault(f => f.FacultyId == request.FacultyId && (f.IsDelete == false || f.IsDelete == null));
            if (faculty is null)
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Faculty ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });
        }

        var isDuplicateOnOther = _db.Subjects.Any(x => x.SemesterId == request.SemesterId
                                                   && (x.SubjectName.Trim().ToLower() == request.SubjectName!.Trim().ToLower()
                                                       || x.SubjectCode.Trim().ToLower() == request.SubjectCode!.Trim().ToLower())
                                                   && x.SubjectId != id
                                                   && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicateOnOther)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ဘာသာရပ်အမည် သို့မဟုတ် ကုဒ်သည် အခြားမှတ်တမ်းတစ်ခုတွင် အသုံးပြုထားပြီး ဖြစ်ပါသည်။" });

        item.SemesterId      = request.SemesterId;
        item.FacultyId       = request.FacultyId;
        item.SubjectName     = request.SubjectName!.Trim();
        item.SubjectCode     = request.SubjectCode!.Trim().ToUpper();
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy      = request.ModifiedBy;

        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle   = "Subject Updated",
            Description     = $"Subject '{request.SubjectName}' was updated in the system.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy       = "Admin"
        });
        _db.SaveChanges();

        return Ok(new SubjectResponseModel
        {
            IsSuccess = result > 0,
            Message   = result > 0 ? "Update Successful" : "Update Failed",
            Data      = new SubjectModel
            {
                SubjectId   = item.SubjectId,
                SemesterId  = item.SemesterId,
                FacultyId   = item.FacultyId,
                SubjectName = item.SubjectName,
                SubjectCode = item.SubjectCode
            }
        });
    }

    // DELETE: api/subject/{id}
    [HttpDelete("{id}")]
    [Permission("Subject.Delete")]
    public IActionResult DeleteSubject(int id)
    {
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.Subjects.FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ဖျက်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        item.IsDelete = true;
        int result = _db.SaveChanges();

        _db.Activities.Add(new Activity
        {
            ActivityTitle   = "Subject Deleted",
            Description     = $"Subject '{item.SubjectName}' was deleted from the system.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy       = "Admin"
        });
        _db.SaveChanges();

        return Ok(new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Delete Successfully" : "Delete Failed" });
    }
}
