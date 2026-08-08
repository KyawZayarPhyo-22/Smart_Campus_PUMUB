using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemesterController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public SemesterController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/semesters
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetSemesters()
        {
            var lst = _db.Semesters
                         .Where(x => x.IsDelete == false)
                         .OrderByDescending(x => x.SemesterId)
                         .ToList();
            return Ok(lst);
        }

        // GET /api/semesters/{id}
        [HttpGet("{id}")]
        public IActionResult GetSemester(int id)
        {
            var item = _db.Semesters.FirstOrDefault(x => x.SemesterId == id && x.IsDelete == false);
            if (item is null) return NotFound("Semester ကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/semesters
        [HttpPost]
        public IActionResult CreateSemester(SemesterCreateRequestModel request)
        {
            // Validation: နာမည်တူ စစ်ဆေးခြင်း
            if (_db.Semesters.Any(x => x.SemesterName == request.SemesterName && x.IsDelete == false))
            {
                return BadRequest(new SemesterCreateResponseModel { IsSuccess = false, Message = "Semester အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            _db.Semesters.Add(new Semester { SemesterName = request.SemesterName, Sequence = request.Sequence, IsDelete = false });
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "New Semester added",
                Description = $"{request.SemesterName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();
            return StatusCode(201, new SemesterCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/semesters/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateSemester(int id, SemesterUpdateRequestModel request)
        {
            var item = _db.Semesters.FirstOrDefault(x => x.SemesterId == id && x.IsDelete == false);
            if (item is null) return NotFound(new SemesterUpdateResponseModel { IsSuccess = false, Message = "Semester ကို ရှာမတွေ့ပါ။" });

            // Validation: အခြားနာမည်တူ ရှိမရှိ စစ်ဆေးခြင်း
            if (_db.Semesters.Any(x => x.SemesterName == request.SemesterName && x.SemesterId != id && x.IsDelete == false))
            {
                return BadRequest(new SemesterUpdateResponseModel { IsSuccess = false, Message = "Semester အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            item.SemesterName = request.SemesterName;
            item.Sequence = request.Sequence ?? item.Sequence;
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Semester Updated",
                Description = $"{request.SemesterName} was updated to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new SemesterUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new SemesterModel { SemesterId = item.SemesterId, SemesterName = item.SemesterName, Sequence = item.Sequence }
            });
        }

        // DELETE /api/semesters/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteSemester(int id)
        {
            var item = _db.Semesters
                .FirstOrDefault(x => x.SemesterId == id && x.IsDelete == false);

            if (item is null)
            {
                return NotFound(new SemesterDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "Semester ကို ရှာမတွေ့ပါ။"
                });
            }

            bool hasRelatedData = _db.Subjects.Any(x => x.SemesterId == id && x.IsDelete == false );

            if (hasRelatedData)
            {
                return BadRequest(new SemesterDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "ဤ Semester ကို Subject များအသုံးပြုထားသောကြောင့် ဖျက်၍ မရပါ။"
                });
            }

            item.IsDelete = true;
            int result = _db.SaveChanges();

            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Semester deleted",
                Description = $"{item.SemesterName} was deleted to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new SemesterDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0
                    ? "Semester ကို အောင်မြင်စွာ ဖျက်ပြီးပါပြီ။"
                    : "ဖျက်ခြင်း မအောင်မြင်ပါ။"
            });
        }

        [HttpGet("paginate")]
        [AllowAnonymous]
        public IActionResult GetSemestersPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Semesters
                .AsNoTracking()
                .Where(x => x.IsDelete == false || x.IsDelete == null);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.SemesterName != null && x.SemesterName.Contains(searchTerm));
            }

            var totalCount = query.Count();

            var items = query
                .OrderBy(x => x.SemesterId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PagedResult<Semester>
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


