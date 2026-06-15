using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Smart_Campus_PUMUB.WebApi.Models.StudentRegistrationModel;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Student;

public partial class Page_StudentList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<StudentRegistrationModel> RegistrationList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;

    private bool ShowDetailModal { get; set; } = false;
    private StudentRegistrationModel? SelectedRegistration { get; set; }

    private IEnumerable<StudentRegistrationModel> FilteredRegistrations
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
                return RegistrationList;

            return RegistrationList.Where(r =>
                r.StudentNameMm?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                r.StudentNrcNo?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                r.RollNo?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                r.Major?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) == true);
        }
    }

    protected override async Task OnInitializedAsync() => await LoadRegistrations();

    private async Task LoadRegistrations()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<StudentRegistrationModel>>("StudentRegistrations", EnumHttpMethod.Get);
            if (response != null) RegistrationList = response;
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Error Loading Data: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    private async Task OpenDetailModal(int registrationId)
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<StudentRegistrationModel>($"StudentRegistrations/{registrationId}", EnumHttpMethod.Get);
            if (response != null)
            {
                SelectedRegistration = response;
                ShowDetailModal = true;
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Error Loading Detail: {ex.Message}");
        }
    }

    private void CloseDetailModal()
    {
        SelectedRegistration = null;
        ShowDetailModal = false;
    }

    private async Task UpdateStatus(int registrationId, string status)
    {
        bool confirm = await JSRuntime.InvokeAsync<bool>("confirm", $"ဤကျောင်းအပ်ဖောင်ကို '{status}' အဖြစ် ပြောင်းလဲရန် သေချာပါသလား ကိုကို?");
        if (!confirm) return;

        IsProcessing = true;
        try
        {
            var patchModel = new StudentRegistrationStatusPatchModel
            {
                Status = status,
                modified_by = "Admin_Panel"
            };

            var response = await HttpClientService.ExecuteAsync<StudentRegistrationResponseModel>(
                $"StudentRegistrations/{registrationId}/status",
                EnumHttpMethod.Patch,
                patchModel
            );

            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Status updated to {status} successfully!");
                CloseDetailModal();
                await LoadRegistrations();
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ပြောင်းလဲမှု မအောင်မြင်ပါ။");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", ex.Message);
        }
        finally { IsProcessing = false; }
    }
}