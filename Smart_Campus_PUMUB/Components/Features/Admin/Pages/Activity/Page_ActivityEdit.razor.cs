using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Activity;

public partial class Page_ActivityEdit
{
    [Parameter] public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm] private ActivityUpdateRequestModel activityModel { get; set; } = new();
    private string statusMessage = "";
    private bool IsLoading = true;
    private bool isProcessing = false;
    private string? PreviewImageUrl { get; set; }

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
                PreviewImageUrl = response.Image; // လက်ရှိပုံဟောင်းအား ကြိုပြထားရန်
                if (!string.IsNullOrEmpty(response.Image))
                {
                    PreviewImageUrl = $"https://localhost:7297/{response.Image}";
                }
            }
        }
        catch (Exception ex) { statusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file != null)
        {
            activityModel.ImageFileName = file.Name;
            using var ms = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 5).CopyToAsync(ms);
            var bytes = ms.ToArray();
            activityModel.ImageBase64 = Convert.ToBase64String(bytes);
            PreviewImageUrl = $"data:{file.ContentType};base64,{activityModel.ImageBase64}";
        }
    }

    // private async Task UpdateActivity()
    // {
    //     if (string.IsNullOrWhiteSpace(activityModel.ActivityTitle))
    //     {
    //         statusMessage = "Activity Title ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
    //         return;
    //     }
    //     isProcessing = true;
    //     statusMessage = "ပြင်ဆင်ချက်များကို သိမ်းဆည်းနေပါသည်...";
    //     try
    //     {
    //         var response = await HttpClientService.ExecuteAsync<ActivityUpdateResponseModel>($"activity/{Id}", EnumHttpMethod.Put, activityModel);
    //         if (response != null && response.IsSuccess)
    //         {
    //             await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "Activity ပြင်ဆင်မှု အောင်မြင်ပါသည်။");
    //             NavigationManager.NavigateTo("/admin/activities");
    //         }
    //         else { statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။"; }
    //     }
    //     catch (Exception ex)
    //     {
    //         if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
    //             statusMessage = "Activity ခေါင်းစဉ်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
    //         else
    //             statusMessage = $"Error: {ex.Message}";
    //     }
    //     finally { isProcessing = false; }
    // }
    private async Task UpdateActivity()
    {
        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";

        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(activityModel.ActivityTitle ?? ""), "ActivityTitle");
            content.Add(new StringContent(activityModel.Description ?? ""), "Description");
            content.Add(new StringContent(activityModel.Location ?? ""), "Location");

            if (!string.IsNullOrEmpty(activityModel.ImageBase64))
            {
                var fileBytes = Convert.FromBase64String(activityModel.ImageBase64);
                var fileContent = new ByteArrayContent(fileBytes);
                content.Add(fileContent, "ImageFile", activityModel.ImageFileName ?? "image.jpg");
            }

            // 💡 ဤနေရာတွင် url ကို "activity/update/{Id}" ဟု ပြောင်းလိုက်ပါ
            var response = await HttpClientService.ExecuteMultipartAsync<ActivityUpdateResponseModel>($"activity/update/{Id}", content);

            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", "ပြင်ဆင်မှု အောင်မြင်ပါသည်။");
                NavigationManager.NavigateTo("/admin/activities");
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
        }
        finally { isProcessing = false; }
    }
}