using Microsoft.AspNetCore.Authorization;
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
        private void AddActivityLog(string title, string description)
        {
            _db.Activities.Add(new Activity
            {
                ActivityTitle = title,
                Description = description,
                CreatedDateTime = DateTime.UtcNow,
                IsDelete = false
            });
            _db.SaveChanges();
        }

        //[Authorize]
        // GET /api/activities
        [HttpGet]
        public IActionResult GetActivities()
        {
            // Database ကနေ Data အရင်ဆွဲထုတ်ပြီး မှတ်ဉာဏ်ထဲမှာ စစ်မယ်
            var lst = _db.Activities
                             .AsNoTracking()
                             .Where(x => x.IsDelete == false)
                             .ToList() // Memory ထဲဆွဲထုတ်ခြင်း
                             .Where(x => !IsSystemLog(x.ActivityTitle))
                             .OrderByDescending(x => x.CreatedDateTime) // 🌟 အသစ်ဆုံးကို အရင်ပြရန်
                             .Select(x => new
                             {
                                 x.ActivityId,
                                 x.ActivityTitle,
                                 x.Description,
                                 x.Image,
                                 x.Location,
                                 CreatedAt = x.CreatedDateTime // 🌟 Model မှာ CreatedAt လို့သုံးထားရင် ဒီလို Mapping လုပ်ပါ
                             })
                             .ToList();

            return Ok(lst);
        }

        private bool IsSystemLog(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;

            // စာသားအားလုံးကို lowercase ပြောင်းလိုက်ပါ
            var t = title.ToLower();

            // Log လို သတ်မှတ်ထားတဲ့ စာသားအပိုင်းအစများ (Keywords)
            var logKeywords = new List<string>
    {
        "uploaded", "updated", "deleted", "added","removed",
        "department", "position", "book", "role", "student", "semester"
    };

            // Title ထဲမှာ အပေါ်က စာလုံးတွေထဲက တစ်ခုခု ပါနေရင် System Log လို သတ်မှတ်မယ်
            return logKeywords.Any(k => t.Contains(k));
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
            AddActivityLog("New Activity Uploaded", $"{request.ActivityTitle} was added.");
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
            AddActivityLog("Activity Updated", $"{item.ActivityTitle} was updated to the Activity.");

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
            AddActivityLog("Activity Deleted", $"{item.ActivityTitle} was deleted to the Activity.");

            return Ok(new ActivityDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။" : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
            });
        }

        [HttpGet("count/active")]
        public IActionResult GetActivityCount()
        {
            // ၁။ Delete မဖြစ်သေးသော Activity အားလုံးကို အရင်ယူပါ
            var activeActivities = _db.Activities
                                   .AsNoTracking()
                                   .Where(x => x.IsDelete == false)
                                   .ToList();

            // ၂။ System Log များ မပါဝင်သော Activity အရေအတွက်ကိုသာ ရေတွက်ပါ
            int count = activeActivities.Count(x => !IsSystemLog(x.ActivityTitle));

            return Ok(new { Count = count });
        }

        [HttpGet("recent")]
        public IActionResult GetRecentActivities()
        {
            // ၁။ Database မှ Data ကို အရင်ဆွဲထုတ်ပါ (ToList() သုံးလိုက်သည့်အတွက် Query ပြီးဆုံးသွားသည်)
            var activities = _db.Activities
                                 .AsNoTracking()
                                 .Where(x => x.IsDelete == false
                                 && x.ActivityTitle != "New Activity Uploaded"
                                && x.ActivityTitle != "Activity Updated"
                                && x.ActivityTitle != "Activity deleted")
                                 .OrderByDescending(x => x.CreatedDateTime)
                                 .Take(5)
                                 .ToList(); // 👈 ဒီနေရာမှာ Memory ထဲရောက်သွားပြီ

            // ၂။ Memory ထဲရောက်မှ Icon ကို Mapping လုပ်ပါ
            var recentActivities = activities.Select(x => new
            {
                ActivityTitle = x.ActivityTitle,
                Description = x.Description,
                CreatedDateTime = x.CreatedDateTime,
                Icon = GetIconByActivityType(x.ActivityTitle) // 👈 အခုဆိုရင် Error မတက်တော့ပါ
            }).ToList();

            return Ok(recentActivities);
        }
        // Activity အမျိုးအစားအလိုက် Icon သတ်မှတ်ပေးမည့် Helper Method
        private string GetIconByActivityType(string title)
        {
            title = title.ToLower();
            if (title.Contains("book")) return "bi-book";
            if (title.Contains("student")) return "bi-person";
            if (title.Contains("tutor")) return "bi-person-badge";
            if (title.Contains("role")) return "bi-shield-lock";
            if (title.Contains("position")) return "bi-briefcase";
            if (title.Contains("faculty")) return "bi-building";
            if (title.Contains("semester")) return "bi-calendar3";
            if (title.Contains("category")) return "bi-tags";
            if (title.Contains("department")) return "bi-diagram-3";
            if (title.Contains("subject")) return "bi-journal-bookmark";
            if (title.Contains("rule")) return "bi-file-earmark-text";

            return "bi-info-circle";
        }


    }


}