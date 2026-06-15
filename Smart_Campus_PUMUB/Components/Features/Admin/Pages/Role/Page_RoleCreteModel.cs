using System;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Role;

public class RoleCreateRequestModel
{
    public string? RoleName { get; set; }
}

public class RoleCreateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class RoleUpdateRequestModel
{
    public string? RoleName { get; set; }
}

public class RoleUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public RoleModel? Data { get; set; }
}

public class RoleModel
{
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
}

public class TblRole
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public bool IsDeleted { get; set; } = false;
}

public class RoleDeleteResponseModel
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = null!;
}
