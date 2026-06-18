using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Subject;

public partial class Page_SubjectList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<SubjectModel> SubjectList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private SubjectModel? SelectedSubject { get; set; }

    private IEnumerable<SubjectModel> FilteredSubjects => string.IsNullOrWhiteSpace(SearchTerm)
        ? SubjectList
        : SubjectList.Where(s => (s.SubjectName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                 (s.SubjectCode?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));

    protected override async Task OnInitializedAsync() => await LoadSubjects();

    private async Task LoadSubjects()
    {
        IsLoading = true;
        // API Route ကို "api/subject" ဟု ပြောင်းလိုက်ပါ
        SubjectList = await HttpClientService.ExecuteAsync<List<SubjectModel>>("subject", EnumHttpMethod.Get) ?? new();
        IsLoading = false;
    }

    private void OpenDeleteModal(SubjectModel subject)
    {
        SelectedSubject = subject;
        ShowModal = true;
        StateHasChanged();
    }

    private void CloseDeleteModal()
    {
        SelectedSubject = null;
        ShowModal = false;
    }

    private string statusMessage = string.Empty;
    private bool IsSuccess = false;

    private async Task DeleteSubject()
    {
        if (SelectedSubject == null) return;

        IsProcessing = true;

        statusMessage = string.Empty;
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<ActionResponseModel>(
                $"subject/{SelectedSubject.SubjectId}",
                EnumHttpMethod.Delete);

            if (response?.IsSuccess == true)
            {
                IsSuccess = true;
                statusMessage = response.Message ?? "Subject ကို အောင်မြင်စွာ ဖျက်ပြီးပါပြီ။";

                await LoadSubjects();
                await Task.Delay(800);

                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;
                statusMessage = response?.Message ?? "Subject ကို ဖျက်၍ မရပါ။";
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }
}