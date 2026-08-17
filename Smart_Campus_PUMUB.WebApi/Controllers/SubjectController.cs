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
                           .Include(s => s.Major)
                           .Include(s => s.Prerequisites)
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
                               SubjectType = s.SubjectType,
                               SemesterId  = s.SemesterId,
                               SemesterName = s.Semester.SemesterName,
                               FacultyId   = s.FacultyId,
                               FacultyName = s.Faculty != null ? s.Faculty.FacultyName : null,
                               MajorId     = s.MajorId,
                               MajorName   = s.Major != null ? s.Major.MajorName : null,
                               PrerequisiteSubjectIds = s.Prerequisites.Select(p => p.PrerequisiteSubjectId).ToList()
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
                       .Include(s => s.Major)
                       .Include(s => s.Prerequisites)
                       .FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "တောင်းဆိုထားသော ဘာသာရပ်ဒေတာ ရှာမတွေ့ပါ။" });

        return Ok(new SubjectModel
        {
            SubjectId    = item.SubjectId,
            SubjectName  = item.SubjectName,
            SubjectCode  = item.SubjectCode,
            SubjectType  = item.SubjectType,
            SemesterId   = item.SemesterId,
            SemesterName = item.Semester?.SemesterName,
            FacultyId    = item.FacultyId,
            FacultyName  = item.Faculty?.FacultyName,
            MajorId      = item.MajorId,
            MajorName    = item.Major?.MajorName,
            PrerequisiteSubjectIds = item.Prerequisites.Select(p => p.PrerequisiteSubjectId).ToList()
        });
    }

    // GET: api/subject/by-semester/{semesterId}
    [HttpGet("by-semester/{semesterId}")]
    [AllowAnonymous]
    public IActionResult GetBySemester(int semesterId, [FromQuery] int? facultyId, [FromQuery] int? majorId)
    {
        if (semesterId <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော Semester ID ဖြစ်နေပါသည်။" });

        var query = _db.Subjects
                       .Include(s => s.Semester)
                       .Include(s => s.Faculty)
                       .Include(s => s.Major)
                       .Where(x => x.SemesterId == semesterId && (x.IsDelete == false || x.IsDelete == null));

        if (facultyId.HasValue && facultyId > 0)
        {
            query = query.Where(x => x.FacultyId == facultyId.Value || x.FacultyId == null);
        }

        if (majorId.HasValue && majorId > 0)
        {
            query = query.Where(x => x.MajorId == majorId.Value || x.MajorId == null);
        }

        var subjects = query
                       .OrderBy(s => s.SubjectName)
                       .Select(s => new SubjectModel
                       {
                           SubjectId    = s.SubjectId,
                           SubjectName  = s.SubjectName,
                           SubjectCode  = s.SubjectCode,
                           SubjectType  = s.SubjectType,
                           SemesterId   = s.SemesterId,
                           SemesterName = s.Semester.SemesterName,
                           FacultyId    = s.FacultyId,
                           FacultyName  = s.Faculty != null ? s.Faculty.FacultyName : null,
                           MajorId      = s.MajorId,
                           MajorName    = s.Major != null ? s.Major.MajorName : null
                       }).ToList();

        return Ok(subjects);
    }

    // GET: api/subject/paginate
    [HttpGet("paginate")]
    [Permission("Subject.View")]
    public IActionResult GetPaginated([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
    {
        var query = _db.Subjects
                        .Include(s => s.Semester)
                        .Include(s => s.Faculty)
                        .Include(s => s.Major)
                        .Include(s => s.Prerequisites)
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
                (x.Faculty != null && x.Faculty.FacultyName.ToLower().Contains(term)) ||
                (x.Major != null && x.Major.MajorName.ToLower().Contains(term)));
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
                SubjectType  = s.SubjectType,
                SemesterId   = s.SemesterId,
                SemesterName = s.Semester.SemesterName,
                FacultyId    = s.FacultyId,
                FacultyName  = s.Faculty != null ? s.Faculty.FacultyName : null,
                MajorId      = s.MajorId,
                MajorName    = s.Major != null ? s.Major.MajorName : null,
                PrerequisiteSubjectIds = s.Prerequisites.Select(p => p.PrerequisiteSubjectId).ToList()
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

        if (request.SubjectType == EnumSubjectType.None)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Subject Type (Core သို့မဟုတ် Elective) ကို ရွေးချယ်ပေးပါ။" });

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

        // Major FK validation (if provided)
        if (request.MajorId.HasValue && request.MajorId > 0)
        {
            var major = _db.Majors.FirstOrDefault(m => m.MajorId == request.MajorId && (m.IsDelete == false || m.IsDelete == null));
            if (major is null)
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Major ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });
        }

        var isDuplicate = _db.Subjects.Any(x => x.SemesterId == request.SemesterId
                                          && (x.SubjectName.Trim().ToLower() == request.SubjectName!.Trim().ToLower()
                                              || x.SubjectCode.Trim().ToLower() == request.SubjectCode!.Trim().ToLower())
                                          && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicate)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ဤ Semester အောက်တွင် ဘာသာရပ်အမည် သို့မဟုတ် ဘာသာရပ်ကုဒ် (Subject Code) ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });

        var newSubject = new Subject
        {
            SemesterId  = request.SemesterId,
            FacultyId   = request.FacultyId,
            MajorId     = request.MajorId,
            SubjectName = request.SubjectName!.Trim(),
            SubjectCode = request.SubjectCode!.Trim().ToUpper(),
            SubjectType = request.SubjectType,
            CreatedDateTime = DateTime.Now,
            CreatedBy   = request.CreatedBy,
            IsDelete    = false
        };

        if (request.PrerequisiteSubjectIds != null && request.PrerequisiteSubjectIds.Any())
        {
            foreach (var pId in request.PrerequisiteSubjectIds)
            {
                newSubject.Prerequisites.Add(new SubjectPrerequisite 
                { 
                    PrerequisiteSubjectId = pId,
                    CreatedBy = request.CreatedBy,
                    CreatedDateTime = DateTime.Now
                });
            }
        }

        _db.Subjects.Add(newSubject);

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

        if (request.SubjectType == EnumSubjectType.None)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Subject Type (Core သို့မဟုတ် Elective) ကို ရွေးချယ်ပေးပါ။" });

        var item = _db.Subjects
            .Include(s => s.Prerequisites)
            .FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));
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

        // Major FK validation (if provided)
        if (request.MajorId.HasValue && request.MajorId > 0)
        {
            var major = _db.Majors.FirstOrDefault(m => m.MajorId == request.MajorId && (m.IsDelete == false || m.IsDelete == null));
            if (major is null)
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Major ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });
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
        item.MajorId         = request.MajorId;
        item.SubjectName     = request.SubjectName!.Trim();
        item.SubjectCode     = request.SubjectCode!.Trim().ToUpper();
        item.SubjectType     = request.SubjectType;
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy      = request.ModifiedBy;

        // Update prerequisites
        _db.SubjectPrerequisites.RemoveRange(item.Prerequisites);
        if (request.PrerequisiteSubjectIds != null && request.PrerequisiteSubjectIds.Any())
        {
            foreach (var pId in request.PrerequisiteSubjectIds)
            {
                item.Prerequisites.Add(new SubjectPrerequisite 
                { 
                    PrerequisiteSubjectId = pId,
                    CreatedBy = request.ModifiedBy,
                    CreatedDateTime = DateTime.Now
                });
            }
        }

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
                MajorId     = item.MajorId,
                SubjectName = item.SubjectName,
                SubjectCode = item.SubjectCode,
                SubjectType = item.SubjectType,
                PrerequisiteSubjectIds = item.Prerequisites.Select(p => p.PrerequisiteSubjectId).ToList()
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
