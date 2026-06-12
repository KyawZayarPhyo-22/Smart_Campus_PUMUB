namespace Smart_Campus_PUMUB.WebApi.Models
{
    // --- Create ---
    public class PositionCreateRequestModel { public string? PositionName { get; set; } }
    public class PositionCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- Update ---
    public class PositionUpdateRequestModel { public string? PositionName { get; set; } }
    public class PositionUpdateResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public PositionModel? Data { get; set; }
    }

    // --- Delete ---
    public class PositionDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- View Model ---
    public class PositionModel
    {
        public int PositionId { get; set; }
        public string? PositionName { get; set; }
    }
}
