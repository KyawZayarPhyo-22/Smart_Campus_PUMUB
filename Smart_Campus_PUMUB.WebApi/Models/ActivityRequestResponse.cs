using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Smart_Campus_PUMUB.WebApi.Models
{
    // --- Create ---
    public class ActivityCreateRequestModel
    {
        public string ActivityTitle { get; set; } = null!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? ActivityDate { get; set; }
        public IFormFile? ImageFile { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
    }

    public class ActivityCreateResponseModel 
    { 
        public bool IsSuccess { get; set; } 
        public string? Message { get; set; } 
    }

    // --- Update ---
    public class ActivityUpdateRequestModel
    {
        public string? ActivityTitle { get; set; }
        public string? ActivityContent { get; set; }
        public string? Image { get; set; }
        public IFormFile? ImageFile { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
        public string? ExistingImagesJson { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime? ActivityDate { get; set; }
    }

    public class ActivityUpdateResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public ActivityModel? Data { get; set; }
    }

    // --- Delete ---
    public class ActivityDeleteResponseModel 
    { 
        public bool IsSuccess { get; set; } 
        public string? Message { get; set; } 
    }

    // --- View Model ---
    public class ActivityModel
    {
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; } = null!;
        public string? Image { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ActivityDate { get; set; }

        public List<string> ImageList => GetImageList(Image);

        public string PrimaryImage => ImageList.FirstOrDefault() ?? Image ?? "";

        public static List<string> GetImageList(string? imageStr)
        {
            if (string.IsNullOrWhiteSpace(imageStr)) return new List<string>();

            imageStr = imageStr.Trim();
            if (imageStr.StartsWith("[") && imageStr.EndsWith("]"))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(imageStr);
                    if (list != null && list.Count > 0)
                        return list.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }
                catch { }
            }

            if (imageStr.Contains(';'))
            {
                return imageStr.Split(';', StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .Where(s => !string.IsNullOrWhiteSpace(s))
                               .ToList();
            }

            return new List<string> { imageStr };
        }
    }
}
