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
            // Deleted ဖြစ်မနေတဲ့ Data တွေကိုပဲ ယူပါ
            var lst = _db.Roles
                         .Where(r => r.IsDelete == false)
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
            // ဖျက်ပြီးသား မဟုတ်တဲ့ Item တွေကိုပဲ ရှာဖွေပါ
            var item = _db.Roles.FirstOrDefault(x => x.RoleId == id && x.IsDelete == false);

            if (item is null)
            {
                return NotFound(new RoleDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "Role not found or already deleted"
                });
            }

            // Soft Delete Logic: Row ကို မဖျက်ဘဲ Status ကိုပဲ ပြောင်းလိုက်တာပါ
            item.IsDelete = true;

            int result = _db.SaveChanges();

            return Ok(new RoleDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Delete Successfully " : "Delete Failed"
            });
        }
    }
}
