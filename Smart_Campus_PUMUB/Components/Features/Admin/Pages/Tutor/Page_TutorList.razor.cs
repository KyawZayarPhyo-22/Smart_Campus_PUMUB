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

    protected override async Task OnInitializedAsync() => await LoadTutors();

    private async Task LoadTutors()
    {
        IsLoading = true;
        TutorList = await HttpClientService.ExecuteAsync<List<TutorModel>>("tutor", EnumHttpMethod.Get) ?? new();
        IsLoading = false;
    }

    private IEnumerable<TutorModel> FilteredTutors => string.IsNullOrWhiteSpace(SearchTerm)
        ? TutorList
        : TutorList.Where(t => t.Tutor_Name != null && t.Tutor_Name.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

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
            $"tutor/{SelectedTutor.Tutor_Id}", EnumHttpMethod.Delete);

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