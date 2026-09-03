using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public PermissionController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/permission
        [HttpGet]
        public IActionResult GetPermissions()
        {
            var list = _db.Permissions
                          .AsNoTracking()
                          .OrderBy(x => x.PermissionId)
                          .Select(x => new PermissionModel
                          {
                              PermissionId = x.PermissionId,
                              PermissionName = x.PermissionName
                          })
                          .ToList();
            return Ok(list);
        }

        // GET /api/permission/{id}
        [HttpGet("{id}")]
        public IActionResult GetPermission(int id)
        {
            var item = _db.Permissions.FirstOrDefault(x => x.PermissionId == id);
            if (item is null) return NotFound("Permission ကို ရှာမတွေ့ပါ။");

            return Ok(new PermissionModel
            {
                PermissionId = item.PermissionId,
                PermissionName = item.PermissionName
            });
        }

        // POST /api/permission
        [HttpPost]
        public IActionResult CreatePermission(PermissionCreateRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.PermissionName))
            {
                return BadRequest("Permission အမည် ထည့်သွင်းပေးပါ။");
            }

            if (_db.Permissions.Any(x => x.PermissionName == request.PermissionName))
            {
                return BadRequest("Permission အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။");
            }

            var entity = new Permission
            {
                PermissionName = request.PermissionName
            };
            _db.Permissions.Add(entity);
            _db.SaveChanges();

            return Ok(new PermissionModel
            {
                PermissionId = entity.PermissionId,
                PermissionName = entity.PermissionName
            });
        }

        // PUT /api/permission/{id}
        [HttpPut("{id}")]
        public IActionResult UpdatePermission(int id, PermissionUpdateRequestModel request)
        {
            var item = _db.Permissions.FirstOrDefault(x => x.PermissionId == id);
            if (item is null) return NotFound("Permission ကို ရှာမတွေ့ပါ။");

            if (string.IsNullOrWhiteSpace(request.PermissionName))
            {
                return BadRequest("Permission အမည် ထည့်သွင်းပေးပါ။");
            }

            if (_db.Permissions.Any(x => x.PermissionName == request.PermissionName && x.PermissionId != id))
            {
                return BadRequest("Permission အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။");
            }

            item.PermissionName = request.PermissionName;
            _db.SaveChanges();

            return Ok(new PermissionModel
            {
                PermissionId = item.PermissionId,
                PermissionName = item.PermissionName
            });
        }

        // DELETE /api/permission/{id}
        [HttpDelete("{id}")]
        public IActionResult DeletePermission(int id)
        {
            var item = _db.Permissions.FirstOrDefault(x => x.PermissionId == id);
            if (item is null) return NotFound("Permission ကို ရှာမတွေ့ပါ။");

            var relatedRolePermissions = _db.RolePermissions.Where(rp => rp.PermissionId == id);
            _db.RolePermissions.RemoveRange(relatedRolePermissions);

            _db.Permissions.Remove(item);
            int result = _db.SaveChanges();

            return Ok(new PermissionDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "Permission ဖျက်ခြင်း အောင်မြင်ပါသည်။" : "Permission ဖျက်ခြင်း မအောင်မြင်ပါ။"
            });
        }

        // GET /api/permission/student-apply-now-status
        [HttpGet("student-apply-now-status")]
        [AllowAnonymous]
        public IActionResult GetStudentApplyNowStatus()
        {
            var studentRole = _db.Roles.FirstOrDefault(r => r.RoleName == "Student" || r.RoleId == 3);
            int studentRoleId = studentRole?.RoleId ?? 3;

            bool hasPermission = _db.RolePermissions
                .Include(rp => rp.Permission)
                .Any(rp => rp.RoleId == studentRoleId && 
                           (rp.Permission.PermissionName == "Student.ApplyNow" || 
                            rp.Permission.PermissionName == "Student.ApplyJoin" ||
                            rp.Permission.PermissionName == "StudentRegistrations.Create"));

            return Ok(new ApplyNowStatusResponseModel
            {
                IsSuccess = true,
                IsEnabled = hasPermission,
                Message = hasPermission ? "Apply Now button is visible." : "Apply Now button is hidden."
            });
        }

        // GET /api/permission/paginate
        [HttpGet("paginate")]
        [AllowAnonymous]
        public IActionResult GetPermissionsPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Permissions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.PermissionName != null && x.PermissionName.Contains(searchTerm));
            }

            var totalCount = query.Count();

            var items = query
                .OrderBy(x => x.PermissionId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PermissionModel
                {
                    PermissionId = x.PermissionId,
                    PermissionName = x.PermissionName
                })
                .ToList();

            var result = new PagedResult<PermissionModel>
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
