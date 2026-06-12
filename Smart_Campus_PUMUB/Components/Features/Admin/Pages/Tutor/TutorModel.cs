namespace Smart_Campus_PUMUB.WebApi.Models;

public class TutorCreateRequestModel
{
    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string? TutorName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? About { get; set; }
    public IFormFile? PhotoFile { get; set; }
}
public class UserModels
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string? UserName { get; set; }
    public string? RoleName { get; set; }
}

public class TutorCreateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class TutorUpdateRequestModel
{
    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
    public int UserId { get; set; }
    public string? TutorName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? About { get; set; }
    public IFormFile? PhotoFile { get; set; }
}
public class RoleModels
{
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
}


public class TutorUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public TutorModel? Data { get; set; }
}

public class TutorDeleteResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class TutorModel
{
    public int TutorId { get; set; }
    public int Department_Id { get; set; }
    public int Position_Id { get; set; }
    public int UserId { get; set; }
    public string? TutorName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Profile { get; set; }

    public string? About { get; set; }

    public DateTime? CreatedDateTime { get; set; }
    // public DateTime? CreatedDateTime { get; set; }
}