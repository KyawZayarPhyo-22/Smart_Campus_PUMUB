using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Semester;

public partial class Page_SemesterList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<SemesterModel> SemesterList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private SemesterModel? SelectedSemester { get; set; }

    private IEnumerable<SemesterModel> FilteredSemesters => string.IsNullOrWhiteSpace(SearchTerm)
        ? SemesterList
        : SemesterList.Where(s => s.SemesterName != null && s.SemesterName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        await LoadSemesters();
    }

    private async Task LoadSemesters()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<SemesterModel>>("semester", EnumHttpMethod.Get);
            if (response != null) SemesterList = response;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(SemesterModel semester) { SelectedSemester = semester; ShowModal = true; }
    private void CloseDeleteModal() { SelectedSemester = null; ShowModal = false; }

    private async Task DeleteSemester()
    {
        if (SelectedSemester == null) return;
        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<SemesterDeleteResponseModel>($"semester/{SelectedSemester.SemesterId}", EnumHttpMethod.Delete);
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadSemesters();
            }
            else { await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။"); }
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
        finally { IsProcessing = false; }
    }
}