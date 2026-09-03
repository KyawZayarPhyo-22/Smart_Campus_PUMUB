using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Activity;

public partial class Page_ActivityEdit
{
    [Parameter] public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IConfiguration Configuration { get; set; } = null!;

    [SupplyParameterFromForm] private ActivityUpdateRequestModel activityModel { get; set; } = new();
    private string statusMessage = "";
    private bool IsLoading = true;
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<ActivityModel>($"activity/{Id}", EnumHttpMethod.Get);
            if (response != null)
            {
                activityModel.ActivityTitle = response.ActivityTitle;
                activityModel.Location = response.Location;
                activityModel.Description = response.Description;
                activityModel.Image = response.Image;
                activityModel.ActivityDate = response.ActivityDate ?? (response.CreatedAt.Year > 1 ? response.CreatedAt : DateTime.Today);
                activityModel.ExistingImages = ActivityModel.GetImageList(response.Image);
            }
        }
        catch (Exception ex) 
        { 
            statusMessage = $"Error: {ex.Message}"; 
        }
        finally 
        { 
            IsLoading = false; 
        }
    }

    private async Task HandleNewFilesSelected(InputFileChangeEventArgs e)
    {
        try
        {
            var files = e.GetMultipleFiles(20);
            foreach (var file in files)
            {
                if (file != null)
                {
                    using var ms = new MemoryStream();
                    await file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 10).CopyToAsync(ms); // 10MB Max
                    var bytes = ms.ToArray();
                    var base64 = Convert.ToBase64String(bytes);

                    activityModel.NewImages.Add(new ImageUploadItem
                    {
                        FileName = file.Name,
                        Base64 = base64,
                        ContentType = file.ContentType
                    });
                }
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Image upload error: {ex.Message}";
        }
    }

    private void RemoveExistingImage(int index)
    {
        if (index >= 0 && index < activityModel.ExistingImages.Count)
        {
            activityModel.ExistingImages.RemoveAt(index);
        }
    }

    private void RemoveNewImage(int index)
    {
        if (index >= 0 && index < activityModel.NewImages.Count)
        {
            activityModel.NewImages.RemoveAt(index);
        }
    }

    private void ClearNewImages()
    {
        activityModel.NewImages.Clear();
    }

    private string GetFullImageUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "";
        if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return path;
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        var baseUrl = Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5077";
        return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private async Task UpdateActivity()
    {
        if (string.IsNullOrWhiteSpace(activityModel.ActivityTitle))
        {
            statusMessage = "Activity Title ထည့်သွင်းရန် လိုအပ်ပါသည်။";
            return;
        }

        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";

        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(activityModel.ActivityTitle ?? ""), "ActivityTitle");
            content.Add(new StringContent(activityModel.Description ?? ""), "Description");
            content.Add(new StringContent(activityModel.Location ?? ""), "Location");

            if (activityModel.ActivityDate.HasValue)
            {
                content.Add(new StringContent(activityModel.ActivityDate.Value.ToString("yyyy-MM-ddTHH:mm:ss")), "ActivityDate");
            }

            // Existing images list as JSON
            var existingJson = JsonSerializer.Serialize(activityModel.ExistingImages ?? new List<string>());
            content.Add(new StringContent(existingJson), "ExistingImagesJson");

            // New uploaded images
            if (activityModel.NewImages != null && activityModel.NewImages.Any())
            {
                foreach (var img in activityModel.NewImages)
                {
                    if (!string.IsNullOrEmpty(img.Base64))
                    {
                        var fileBytes = Convert.FromBase64String(img.Base64);
                        var fileContent = new ByteArrayContent(fileBytes);
                        content.Add(fileContent, "ImageFiles", img.FileName);
                    }
                }
            }

            var response = await HttpClientService.ExecuteMultipartAsync<ActivityUpdateResponseModel>($"activity/update/{Id}", content);

            if (response != null && response.IsSuccess)
            {
                NavigationManager.NavigateTo("/admin/activities");
            }
            else
            {
                statusMessage = response?.Message ?? "ပြင်ဆင်ရာတွင် အမှားအယွင်းရှိပါသည်။";
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
        }
        finally 
        { 
            isProcessing = false; 
        }
    }
}