namespace Smart_Campus_PUMUB.WebApi.Models
{
    // --- Create ---
    public class SemesterCreateRequestModel { public string? SemesterName { get; set; } }
    public class SemesterCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- Update ---
    public class SemesterUpdateRequestModel { public string? SemesterName { get; set; } }
    public class SemesterUpdateResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public SemesterModel? Data { get; set; }
    }

    // --- Delete ---
    public class SemesterDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- View Model ---
    public class SemesterModel
    {
        public int SemesterId { get; set; }
        public string? SemesterName { get; set; }
    }
}
