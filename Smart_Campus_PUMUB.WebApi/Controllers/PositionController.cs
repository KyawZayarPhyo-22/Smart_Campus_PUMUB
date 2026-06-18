using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext;
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
        public IActionResult GetPosition(int id)
        {
            var item = _db.Positions.FirstOrDefault(x => x.PositionId == id && x.IsDelete == false);
            if (item is null) return NotFound("ရာထူးကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/positions (Create)
        [HttpPost]
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
                ActivityTitle = "New Position added",
                Description = $"{request.PositionName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
            });
            _db.SaveChanges();

            return StatusCode(201, new PositionCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/positions/{id} (Update)
        [HttpPut("{id}")]
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
                ActivityTitle = " Position updated",
                Description = $"{request.PositionName} was updated to the System.",
                CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
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

            // ဒီ Position ကို အသုံးပြုနေတဲ့ သူ (Employee/User) ရှိမရှိ စစ်ဆေး
            // Database Table အမည်ကို လိုအပ်သလို ပြင်သုံးပေးပါ (ဥပမာ - _db.Employees သို့မဟုတ် _db.Users)
            // x.Tutors ထဲမှာ ကိုကိုစစ်ချင်တဲ့ id ပါသလားဆိုတာကို .Any() နဲ့ ထပ်စစ်ပေးတာပါ
            bool hasUsers = _db.Tutors.Any(x => x.TutorId == id);

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
                ActivityTitle = " Position deleted",
                Description = $"{item.PositionName} was deleted to the System.",
                CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
            });
            _db.SaveChanges();

            return Ok(new PositionDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ရာထူးဖျက်ခြင်း အောင်မြင်ပါသည်။" : "ရာထူးဖျက်ခြင်း မအောင်မြင်ပါ။"
            });
        }
    }
}