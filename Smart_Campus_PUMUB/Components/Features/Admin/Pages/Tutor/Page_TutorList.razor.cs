using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Tutor;

public partial class Page_TutorList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<TutorModel> TutorList = new();
    private string _searchTerm = "";
    private bool IsLoading = true;
    private bool IsProcessing = false;
    
    // Delete Modal
    private bool ShowModal = false;
    private TutorModel? SelectedTutor;

    // Search Logic with Property Setter
    public string SearchTerm 
    { 
        get => _searchTerm; 
        set { _searchTerm = value; StateHasChanged(); } 
    }

    private string SearchInput = "";
    private string SelectedRoleInput = "All";
    private string SelectedRole = "All";

    private void ApplyFilter()
    {
        SearchTerm = SearchInput;
        SelectedRole = SelectedRoleInput;
        CurrentPage = 1;
        StateHasChanged();
    }

    private void ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        SelectedRoleInput = "All";
        SelectedRole = "All";
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

    protected override async Task OnInitializedAsync() => await LoadTutors();

    private async Task LoadTutors()
    {
        IsLoading = true;
        TutorList = await HttpClientService.ExecuteAsync<List<TutorModel>>("tutor", EnumHttpMethod.Get) ?? new();
        IsLoading = false;
    }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<TutorModel> GetFilteredTutors()
    {
        var list = TutorList.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            list = list.Where(t => t.TutorName != null && t.TutorName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
        }
        if (SelectedRole != "All")
        {
            list = list.Where(t => t.RoleName == SelectedRole);
        }
        return list;
    }

    private IEnumerable<TutorModel> FilteredTutors
    {
        get
        {
            var allFiltered = GetFilteredTutors();
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

    private void OpenDeleteModal(TutorModel tutor)
    {
        SelectedTutor = tutor;
        ShowModal = true;
    }

    private async Task DeleteTutor()
    {
        if (SelectedTutor == null) return;
        IsProcessing = true;

        var response = await HttpClientService.ExecuteAsync<TutorDeleteResponseModel>(
            $"tutor/{SelectedTutor.TutorId}", EnumHttpMethod.Delete);

        if (response?.IsSuccess == true)
        {
            await LoadTutors();
            ShowModal = false;
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "Delete failed.");
        }
        IsProcessing = false;
    }
}