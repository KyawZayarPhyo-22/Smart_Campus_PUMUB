namespace Smart_Campus_PUMUB.WebApi.Models;

public class GradeModel
{
    public int GradeId { get; set; }
    public string Name { get; set; } = null!;
    public decimal GradePoint { get; set; } = 0.0m;
    public string? Status { get; set; }
    public decimal? MinMark { get; set; }
    public decimal? MaxMark { get; set; }
}

public class GradeRequestModel
{
    public string Name { get; set; } = null!;
    public decimal GradePoint { get; set; } = 0.0m;
    public string? Status { get; set; }
    public decimal? MinMark { get; set; }
    public decimal? MaxMark { get; set; }
}

public class GradeResponseModel : ActionResponseModel
{
    public GradeModel? Data { get; set; }
}

public class GradeListResponseModel : ActionResponseModel
{
    public List<GradeModel>? Data { get; set; }
}
