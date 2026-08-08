using Microsoft.AspNetCore.Authorization;

namespace Smart_Campus_PUMUB.WebApi.Filters;

// Custom attribute for policy-based permission authorization
public class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(string permissionName)
    {
        // Set policy name to permission identifier
        Policy = permissionName;
    }
}
