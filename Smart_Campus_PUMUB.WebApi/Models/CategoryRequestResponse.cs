namespace Smart_Campus_PUMUB.WebApi.Models
{
    // --- Create ---
    public class CategoryCreateRequestModel { public string? CategoryName { get; set; } }
    public class CategoryCreateResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- Update ---
    public class CategoryUpdateRequestModel { public string? CategoryName { get; set; } }
    public class CategoryUpdateResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public CategoryModel? Data { get; set; }
    }

    // --- Delete ---
    public class CategoryDeleteResponseModel { public bool IsSuccess { get; set; } public string? Message { get; set; } }

    // --- View Model ---
    public class CategoryModel
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}   
