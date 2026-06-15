using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public ActivityController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/activities
        [HttpGet]
        public IActionResult GetActivities()
        {
            var lst = _db.Activities
                         .AsNoTracking()
                         .Where(x => x.IsDelete == false)
                         .OrderByDescending(x => x.ActivityId)
                         .ToList();
            return Ok(lst);
        }

        // GET /api/activities/{id}
        [HttpGet("{id}")]
        public IActionResult GetActivity(int id)
        {
            var item = _db.Activities.FirstOrDefault(x => x.ActivityId == id && x.IsDelete == false);
            if (item is null) return NotFound("သတင်း/လှုပ်ရှားမှုအား ရှာမတွေ့ပါ။");
            return Ok(item);
        }


        [HttpPost]
        public async Task<IActionResult> CreateActivity([FromForm] ActivityCreateRequestModel request)
        {
            // 💡 Title တူတာကို စစ်ဆေးတဲ့ if condition မပါတော့တဲ့အတွက် 
            // Title တူရင်လည်း အောင်မြင်စွာ သိမ်းဆည်းနိုင်ပါပြီ။

            string? imagePath = null;

            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ImageFile.FileName);

                // ဤလမ်းကြောင်းမှာ အရေးကြီးပါသည်
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }

                // ဤ path သည် URL အတွက်ဖြစ်သည်
                imagePath = "/uploads/" + fileName;
            }

            _db.Activities.Add(new Activity
            {
                ActivityTitle = request.ActivityTitle,
                Image = imagePath,
                Description = request.Description,
                Location = request.Location,
                IsDelete = false
            });

            await _db.SaveChangesAsync();
            return StatusCode(201, new { IsSuccess = true, Message = "Saving Successful" });
        }

  
        [HttpPost("update/{id}")] // Route ကိုသီးသန့်ခွဲထားပါ
        public async Task<IActionResult> UpdateActivity(int id, [FromForm] ActivityUpdateRequestModel request)
        {
            var item = _db.Activities.FirstOrDefault(x => x.ActivityId == id && x.IsDelete == false);
            if (item is null) return NotFound(new { IsSuccess = false, Message = "Activity not found" });

            // ပုံအသစ်တင်လျှင် အစားထိုးခြင်း
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ImageFile.FileName);
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }
                item.Image = "/uploads/" + fileName;
            }

            item.ActivityTitle = request.ActivityTitle ?? item.ActivityTitle;
            item.Description = request.Description ?? item.Description;
            item.Location = request.Location ?? item.Location;

            await _db.SaveChangesAsync();

            return Ok(new { IsSuccess = true, Message = "Activity Update Successfully" });
        }


        // DELETE /api/activities/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteActivity(int id)
        {
            var item = _db.Activities.FirstOrDefault(x => x.ActivityId == id && x.IsDelete == false);
            if (item is null) return NotFound(new ActivityDeleteResponseModel { IsSuccess = false, Message = "သတင်း/လှုပ်ရှားမှုအား ရှာမတွေ့ပါ။" });

            // Soft Delete
            item.IsDelete = true;
            int result = _db.SaveChanges();

            return Ok(new ActivityDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။" : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
            });
        }
    }


}