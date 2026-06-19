namespace Smart_Campus_PUMUB.WebApi.Models;

public class StudentCreateRequestModel
{
    public int UserId { get; set; }
    public string Name { get; set; }
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; } // ဥပမာ - MUB-1098
}

public class StudentUpdateRequestModel
{
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
    public string? FullName { get; set; }
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; }
    public string Status { get; set; } = "Active";
    public string? Sem1_Result { get; set; }
    public string? Sem2_Result { get; set; }
    public string? Sem3_Result { get; set; }
    public string? Sem4_Result { get; set; }
    public string? Sem5_Result { get; set; }
    public string? Sem6_Result { get; set; }
    public string? Sem7_Result { get; set; }
    public string? Sem8_Result { get; set; }
    public string? Sem9_Result { get; set; }
}
public class StudentPatchRequestModel
{
    public string? CurrentClassYear { get; set; }
    public string? CurrentMajor { get; set; }
    public string? CurrentRollNo { get; set; }
    public string? Status { get; set; }
    public string? Sem1_Result { get; set; }
    public string? Sem2_Result { get; set; }
    public string? Sem3_Result { get; set; }
    public string? Sem4_Result { get; set; }
    public string? Sem5_Result { get; set; }
    public string? Sem6_Result { get; set; }
    public string? Sem7_Result { get; set; }
    public string? Sem8_Result { get; set; }
    public string? Sem9_Result { get; set; }
}