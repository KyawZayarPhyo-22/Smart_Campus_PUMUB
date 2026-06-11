using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            string? imagePath = null;

            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                // 1. File နာမည်အသစ်ပေးခြင်း (Random name)
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ImageFile.FileName);

                // 2. သိမ်းမည့် လမ်းကြောင်း (wwwroot/uploads)
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                // 3. File ကို Save လုပ်ခြင်း
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }

                // Database ထဲမှာ File Path (သို့) ဖိုင်နာမည်ကို သိမ်းပါ
                imagePath = "/uploads/" + fileName;
            }

            _db.Activities.Add(new Activity
            {
                ActivityTitle = request.ActivityTitle,
                Image = imagePath, // Path ကိုပဲ သိမ်းတာပါ
                Description = request.Description,
                Location = request.Location,
                IsDelete = false
            });

            await _db.SaveChangesAsync();
            return StatusCode(201, new { Message = "Saving Successful" });
        }

        [HttpPut("{id}")]
        public IActionResult UpdateActivity(int id, ActivityUpdateRequestModel request)
        {
            var item = _db.Activities.FirstOrDefault(x => x.ActivityId == id && x.IsDelete == false);
            if (item is null) return NotFound(new ActivityUpdateResponseModel { IsSuccess = false, Message = "Activity not found" });

            item.ActivityTitle = request.ActivityTitle;
            item.Image = request.Image;
            item.Description = request.Description;
            item.Location = request.Location;

            int result = _db.SaveChanges();

            return Ok(new ActivityUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Activity Update Successfully" : "Activity Update Failed",
                Data = new ActivityModel
                {
                    ActivityId = item.ActivityId,
                    ActivityTitle = item.ActivityTitle,
                    Image = item.Image,
                    Description = item.Description,
                    Location = item.Location
                }
            });
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