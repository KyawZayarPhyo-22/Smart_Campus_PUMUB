namespace Smart_Campus_PUMUB.WebApi.Models;

// --- Create Models ---
public class RoleCreateRequestModel
{
    public string? RoleName { get; set; }
}

public class RoleCreateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

// --- Update Models ---
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

// --- Data View Model ---
public class RoleModel
{
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
}

public class TblRole
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public bool IsDeleted { get; set; } = false; // Soft Delete အတွက် ဒီ property လိုပါတယ်
}

public class RoleDeleteResponseModel
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
}