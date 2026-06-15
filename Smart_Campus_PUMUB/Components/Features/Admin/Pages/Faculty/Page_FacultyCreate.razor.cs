using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Faculty;

public partial class Page_FacultyCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm] private FacultyCreateRequestModel facultyModel { get; set; } = new();
    private string statusMessage = "";
    private bool isProcessing = false;

    //private async Task SaveFaculty()
    //{
    //    if (string.IsNullOrWhiteSpace(facultyModel.FacultyName))
    //    {
    //        statusMessage = "Faculty Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
    //        return;
    //    }
    //    isProcessing = true;
    //    statusMessage = "သိမ်းဆည်းနေပါသည်...";

    //    try
    //    {
    //        var response = await HttpClientService.ExecuteAsync<FacultyCreateResponseModel>("faculty", EnumHttpMethod.Post, facultyModel);
    //        if (response != null && response.IsSuccess)
    //        {

    //            await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "Faculty ဖန်တီးမှု အောင်မြင်ပါသည်။");
    //            NavigationManager.NavigateTo("/admin/faculties");
    //        }
    //        else { statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။"; }
    //    }
    //    catch (Exception ex)
    //    {
    //        if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
    //            statusMessage = "Faculty အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
    //        else
    //            statusMessage = $"Error: {ex.Message}";
    //    }
    //    finally { isProcessing = false; }
    //}

    private async Task SaveFaculty()
    {
        // Validation စစ်ဆေးခြင်း
        if (string.IsNullOrWhiteSpace(facultyModel.FacultyName))
        {
            statusMessage = "Faculty Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
            return;
        }

        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";

        try
        {
            var response = await HttpClientService.ExecuteAsync<FacultyCreateResponseModel>("faculty", EnumHttpMethod.Post, facultyModel);

            if (response != null && response.IsSuccess)
            {
                // Alert အစား success message ကို statusMessage မှာ ထည့်လိုက်သည်
                statusMessage = response.Message ?? "Faculty ဖန်တီးမှု အောင်မြင်ပါသည်။";

                // ခဏစောင့်ပြီးမှ နောက်စာမျက်နှာသို့ သွားစေခြင်း (အသုံးပြုသူ အောင်မြင်ကြောင်း မြင်ရစေရန်)
                await Task.Delay(1000);
                NavigationManager.NavigateTo("/admin/faculties");
            }
            else
            {
                statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။";
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
                statusMessage = "Faculty အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
            else
                statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }
}