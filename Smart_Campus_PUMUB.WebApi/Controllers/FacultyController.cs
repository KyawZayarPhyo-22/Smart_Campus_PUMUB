using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class FacultyController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;
        private readonly IFacultyDataScopeService _scopeService;

        public FacultyController(SmartCampusDbContext db, IFacultyDataScopeService scopeService)
        {
            _db = db;
            _scopeService = scopeService;
        }

        // GET /api/faculties
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetFaculties()
        {
            var query = _db.Faculties
                         .AsNoTracking()
                         .Include(x => x.Departments)
                         .ThenInclude(d => d.Tutors)
                         .ThenInclude(t => t.Position)
                         .Where(x => x.IsDelete == false || x.IsDelete == null);

            // Hierarchical RBAC Data Scoping:
            // Super Admin sees ALL faculties. Faculty Admin sees ONLY their assigned faculty.
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
                if (scopedFacultyId.HasValue)
                {
                    query = query.Where(x => x.FacultyId == scopedFacultyId.Value);
                }
            }

            var lst = query
                .OrderBy(x => x.FacultyId)
                .Select(x => new FacultyModel
                {
                    FacultyId = x.FacultyId,
                    FacultyName = x.FacultyName
                })
                .ToList();
            return Ok(lst);
        }

        // GET /api/faculties/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public IActionResult GetFaculty(int id)
        {
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                if (!_scopeService.CanAccessFaculty(User, id))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { IsSuccess = false, Message = "ဒီ Faculty ဒေတာကို ကြည့်ရှုခွင့် မရှိပါ။" });
                }
            }

            var item = _db.Faculties.Include(x => x.Departments)
                         .ThenInclude(d => d.Tutors)
                         .ThenInclude(t => t.Position)
                         .FirstOrDefault(x => x.FacultyId == id && (x.IsDelete == false || x.IsDelete == null));

            if (item is null) return NotFound("Faculty ကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/faculties
        [HttpPost]
        [AllowAnonymous]
        public IActionResult CreateFaculty(FacultyCreateRequestModel request)
        {
            // Hierarchical RBAC: Only Super Admin can create new Faculties
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FacultyCreateResponseModel { IsSuccess = false, Message = "Super Admin သာလျှင် Faculty အသစ် ဖန်တီးခွင့် ရှိပါသည်။" });
            }

            // Validation: Faculty Name တူနေခြင်း ရှိ/မရှိ စစ်ဆေးခြင်း
            if (_db.Faculties.Any(x => x.FacultyName == request.FacultyName && (x.IsDelete == false || x.IsDelete == null)))
            {
                return BadRequest(new FacultyCreateResponseModel { IsSuccess = false, Message = "Faculty အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            _db.Faculties.Add(new Faculty { FacultyName = request.FacultyName, IsDelete = false, CreatedDateTime = DateTime.Now });
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Faculty Added",
                Description = $"{request.FacultyName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return StatusCode(201, new FacultyCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/faculties/{id}
        [HttpPut("{id}")]
        [AllowAnonymous]
        public IActionResult UpdateFaculty(int id, FacultyUpdateRequestModel request)
        {
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                if (!_scopeService.CanAccessFaculty(User, id))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new FacultyUpdateResponseModel { IsSuccess = false, Message = "ဒီ Faculty ဒေတာကို ပြင်ဆင်ခွင့် မရှိပါ။" });
                }
            }

            var item = _db.Faculties.FirstOrDefault(x => x.FacultyId == id && (x.IsDelete == false || x.IsDelete == null));
            if (item is null) return NotFound(new FacultyUpdateResponseModel { IsSuccess = false, Message = "Faculty ကို ရှာမတွေ့ပါ။" });

            if (_db.Faculties.Any(x => x.FacultyName == request.FacultyName && x.FacultyId != id && (x.IsDelete == false || x.IsDelete == null)))
            {
                return BadRequest(new FacultyUpdateResponseModel { IsSuccess = false, Message = "Faculty အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            item.FacultyName = request.FacultyName;
            item.ModifiedDateTime = DateTime.Now;
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Faculty Updated",
                Description = $"{request.FacultyName} was updated to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new FacultyUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new FacultyModel { FacultyId = item.FacultyId, FacultyName = item.FacultyName }
            });
        }

        // DELETE /api/faculties/{id}
        [HttpDelete("{id}")]
        [AllowAnonymous]
        public IActionResult DeleteFaculty(int id)
        {
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new FacultyDeleteResponseModel { IsSuccess = false, Message = "Super Admin သာလျှင် Faculty ဖျက်ဆီးခွင့် ရှိပါသည်။" });
            }

            var item = _db.Faculties
                .FirstOrDefault(x => x.FacultyId == id && (x.IsDelete == false || x.IsDelete == null));

            if (item is null)
            {
                return NotFound(new FacultyDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "Faculty ကို ရှာမတွေ့ပါ။"
                });
            }

            var hasDepartments = _db.Departments
                .Any(x => x.FacultyId == id && (x.IsDelete == false || x.IsDelete == null));

            if (hasDepartments)
            {
                return BadRequest(new FacultyDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "ဒီ Faculty ကို Department တွေက အသုံးပြုနေပါတယ်။ ဖျက်လို့မရပါ။"
                });
            }

            item.IsDelete = true;
            item.ModifiedDateTime = DateTime.Now;

            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Faculty Deleted",
                Description = $"{item.FacultyName} was deleted from the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new FacultyDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0
                    ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။"
                    : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
            });
        }

        [HttpGet("paginate")]
        [AllowAnonymous]
        public IActionResult GetFacultiesPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Faculties
                .AsNoTracking()
                .Where(x => x.IsDelete == false || x.IsDelete == null);

            // Hierarchical RBAC Data Scoping:
            if (User?.Identity?.IsAuthenticated == true && !_scopeService.IsSuperAdmin(User))
            {
                var scopedFacultyId = _scopeService.GetScopedFacultyId(User);
                if (scopedFacultyId.HasValue)
                {
                    query = query.Where(x => x.FacultyId == scopedFacultyId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.FacultyName != null && x.FacultyName.Contains(searchTerm));
            }

            var totalCount = query.Count();

            var items = query
                .OrderBy(x => x.FacultyId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedResult<Faculty>
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
