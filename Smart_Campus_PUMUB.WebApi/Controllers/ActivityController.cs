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
    //[Authorize]

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
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                IsDelete = false
            });
            _db.SaveChanges();
        }

        //[Authorize]
        // GET /api/activities
        [HttpGet]
        [Permission("Activity.View")]
        [AllowAnonymous]
        public IActionResult GetActivities()
        {
            // Query active activities from DB and filter in memory
            var lst = _db.Activities
                             .AsNoTracking()
                             .Where(x => x.IsDelete == false)
                             .ToList() // Load to memory
                             .Where(x => !IsSystemLog(x.ActivityTitle))
                             .OrderByDescending(x => x.CreatedDateTime) // Sort latest first
                             .Select(x => new
                             {
                                 x.ActivityId,
                                 x.ActivityTitle,
                                 x.Description,
                                 x.Image,
                                 x.Location,
                                 CreatedAt = x.CreatedDateTime // Model mapping
                             })
                             .ToList();

            return Ok(lst);
        }

        private bool IsSystemLog(string title)
        {
            if (string.IsNullOrEmpty(title)) return false;

            var t = title.ToLower();

            var logKeywords = new List<string>
            {
                "uploaded", "updated", "deleted", "added", "removed", "registered", "toggled", "created",
                "department", "position", "book", "role", "student", "semester", "tutor", "user", "account", "login", "category", "faculty", "subject", "payment", "rule", "fee"
            };

            return logKeywords.Any(k => t.Contains(k));
        }

        // GET /api/activities/{id}
        [HttpGet("{id}")]
        public IActionResult GetActivity(int id)
        {
            var item = _db.Activities.FirstOrDefault(x => x.ActivityId == id && x.IsDelete == false);
            if (item is null) return NotFound("Activity not found.");
            return Ok(item);
        }


        [HttpPost]
        [Permission("Activity.Create")]
        public async Task<IActionResult> CreateActivity([FromForm] ActivityCreateRequestModel request)
        {
            var uploadedPaths = new List<string>();
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var filesToProcess = new List<IFormFile>();
            if (request.ImageFiles != null && request.ImageFiles.Count > 0)
            {
                filesToProcess.AddRange(request.ImageFiles.Where(f => f.Length > 0));
            }
            if (request.ImageFile != null && request.ImageFile.Length > 0 && !filesToProcess.Contains(request.ImageFile))
            {
                filesToProcess.Add(request.ImageFile);
            }

            foreach (var file in filesToProcess)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                uploadedPaths.Add("/uploads/" + fileName);
            }

            string? finalImageValue = null;
            if (uploadedPaths.Count == 1)
            {
                finalImageValue = uploadedPaths[0];
            }
            else if (uploadedPaths.Count > 1)
            {
                finalImageValue = System.Text.Json.JsonSerializer.Serialize(uploadedPaths);
            }

            var activityDate = request.ActivityDate ?? DateTime.UtcNow.AddHours(6).AddMinutes(30);

            _db.Activities.Add(new Activity
            {
                ActivityTitle = request.ActivityTitle,
                Image = finalImageValue,
                Description = request.Description,
                Location = request.Location,
                CreatedDateTime = activityDate,
                IsDelete = false
            });

            await _db.SaveChangesAsync();
            AddActivityLog("New Activity Uploaded", $"{request.ActivityTitle} was added.");
            return StatusCode(201, new { IsSuccess = true, Message = "Saving Successful" });
        }


        [HttpPost("update/{id}")]
        [Permission("Activity.Edit")]
        public async Task<IActionResult> UpdateActivity(int id, [FromForm] ActivityUpdateRequestModel request)
        {
            var item = _db.Activities.FirstOrDefault(x => x.ActivityId == id && x.IsDelete == false);
            if (item is null) return NotFound(new { IsSuccess = false, Message = "Activity not found" });

            var finalPaths = new List<string>();

            // Parse remaining existing images if provided
            if (!string.IsNullOrEmpty(request.ExistingImagesJson))
            {
                try
                {
                    var existingList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(request.ExistingImagesJson);
                    if (existingList != null) finalPaths.AddRange(existingList.Where(s => !string.IsNullOrWhiteSpace(s)));
                }
                catch { }
            }
            else if (!string.IsNullOrEmpty(request.Image))
            {
                finalPaths.AddRange(ActivityModel.GetImageList(request.Image));
            }

            // Save new uploaded images
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var filesToProcess = new List<IFormFile>();
            if (request.ImageFiles != null && request.ImageFiles.Count > 0)
            {
                filesToProcess.AddRange(request.ImageFiles.Where(f => f.Length > 0));
            }
            if (request.ImageFile != null && request.ImageFile.Length > 0 && !filesToProcess.Contains(request.ImageFile))
            {
                filesToProcess.Add(request.ImageFile);
            }

            foreach (var file in filesToProcess)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                finalPaths.Add("/uploads/" + fileName);
            }

            if (finalPaths.Count == 1)
            {
                item.Image = finalPaths[0];
            }
            else if (finalPaths.Count > 1)
            {
                item.Image = System.Text.Json.JsonSerializer.Serialize(finalPaths);
            }
            else if (filesToProcess.Count > 0 || !string.IsNullOrEmpty(request.ExistingImagesJson))
            {
                item.Image = null;
            }

            item.ActivityTitle = request.ActivityTitle ?? item.ActivityTitle;
            item.Description = request.Description ?? item.Description;
            item.Location = request.Location ?? item.Location;
            if (request.ActivityDate.HasValue)
            {
                item.CreatedDateTime = request.ActivityDate.Value;
            }

            await _db.SaveChangesAsync();
            AddActivityLog("Activity Updated", $"{item.ActivityTitle} was updated to the Activity.");

            return Ok(new { IsSuccess = true, Message = "Activity Update Successfully" });
        }


        // DELETE /api/activities/{id}
        [HttpDelete("{id}")]
        [Permission("Activity.Delete")]
        public IActionResult DeleteActivity(int id)
        {
            var item = _db.Activities.FirstOrDefault(x => x.ActivityId == id && x.IsDelete == false);
            if (item is null) return NotFound(new ActivityDeleteResponseModel { IsSuccess = false, Message = "Activity not found." });

            // Soft Delete
            item.IsDelete = true;
            int result = _db.SaveChanges();
            AddActivityLog("Activity Deleted", $"{item.ActivityTitle} was deleted to the Activity.");

            return Ok(new ActivityDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Deleted successfully." : "Deletion failed."
            });
        }

        [HttpGet("count/active")]
        public IActionResult GetActivityCount()
        {
            // 1. Query all active activities
            var activeActivities = _db.Activities
                                   .AsNoTracking()
                                   .Where(x => x.IsDelete == false)
                                   .ToList();

            // 2. Count non-system logs
            int count = activeActivities.Count(x => !IsSystemLog(x.ActivityTitle));

            return Ok(new { Count = count });
        }

    [HttpGet("locations")]
    [AllowAnonymous]
    public IActionResult GetLocations()
    {
        var locations = _db.Activities
            .AsNoTracking()
            .Where(x => (x.IsDelete == false || x.IsDelete == null) && x.Location != null && x.Location != "")
            .Select(x => x.Location)
            .Distinct()
            .ToList();
        return Ok(locations);
    }

    [HttpGet("paginate")]
    [AllowAnonymous]
    public IActionResult GetActivitiesPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? location = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var allActivities = _db.Activities
            .AsNoTracking()
            .Where(x => x.IsDelete == false || x.IsDelete == null)
            .ToList();

        // Exclude system audit logs
        var filtered = allActivities
            .Where(x => !IsSystemLog(x.ActivityTitle));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            filtered = filtered.Where(x => (x.ActivityTitle != null && x.ActivityTitle.ToLower().Contains(term)) ||
                                           (x.Description != null && x.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(location) && !location.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(x => x.Location != null && x.Location.Equals(location, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = filtered.Count();

        var items = filtered
            .OrderByDescending(x => x.CreatedDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ActivityModel
            {
                ActivityId = x.ActivityId,
                ActivityTitle = x.ActivityTitle,
                Image = x.Image,
                Description = x.Description,
                Location = x.Location
            })
            .ToList();

        var result = new PagedResult<ActivityModel>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(result);
    }

    [HttpGet("recent")]
    public IActionResult GetRecentActivities()
    {
        // Query non-deleted activities and filter system logs in memory
        var allActivities = _db.Activities
                             .AsNoTracking()
                             .Where(x => x.IsDelete == false || x.IsDelete == null)
                             .OrderByDescending(x => x.CreatedDateTime)
                             .ToList();

        // Include system logs only for audit display
        var recentActivities = allActivities
            .Where(x => IsSystemLog(x.ActivityTitle ?? ""))
            .Take(6)
            .Select(x => new
            {
                ActivityId = x.ActivityId,
                ActivityTitle = x.ActivityTitle,
                Description = x.Description,
                CreatedDateTime = x.CreatedDateTime,
                CreatedBy = !string.IsNullOrWhiteSpace(x.CreatedBy) ? x.CreatedBy : "Admin",
                Icon = GetIconByActivityType(x.ActivityTitle ?? "")
            }).ToList();

        return Ok(recentActivities);
    }
        // Helper method to resolve icon by activity type
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
