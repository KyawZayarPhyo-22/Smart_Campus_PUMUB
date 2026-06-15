using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Semester;

public partial class Page_SemesterEdit
{
    [Parameter] public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm] private SemesterUpdateRequestModel semesterModel { get; set; } = new();
    private string statusMessage = "";
    private bool IsLoading = true;
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<SemesterModel>($"semester/{Id}", EnumHttpMethod.Get);
            if (response != null) semesterModel.SemesterName = response.SemesterName;
        }
        catch (Exception ex) { statusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task UpdateSemester()
    {
        if (string.IsNullOrWhiteSpace(semesterModel.SemesterName))
        {
            statusMessage = "Semester Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
            return;
        }
        isProcessing = true;
        statusMessage = "ပြင်ဆင်ချက်များကို သိမ်းဆည်းနေပါသည်...";
        try
        {
            var response = await HttpClientService.ExecuteAsync<SemesterUpdateResponseModel>($"semester/{Id}", EnumHttpMethod.Put, semesterModel);
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "Semester ပြင်ဆင်မှု အောင်မြင်ပါသည်။");
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