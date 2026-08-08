using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;

namespace Smart_Campus_PUMUB.WebApi.Services
{
    public interface IFacultyDataScopeService
    {
        bool IsSuperAdmin(ClaimsPrincipal user);
        int? GetScopedFacultyId(ClaimsPrincipal user);
        bool CanAccessFaculty(ClaimsPrincipal user, int facultyId);
    }

    public class FacultyDataScopeService : IFacultyDataScopeService
    {
        private readonly SmartCampusDbContext _db;

        public FacultyDataScopeService(SmartCampusDbContext db)
        {
            _db = db;
        }

        public bool IsSuperAdmin(ClaimsPrincipal user)
        {
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return false;

            // Check claims for Super Admin or CanAccessAllFaculties
            var roleName = user.FindFirst(ClaimTypes.Role)?.Value;
            var roleIdClaim = user.FindFirst("RoleId")?.Value;
            var canAccessAllClaim = user.FindFirst("CanAccessAllFaculties")?.Value;

            if (canAccessAllClaim == "true" || string.Equals(roleName, "Super Admin", System.StringComparison.OrdinalIgnoreCase) || roleIdClaim == "4")
            {
                return true;
            }

            // Check database role hierarchy if configured
            if (int.TryParse(roleIdClaim, out int roleId))
            {
                var isParentSuperAdmin = _db.RoleHierarchies.Any(rh => rh.ParentRoleId == roleId && rh.CanAccessAllFaculties);
                if (isParentSuperAdmin) return true;
            }

            return false;
        }

        public int? GetScopedFacultyId(ClaimsPrincipal user)
        {
            if (IsSuperAdmin(user))
            {
                return null; // Null means unlimited / all faculties
            }

            var facultyIdClaim = user.FindFirst("FacultyId")?.Value;
            if (int.TryParse(facultyIdClaim, out int facultyId))
            {
                return facultyId;
            }

            // Fallback check from DB using UserId if claim is not present
            var userIdClaim = user.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var dbUser = _db.Users.AsNoTracking().FirstOrDefault(u => u.UserId == userId);
                if (dbUser != null && dbUser.FacultyId.HasValue)
                {
                    return dbUser.FacultyId.Value;
                }
            }

            return null;
        }

        public bool CanAccessFaculty(ClaimsPrincipal user, int facultyId)
        {
            if (IsSuperAdmin(user))
            {
                return true;
            }

            var scopedFacultyId = GetScopedFacultyId(user);
            if (scopedFacultyId.HasValue)
            {
                return scopedFacultyId.Value == facultyId;
            }

            // If user has no specific faculty assigned and is not Super Admin, deny
            return false;
        }
    }
}
