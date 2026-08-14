namespace Smart_Campus_PUMUB.WebApi.Models;

public class GradeModel
{
    public int GradeId { get; set; }
    public string Name { get; set; } = null!;
}

public class GradeRequestModel
{
    public string Name { get; set; } = null!;
}

public class GradeResponseModel : ActionResponseModel
{
    public GradeModel? Data { get; set; }
}

public class GradeListResponseModel : ActionResponseModel
{
    public List<GradeModel>? Data { get; set; }
}
