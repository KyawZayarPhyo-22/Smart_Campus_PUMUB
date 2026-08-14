using System.Collections.Generic;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class GradeModel
{
    public int GradeId { get; set; }
    public string Name { get; set; } = null!;
}

public class GradeListResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public List<GradeModel>? Data { get; set; }
}

public class SubjectGradeBindingModel
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string? Grade { get; set; }
}
