using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.WebApi.Services;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MajorController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;
        private readonly IFacultyDataScopeService _scopeService;

        public MajorController(SmartCampusDbContext db, IFacultyDataScopeService scopeService)
        {
            _db = db;
            _scopeService = scopeService;
        }

        // GET /api/major
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetMajors()
        {
            var query = _db.Majors
                         .AsNoTracking()
                         .Include(x => x.Faculty)
                         .Where(x => x.IsDelete == false || x.IsDelete == null);

            // Hierarchical RBAC Data Scoping:
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
                if (scopedFacultyId.HasValue)
                    query = query.Where(x => x.FacultyId == scopedFacultyId.Value);
            }

            var lst = query
                         .OrderByDescending(x => x.MajorId)
                         .Select(x => new MajorModel
                         {
                             MajorId = x.MajorId,
                             MajorName = x.MajorName,
                             FacultyId = x.FacultyId,
                             FacultyName = x.Faculty != null ? x.Faculty.FacultyName : null
                         })
                         .ToList();
            return Ok(lst);
        }

        // GET /api/major/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetMajor(int id)
        {
            if (id <= 0)
                return BadRequest(new MajorDeleteResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ဖြစ်နေပါသည်။" });

            var item = _db.Majors
                          .Include(x => x.Faculty)
                          .FirstOrDefault(x => x.MajorId == id && (x.IsDelete == false || x.IsDelete == null));
            if (item is null)
                return NotFound(new MajorDeleteResponseModel { IsSuccess = false, Message = "Major ကို ရှာမတွေ့ပါ။" });

            return Ok(new MajorModel
            {
                MajorId = item.MajorId,
                MajorName = item.MajorName,
                FacultyId = item.FacultyId,
                FacultyName = item.Faculty?.FacultyName
            });
        }

        // POST /api/major
        [HttpPost]
        [AllowAnonymous]
        public IActionResult CreateMajor([FromBody] MajorCreateRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.MajorName))
                return BadRequest(new MajorCreateResponseModel { IsSuccess = false, Message = "Major Name ဖြည့်ရန် လိုအပ်ပါသည်။" });

            var faculty = _db.Faculties.FirstOrDefault(f => f.FacultyId == request.FacultyId && (f.IsDelete == false || f.IsDelete == null));
            if (faculty is null)
                return BadRequest(new MajorCreateResponseModel { IsSuccess = false, Message = "ပေးထားသော Faculty မရှိပါ။" });

            var isDuplicate = _db.Majors.Any(x =>
                x.FacultyId == request.FacultyId &&
                x.MajorName.Trim().ToLower() == request.MajorName.Trim().ToLower() &&
                (x.IsDelete == false || x.IsDelete == null));
            if (isDuplicate)
                return BadRequest(new MajorCreateResponseModel { IsSuccess = false, Message = $"'{request.MajorName}' ဟူသော Major သည် ဤ Faculty အောက်တွင် ရှိနှင့်ပြီးသားဖြစ်နေပါသည်။" });

            _db.Majors.Add(new Major
            {
                MajorName = request.MajorName.Trim(),
                FacultyId = request.FacultyId,
                CreatedBy = request.CreatedBy,
                CreatedDateTime = DateTime.Now,
                IsDelete = false
            });
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Major Added",
                Description = $"{request.MajorName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return StatusCode(201, new MajorCreateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
            });
        }

        // PUT /api/major/{id}
        [HttpPut("{id}")]
        [AllowAnonymous]
        public IActionResult UpdateMajor(int id, [FromBody] MajorUpdateRequestModel request)
        {
            var item = _db.Majors.FirstOrDefault(x => x.MajorId == id && (x.IsDelete == false || x.IsDelete == null));
            if (item is null)
                return NotFound(new MajorUpdateResponseModel { IsSuccess = false, Message = "Major ကို ရှာမတွေ့ပါ။" });

            if (string.IsNullOrWhiteSpace(request.MajorName))
                return BadRequest(new MajorUpdateResponseModel { IsSuccess = false, Message = "Major Name ဖြည့်ရန် လိုအပ်ပါသည်။" });

            var faculty = _db.Faculties.FirstOrDefault(f => f.FacultyId == request.FacultyId && (f.IsDelete == false || f.IsDelete == null));
            if (faculty is null)
                return BadRequest(new MajorUpdateResponseModel { IsSuccess = false, Message = "ပေးထားသော Faculty မရှိပါ။" });

            var isDuplicate = _db.Majors.Any(x =>
                x.FacultyId == request.FacultyId &&
                x.MajorName.Trim().ToLower() == request.MajorName.Trim().ToLower() &&
                x.MajorId != id &&
                (x.IsDelete == false || x.IsDelete == null));
            if (isDuplicate)
                return BadRequest(new MajorUpdateResponseModel { IsSuccess = false, Message = $"'{request.MajorName}' ဟူသော Major သည် ဤ Faculty အောက်တွင် ရှိနှင့်ပြီးသားဖြစ်နေပါသည်။" });

            item.MajorName = request.MajorName.Trim();
            item.FacultyId = request.FacultyId;
            item.ModifiedBy = request.ModifiedBy;
            item.ModifiedDateTime = DateTime.Now;

            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Major Updated",
                Description = $"{request.MajorName} was updated in the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new MajorUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new MajorModel { MajorId = item.MajorId, MajorName = item.MajorName, FacultyId = item.FacultyId, FacultyName = faculty.FacultyName }
            });
        }

        // DELETE /api/major/{id}
        [HttpDelete("{id}")]
        [AllowAnonymous]
        public IActionResult DeleteMajor(int id)
        {
            var item = _db.Majors.FirstOrDefault(x => x.MajorId == id && (x.IsDelete == false || x.IsDelete == null));
            if (item is null)
                return NotFound(new MajorDeleteResponseModel { IsSuccess = false, Message = "Major ကို ရှာမတွေ့ပါ။" });

            item.IsDelete = true;
            item.ModifiedDateTime = DateTime.Now;

            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Major Deleted",
                Description = $"{item.MajorName} was deleted from the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new MajorDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : "ဖျက်သိမ်းမှု မအောင်မြင်ပါ။"
            });
        }

        [HttpGet("paginate")]
        [Permission("Major.View")]
        public IActionResult GetMajorsPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? facultyId = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Majors
                .AsNoTracking()
                .Include(x => x.Faculty)
                .Where(x => x.IsDelete == false || x.IsDelete == null);

            // Hierarchical RBAC Data Scoping:
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
                if (scopedFacultyId.HasValue)
                    query = query.Where(x => x.FacultyId == scopedFacultyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.MajorName != null && x.MajorName.Contains(searchTerm));
            }

            if (facultyId.HasValue && facultyId.Value > 0)
            {
                query = query.Where(x => x.FacultyId == facultyId.Value);
            }

            var totalCount = query.Count();

            var items = query
                .OrderByDescending(x => x.MajorId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new MajorModel
                {
                    MajorId = x.MajorId,
                    MajorName = x.MajorName,
                    FacultyId = x.FacultyId,
                    FacultyName = x.Faculty != null ? x.Faculty.FacultyName : "N/A"
                })
                .ToList();

            var result = new PagedResult<MajorModel>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }
    }
}
