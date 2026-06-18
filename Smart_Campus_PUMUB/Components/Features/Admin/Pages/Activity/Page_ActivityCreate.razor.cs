using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.IO;
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
    private string? PreviewImageUrl { get; set; }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file != null)
        {
            activityModel.ImageFileName = file.Name;
            using var ms = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 5).CopyToAsync(ms); // 5MB Max
            var bytes = ms.ToArray();
            activityModel.ImageBase64 = Convert.ToBase64String(bytes);
            PreviewImageUrl = $"data:{file.ContentType};base64,{activityModel.ImageBase64}";
        }
    }

    private async Task SaveActivity()
    {
        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";

        try
        {
            // 💡 API ဘက်ကို File နဲ့ Data တစ်ပါတည်း ပို့ဖို့အတွက် MultipartFormDataContent သုံးခြင်း
            var content = new MultipartFormDataContent();

            content.Add(new StringContent(activityModel.ActivityTitle ?? ""), "ActivityTitle");
            content.Add(new StringContent(activityModel.Description ?? ""), "Description");
            content.Add(new StringContent(activityModel.Location ?? ""), "Location");

            // Base64 မှ File သို့ ပြန်ပြောင်းပြီး ပို့ခြင်း
            if (!string.IsNullOrEmpty(activityModel.ImageBase64))
            {
                var fileBytes = Convert.FromBase64String(activityModel.ImageBase64);
                var fileContent = new ByteArrayContent(fileBytes);
                content.Add(fileContent, "ImageFile", activityModel.ImageFileName ?? "image.jpg");
            }

            // HttpClientService ထဲတွင် SendAsync(HttpMethod.Post, "activity", content) ကို သုံး၍ ခေါ်ပါ
            // အောက်ပါက မင်းဆီမှာရှိတဲ့ Service ရဲ့ ပုံစံပေါ်မူတည်ပြီး အနည်းငယ် ပြင်ပေးပါ
            // အရင်က HttpClientService.ExecuteAsync... နေရာမှာ
            // ဒီလိုလေး အစားထိုးလိုက်ပါ
            var response = await HttpClientService.ExecuteMultipartAsync<ActivityCreateResponseModel>("activity", content);
            if (response != null && response.IsSuccess)
            {
                //await JSRuntime.InvokeVoidAsync("alert", "Activity အောင်မြင်စွာ သိမ်းဆည်းပြီးပါပြီ။");
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