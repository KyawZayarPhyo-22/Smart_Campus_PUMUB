using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Activity;

public partial class Page_ActivityCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm] private ActivityCreateRequestModel activityModel { get; set; } = new();
    private string statusMessage = "";
    private bool isProcessing = false;

    private async Task HandleFilesSelected(InputFileChangeEventArgs e)
    {
        try
        {
            var files = e.GetMultipleFiles(20);
            foreach (var file in files)
            {
                if (file != null)
                {
                    using var ms = new MemoryStream();
                    await file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 10).CopyToAsync(ms); // 10MB Max per image
                    var bytes = ms.ToArray();
                    var base64 = Convert.ToBase64String(bytes);

                    activityModel.Images.Add(new ImageUploadItem
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

    private void RemoveImage(int index)
    {
        if (index >= 0 && index < activityModel.Images.Count)
        {
            activityModel.Images.RemoveAt(index);
        }
    }

    private void ClearAllImages()
    {
        activityModel.Images.Clear();
    }

    private async Task SaveActivity()
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

            // Send all images as ImageFiles
            if (activityModel.Images != null && activityModel.Images.Any())
            {
                foreach (var img in activityModel.Images)
                {
                    if (!string.IsNullOrEmpty(img.Base64))
                    {
                        var fileBytes = Convert.FromBase64String(img.Base64);
                        var fileContent = new ByteArrayContent(fileBytes);
                        content.Add(fileContent, "ImageFiles", img.FileName);
                    }
                }
            }

            var response = await HttpClientService.ExecuteMultipartAsync<ActivityCreateResponseModel>("activity", content);
            if (response != null && response.IsSuccess)
            {
                NavigationManager.NavigateTo("/admin/activities");
            }
            else
            {
                statusMessage = response?.Message ?? "သိမ်းဆည်းရာတွင် အမှားအယွင်းရှိပါသည်။";
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