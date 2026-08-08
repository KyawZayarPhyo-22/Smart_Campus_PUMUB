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
    public class PositionController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public PositionController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/positions (Read All - ဖျက်ထားတာမပါ)
        [HttpGet]
        [Permission("Position.View")]
        public IActionResult GetPositions()
        {
            var lst = _db.Positions
                         .Where(x => x.IsDelete == false) // Soft Delete ဖြစ်နေတာတွေကို မယူပါ
                         .OrderByDescending(x => x.PositionId)
                         .ToList();
            return Ok(lst);
        }

        // GET /api/positions/{id} (Read One)
        [HttpGet("{id}")]
        [Permission("Position.View")]
        public IActionResult GetPosition(int id)
        {
            var item = _db.Positions.FirstOrDefault(x => x.PositionId == id && x.IsDelete == false);
            if (item is null) return NotFound("ရာထူးကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/positions (Create)
        [HttpPost]
        [Permission("Position.Create")]
        public IActionResult CreatePosition(PositionCreateRequestModel request)
        {
            // Validation: နာမည်တူရှိမရှိ စစ်ဆေးခြင်း
            if (_db.Positions.Any(x => x.PositionName == request.PositionName && x.IsDelete == false))
            {
                return BadRequest(new PositionCreateResponseModel { IsSuccess = false, Message = "ရာထူးအမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            _db.Positions.Add(new Position { PositionName = request.PositionName, IsDelete = false });
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Position Added",
                Description = $"{request.PositionName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return StatusCode(201, new PositionCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/positions/{id} (Update)
        [HttpPut("{id}")]
        [Permission("Position.Edit")]
        public IActionResult UpdatePosition(int id, PositionUpdateRequestModel request)
        {
            var item = _db.Positions.FirstOrDefault(x => x.PositionId == id && x.IsDelete == false);
            if (item is null) return NotFound(new PositionUpdateResponseModel { IsSuccess = false, Message = "ရာထူးကို ရှာမတွေ့ပါ။" });

            // Validation: အခြားနာမည်တူ ရှိမရှိ (မိမိကိုယ်တိုင်မှလွဲ၍)
            if (_db.Positions.Any(x => x.PositionName == request.PositionName && x.PositionId != id && x.IsDelete == false))
            {
                return BadRequest(new PositionUpdateResponseModel { IsSuccess = false, Message = "ရာထူးအမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            item.PositionName = request.PositionName;
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Position Updated",
                Description = $"{request.PositionName} was updated to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new PositionUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new PositionModel { PositionId = item.PositionId, PositionName = item.PositionName }
            });
        }

        // DELETE /api/positions/{id} (Soft Delete)
        [HttpDelete("{id}")]
        [Permission("Position.Delete")]
        public IActionResult DeletePosition(int id)
        {
            // Position ရှိမရှိ စစ်ဆေး
            var item = _db.Positions.FirstOrDefault(x => x.PositionId == id && x.IsDelete == false);

            if (item is null)
            {
                return NotFound(new PositionDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "ရာထူးကို ရှာမတွေ့ပါ။"
                });
            }

            bool hasUsers = _db.Tutors.Any(x => x.PositionId == id && x.IsDelete == false);

            if (hasUsers)
            {
                return BadRequest(new PositionDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "ဤရာထူးကို အသုံးပြုနေသူများ ရှိနေသောကြောင့် ဖျက်၍ မရပါ။"
                });
            }

            // Soft Delete အလုပ်လုပ်ပုံ
            item.IsDelete = true;

            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Position Deleted",
                Description = $"{item.PositionName} was deleted from the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new PositionDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ရာထူးဖျက်ခြင်း အောင်မြင်ပါသည်။" : "ရာထူးဖျက်ခြင်း မအောင်မြင်ပါ။"
            });
        }

        [HttpGet("paginate")]
        [AllowAnonymous]
        public IActionResult GetPositionsPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Positions
                .AsNoTracking()
                .Where(x => x.IsDelete == false || x.IsDelete == null);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.PositionName != null && x.PositionName.Contains(searchTerm));
            }

            var totalCount = query.Count();

            var items = query
                .OrderBy(x => x.PositionId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PositionModel
                {
                    PositionId = x.PositionId,
                    PositionName = x.PositionName
                })
                .ToList();

            var result = new PagedResult<PositionModel>
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

