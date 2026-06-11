namespace Smart_Campus_PUMUB.WebApi.Models
{
    // --- Create ---
    public class ActivityCreateRequestModel
    {
        public string ActivityTitle { get; set; } = null!;
        public IFormFile? ImageFile { get; set; } // File အတွက် IFormFile သုံးပါ
        public string? Description { get; set; }
        public string? Location { get; set; }
    }
    public class ActivityCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- Update ---

    public class ActivityUpdateRequestModel
    {
        public string? ActivityTitle { get; set; }
        public string? ActivityContent { get; set; }

        // Added missing properties used by the controller:
        public string? Image { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
    }
    public class ActivityUpdateResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public ActivityModel? Data { get; set; }
    }

    // --- Delete ---
    public class ActivityDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- View Model ---
    public class ActivityModel
    {
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; } = null!;
        public string? Image { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
    }
}
