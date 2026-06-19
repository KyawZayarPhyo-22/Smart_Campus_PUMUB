namespace Smart_Campus_PUMUB.WebApi.Models;

public class UserCreateRequestModel
{
    public int RoleId { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? RoleNo { get; set; }
}

public class UserCreateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class UserUpdateRequestModel
{
    public int RoleId { get; set; }
    public int UserId{get;set;}
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? RoleNo { get; set; }
}
public class UserEditResponseModel
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? RoleNo { get; set; }
}

public class UserUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public UserModel? Data { get; set; }
}

public class UserDeleteResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class UserModel
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public int? PasswordLength { get; set; }
    public string? RoleName { get; set; }
    public string? RoleNo { get; set; }
}
public class UserRegisterRequestModel
{
    public string? FullName { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
}

public class UserLoginRequestModel
{
    public string? UserName { get; set; }
    public string? Password { get; set; }
}
public class RoleModel
{
    public int RoleId { get; set; }
    public string? RoleName { get; set; }
}