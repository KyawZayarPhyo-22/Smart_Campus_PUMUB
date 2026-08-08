namespace Smart_Campus_PUMUB.WebApi.Models
{
    // --- Create ---
    public class MajorCreateRequestModel
    {
        public string? MajorName { get; set; }
        public int FacultyId { get; set; }
        public string? CreatedBy { get; set; }
    }
    public class MajorCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- Update ---
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

    // --- Delete ---
    public class MajorDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- View Model ---
    public class MajorModel
    {
        public int MajorId { get; set; }
        public string? MajorName { get; set; }
        public int FacultyId { get; set; }
        public string? FacultyName { get; set; }
    }
}
