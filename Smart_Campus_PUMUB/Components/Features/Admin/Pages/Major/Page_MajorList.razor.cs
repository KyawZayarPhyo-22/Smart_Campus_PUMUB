using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Major;

public partial class Page_MajorList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<MajorModel> MajorList { get; set; } = new();
    private List<FacultyModel> FacultyList { get; set; } = new();

    private string SearchTerm { get; set; } = "";
    private string SearchInput = "";
    private string FacultyFilter = "";

    // Custom Dropdown Open States
    private bool isFacultyDropdownOpen = false;

    private void ToggleFacultyDropdown()
    {
        isFacultyDropdownOpen = !isFacultyDropdownOpen;
    }

    private void SelectFaculty(string? facultyId)
    {
        FacultyFilter = facultyId ?? "";
        isFacultyDropdownOpen = false;
        // Search button နှိပ်မှသာ filter ဖြစ်မည်
    }

    private void CloseAllDropdowns()
    {
        isFacultyDropdownOpen = false;
    }

    private string GetSelectedFacultyName()
    {
        if (string.IsNullOrWhiteSpace(FacultyFilter)) return "All Faculties";
        if (int.TryParse(FacultyFilter, out int fid))
        {
            var match = FacultyList.FirstOrDefault(f => f.FacultyId == fid);
            return match?.FacultyName ?? "All Faculties";
        }
        return "All Faculties";
    }

    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;

    private string statusMessage = "";
    public bool IsSuccess { get; private set; }
    private bool ShowModal { get; set; } = false;
    private MajorModel? SelectedMajor { get; set; }

    // Pagination
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        SearchTerm = SearchInput;
        CurrentPage = 1;
        await LoadMajors();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        SearchInput = "";
        SearchTerm = "";
        FacultyFilter = "";
        CurrentPage = 1;
        await LoadMajors();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ApplyFilter();
    }

    private IEnumerable<MajorModel> FilteredMajors => MajorList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadMajors();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadFaculties();
        await LoadMajors();
    }

    private async Task LoadFaculties()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            if (response != null)
                FacultyList = response.Where(f => f.FacultyName != null).ToList();
        }
        catch { }
    }

    private async Task LoadMajors()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            int? parsedFacultyId = null;
            if (!string.IsNullOrWhiteSpace(FacultyFilter) && int.TryParse(FacultyFilter, out int fid))
            {
                parsedFacultyId = fid;
            }

            var url = $"major/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}";
            if (parsedFacultyId.HasValue)
            {
                url += $"&facultyId={parsedFacultyId.Value}";
            }

            var delayTask = Task.Delay(1000);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<MajorModel>>(url, EnumHttpMethod.Get);

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;
            if (response != null)
            {
                MajorList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading majors: {ex.Message}");
        }
        finally 
        { 
            IsLoading = false; 
            StateHasChanged();
        }
    }

    private void OpenDeleteModal(MajorModel major)
    {
        SelectedMajor = major;
        ShowModal = true;
        statusMessage = "";
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedMajor = null;
        ShowModal = false;
        statusMessage = "";
        IsSuccess = false;
    }

    private async Task DeleteMajor()
    {
        if (SelectedMajor == null) return;

        IsProcessing = true;
        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<MajorDeleteResponseModel>(
                $"major/{SelectedMajor.MajorId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
                IsSuccess = true;
                await Task.Delay(1500);
                CloseDeleteModal();
                await LoadMajors();
            }
            else
            {
                statusMessage = response?.Message ?? "ဖျက်သိမ်းမှု မအောင်မြင်ပါ။";
                IsSuccess = false;
            }
        }
        catch
        {
            statusMessage = "ဖျက်သိမ်းမှုတွင် အမှားဖြစ်နေပါသည်။";
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
