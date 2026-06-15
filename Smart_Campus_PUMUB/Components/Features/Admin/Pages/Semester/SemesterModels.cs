using System;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class SemesterCreateRequestModel { public string? SemesterName { get; set; } }
public class SemesterCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

public class SemesterUpdateRequestModel { public string? SemesterName { get; set; } }
public class SemesterUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public SemesterModel? Data { get; set; }
}

public class SemesterDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

public class SemesterModel
{
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
}