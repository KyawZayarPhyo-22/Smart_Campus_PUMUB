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

    private async Task DeleteSubject()
    {
        if (SelectedSubject == null) return;

        IsProcessing = true;
        try
        {
            // API Route ကို "api/subject/{id}" ဟု ပြောင်းလိုက်ပါ
            var response = await HttpClientService.ExecuteAsync<ActionResponseModel>(
                $"subject/{SelectedSubject.SubjectId}", EnumHttpMethod.Delete);

            if (response?.IsSuccess == true)
            {
                await LoadSubjects(); // အောင်မြင်ပါက စာရင်းအသစ် ပြန်ဆွဲပါ
                CloseDeleteModal();
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်ရန် အဆင်မပြေပါ");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }
}