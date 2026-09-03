using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class ImageUploadItem
{
    public string FileName { get; set; } = "";
    public string Base64 { get; set; } = "";
    public string ContentType { get; set; } = "image/jpeg";
    public string PreviewUrl => $"data:{ContentType};base64,{Base64}";
}

public class ActivityCreateRequestModel
{
    public string ActivityTitle { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime? ActivityDate { get; set; } = DateTime.Today;

    // Single image backward compat
    public string? ImageBase64 { get; set; } 
    public string? ImageFileName { get; set; }

    // Multiple images
    public List<ImageUploadItem> Images { get; set; } = new();
}

public class ActivityCreateResponseModel 
{ 
    public bool IsSuccess { get; set; } 
    public string? Message { get; set; } 
}

public class ActivityUpdateRequestModel
{
    public string? ActivityTitle { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime? ActivityDate { get; set; }
    public string? Image { get; set; }
    public string? ImageBase64 { get; set; }
    public string? ImageFileName { get; set; }

    // Existing images kept
    public List<string> ExistingImages { get; set; } = new();

    // Newly added images
    public List<ImageUploadItem> NewImages { get; set; } = new();
}

public class ActivityUpdateResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public ActivityModel? Data { get; set; }
}

public class ActivityDeleteResponseModel 
{ 
    public bool IsSuccess { get; set; } 
    public string? Message { get; set; } 
}

public class ActivityModel
{
    public int ActivityId { get; set; }
    public string ActivityTitle { get; set; } = null!;
    public string? Image { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ActivityDate { get; set; }

    public string? CreatedBy { get; set; }

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