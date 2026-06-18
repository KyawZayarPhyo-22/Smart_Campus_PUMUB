using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Semester;

public partial class Page_SemesterCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm] private SemesterCreateRequestModel semesterModel { get; set; } = new();
    private string statusMessage = "";
    private bool isProcessing = false;

    private async Task SaveSemester()
    {
        if (string.IsNullOrWhiteSpace(semesterModel.SemesterName))
        {
            statusMessage = "Semester Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
            return;
        }
        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";
        try
        {
            var response = await HttpClientService.ExecuteAsync<SemesterCreateResponseModel>("semester", EnumHttpMethod.Post, semesterModel);
            if (response != null && response.IsSuccess)
            {
                //await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "Semester ဖန်တီးမှု အောင်မြင်ပါသည်။");
                NavigationManager.NavigateTo("/admin/semesters");
            }
            else { statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။"; }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
                statusMessage = "Semester အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
            else
                statusMessage = $"Error: {ex.Message}";
        }
        finally { isProcessing = false; }
    }
}