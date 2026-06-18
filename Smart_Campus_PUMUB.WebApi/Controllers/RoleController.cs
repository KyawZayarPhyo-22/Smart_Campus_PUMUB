using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public RoleController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/roles
        [HttpGet]
        public IActionResult GetRoles()
        {
            var lst = _db.Roles
                .Where(r => r.IsDelete == false)
                .Select(r => new
                {
                    r.RoleId,
                    r.RoleName,
                    Users = r.Users
                        .Where(u => u.IsDelete == false)
                        .Select(u => new
                        {
                            u.UserId,
                            u.FullName,
                            u.UserName
                        })
                        .ToList()
                })
                .OrderByDescending(r => r.RoleId)
                .ToList();

            return Ok(lst);
        }

        // GET /api/roles/{id}
        [HttpGet("{id}")]
        public IActionResult GetRole(int id)
        {
            var item = _db.Roles.FirstOrDefault(x => x.RoleId == id);
            if (item is null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        // POST /api/roles
        [HttpPost]
        public IActionResult CreateRole(RoleCreateRequestModel request)
        {
            // Validation: Check if RoleName already exists
            var exists = _db.Roles.Any(r => r.RoleName == request.RoleName);
            if (exists)
            {
                return BadRequest(new RoleCreateResponseModel
                {
                    IsSuccess = false,
                    Message = "Role အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။"
                });
            }

            _db.Roles.Add(new Role
            {
                RoleName = request.RoleName
            });
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "New Role Registered",
                Description = $"{request.RoleName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
            });
            _db.SaveChanges();

            return StatusCode(201, new RoleCreateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
            });
        }

        // PUT /api/roles/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateRole(RoleUpdateRequestModel request, int id)
        {
            var item = _db.Roles.FirstOrDefault(x => x.RoleId == id);
            if (item is null)
            {
                return NotFound(new RoleUpdateResponseModel
                {
                    IsSuccess = false,
                    Message = "Role not found"
                });
            }

            // Validation: Check if RoleName exists in OTHER records
            var exists = _db.Roles.Any(r => r.RoleName == request.RoleName && r.RoleId != id);
            if (exists)
            {
                return BadRequest(new RoleUpdateResponseModel
                {
                    IsSuccess = false,
                    Message = "Role အမည် ရှိနှင့်ပြီးသား ဖြစ်နေသည်။"
                });
            }

            item.RoleName = request.RoleName;

            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = " Role updated",
                Description = $"{request.RoleName} was updated to the System.",
                CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
            });
            _db.SaveChanges();
            return Ok(new RoleUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Role အချက်အလက် ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "Role အချက်အလက် ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new RoleModel
                {
                    RoleId = item.RoleId,
                    RoleName = item.RoleName
                }
            });
        }
        [HttpDelete("{id}")]
        public IActionResult DeleteRole(int id)
        {
            // Role ရှိမရှိ စစ်ဆေး
            var item = _db.Roles.FirstOrDefault(x => x.RoleId == id && x.IsDelete == false);

            if (item is null)
            {
                return NotFound(new RoleDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "Role ကို ရှာမတွေ့ပါ။"
                });
            }

            // ဒီ Role ကို အသုံးပြုနေတဲ့ User ရှိမရှိ စစ်ဆေး
            bool hasUsers = _db.Users.Any(x => x.RoleId == id && x.IsDelete == false);

            if (hasUsers)
            {
                return BadRequest(new RoleDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "ဤ Role ကို User များက အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။"
                });
            }

            // Soft Delete
            item.IsDelete = true;

            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = " Role deleted",
                Description = $"{item.RoleName} was delete to the System.",
                CreatedDateTime = DateTime.UtcNow // အချိန်မှန်အောင် UtcNow သုံးပါ
            });
            _db.SaveChanges();

            return Ok(new RoleDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0
                    ? "Role ဖျက်ခြင်း အောင်မြင်ပါသည်။"
                    : "Role ဖျက်ခြင်း မအောင်မြင်ပါ။"
            });
        }
    }
}
