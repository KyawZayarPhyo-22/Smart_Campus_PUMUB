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
    private string statusMessage;

    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<SemesterModel> SemesterList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private SemesterModel? SelectedSemester { get; set; }

    private string SearchInput = "";

    private void ApplyFilter()
    {
        SearchTerm = SearchInput;
        CurrentPage = 1;
        StateHasChanged();
    }

    private void ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        CurrentPage = 1;
        StateHasChanged();
    }

    private void HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            ApplyFilter();
        }
    }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<SemesterModel> GetFilteredSemesters() => string.IsNullOrWhiteSpace(SearchTerm)
        ? SemesterList
        : SemesterList.Where(s => s.SemesterName != null && s.SemesterName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<SemesterModel> FilteredSemesters
    {
        get
        {
            var allFiltered = GetFilteredSemesters();
            int count = allFiltered.Count();
            int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
            TotalPages = calcPages < 1 ? 1 : calcPages;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    private void OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        StateHasChanged();
    }

    public bool IsSuccess { get; private set; }

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

    private void OpenDeleteModal(SemesterModel semester)
    {
        SelectedSemester = semester;
        ShowModal = true;
        statusMessage = string.Empty;
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedSemester = null;
        ShowModal = false;
        statusMessage = string.Empty;
        IsSuccess = false;
    }
    private async Task DeleteSemester()
    {
        if (SelectedSemester == null) return;

        IsProcessing = true;

        statusMessage = string.Empty;
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<SemesterDeleteResponseModel>(
                $"semester/{SelectedSemester.SemesterId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                IsSuccess = true;
                statusMessage = response.Message ?? "Deleted successfully.";

                await LoadSemesters();

                await Task.Delay(800);
                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;

                // 🔥 same as Role style (simple fallback message)
                statusMessage = response?.Message
                    ?? "ဒီ Semester ကို အသုံးပြုထားသော Data များရှိနေသောကြောင့် ဖျက်၍ မရပါ။";
            }
        }
        catch
        {
            IsSuccess = false;

            statusMessage = "Server သို့ ချိတ်ဆက်ရာတွင် အမှားဖြစ်နေပါသည်။";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}