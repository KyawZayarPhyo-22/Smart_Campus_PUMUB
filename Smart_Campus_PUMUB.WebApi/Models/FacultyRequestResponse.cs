namespace Smart_Campus_PUMUB.WebApi.Models
{
    // --- Create ---
    public class FacultyCreateRequestModel { public string? FacultyName { get; set; } }
    public class FacultyCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- Update ---
    public class FacultyUpdateRequestModel { public string? FacultyName { get; set; } }
    public class FacultyUpdateResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public FacultyModel? Data { get; set; }
    }

    // --- Delete ---
    public class FacultyDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- View Model ---
    public class FacultyModel
    {
        public int FacultyId { get; set; }
        public string? FacultyName { get; set; }
    }
}
