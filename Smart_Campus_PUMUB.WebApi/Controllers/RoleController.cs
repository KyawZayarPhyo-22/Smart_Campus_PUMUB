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
    public class RoleController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public RoleController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/roles

        [HttpGet]
        [Permission("Role.View")]
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
        [Permission("Role.View")]
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
        [Permission("Role.Create")]
        public IActionResult CreateRole(RoleCreateRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.RoleName))
            {
                return BadRequest(new RoleCreateResponseModel
                {
                    IsSuccess = false,
                    Message = "Role Name ဖြည့်ရန် လိုအပ်ပါသည်။"
                });
            }

            var roleName = request.RoleName.Trim();

            // Soft-deleted ဖြစ်နေပါက ပြန်ဖွင့်ပေးရန်
            var deletedRole = _db.Roles.FirstOrDefault(r => r.IsDelete == true && r.RoleName.ToLower() == roleName.ToLower());
            if (deletedRole != null)
            {
                deletedRole.IsDelete = false;
                deletedRole.RoleName = roleName;
                _db.SaveChanges();

                _db.Activities.Add(new Activity
                {
                    ActivityTitle = "New Role Registered",
                    Description = $"{roleName} was added to the System.",
                    CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
                });
                _db.SaveChanges();

                return StatusCode(201, new RoleCreateResponseModel
                {
                    IsSuccess = true,
                    Message = "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။"
                });
            }

            // Validation: Check if active RoleName already exists
            var exists = _db.Roles.Any(r => (r.IsDelete == false || r.IsDelete == null) && r.RoleName.ToLower() == roleName.ToLower());
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
                RoleName = roleName,
                IsDelete = false
            });
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "New Role Registered",
                Description = $"{roleName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
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
        [Permission("Role.Edit")]
        public IActionResult UpdateRole(RoleUpdateRequestModel request, int id)
        {
            var item = _db.Roles.FirstOrDefault(x => x.RoleId == id && (x.IsDelete == false || x.IsDelete == null));
            if (item is null)
            {
                return NotFound(new RoleUpdateResponseModel
                {
                    IsSuccess = false,
                    Message = "Role not found"
                });
            }

            // Validation: Check if RoleName exists in OTHER active records
            var exists = _db.Roles.Any(r => (r.IsDelete == false || r.IsDelete == null) && r.RoleName.ToLower() == request.RoleName.Trim().ToLower() && r.RoleId != id);
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
                ActivityTitle = "Role Updated",
                Description = $"{request.RoleName} was updated to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
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
        [Permission("Role.Delete")]
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
                ActivityTitle = "Role Deleted",
                Description = $"{item.RoleName} was deleted from the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
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

        [HttpGet("paginate")]
        [AllowAnonymous]
        public IActionResult GetRolesPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Roles
                .AsNoTracking()
                .Where(x => x.IsDelete == false || x.IsDelete == null);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.RoleName != null && x.RoleName.Contains(searchTerm));
            }

            var totalCount = query.Count();

            var items = query
                .OrderBy(x => x.RoleId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new RoleModel
                {
                    RoleId = x.RoleId,
                    RoleName = x.RoleName
                })
                .ToList();

            var result = new PagedResult<RoleModel>
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


