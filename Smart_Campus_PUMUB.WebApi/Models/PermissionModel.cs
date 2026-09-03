namespace Smart_Campus_PUMUB.WebApi.Models
{
    public class PermissionModel
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
    }

    public class PermissionCreateRequestModel
    {
        public string PermissionName { get; set; } = string.Empty;
    }

    public class PermissionUpdateRequestModel
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
    }

    public class PermissionDeleteResponseModel
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ApplyNowStatusResponseModel
    {
        public bool IsSuccess { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }

    public class ApplyNowToggleRequestModel
    {
        public bool IsEnabled { get; set; }
    }
}

