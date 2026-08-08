using System;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class MajorCreateRequestModel
{
    public string? MajorName { get; set; }
    public int FacultyId { get; set; }
    public string? CreatedBy { get; set; }
}

public class MajorCreateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class MajorUpdateRequestModel
{
    public string? MajorName { get; set; }
    public int FacultyId { get; set; }
    public string? ModifiedBy { get; set; }
}

public class MajorUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public MajorModel? Data { get; set; }
}

public class MajorDeleteResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class MajorModel
{
    public int MajorId { get; set; }
    public string? MajorName { get; set; }
    public int FacultyId { get; set; }
    public string? FacultyName { get; set; }
}
