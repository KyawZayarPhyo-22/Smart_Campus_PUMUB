namespace Smart_Campus_PUMUB.WebApi.Models;

public class StudentCreateRequestModel
{
    public int UserId { get; set; }
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; } // ဥပမာ - MUB-1098
}

public class StudentUpdateRequestModel
{
    public int UserId{get;set;}
    public string? UserName {get;set;}
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; }
    public string Status { get; set; } = "Active";
}

public class StudentResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public StudentModel? Data { get; set; }
}

public class StudentModel
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string? UserName {get;set;}
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; }
    public string Status { get; set; } = "Active";
}
public class StudentPatchRequestModel
{
    public string? CurrentClassYear { get; set; }
    public string? CurrentMajor { get; set; }
    public string? CurrentRollNo { get; set; }
    public string? Status { get; set; }
}