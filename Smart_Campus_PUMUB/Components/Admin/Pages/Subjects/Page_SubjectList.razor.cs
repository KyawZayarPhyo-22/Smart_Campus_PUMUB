using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Subject;

public partial class Page_SubjectList : IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    private List<SubjectModel> SubjectList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private SubjectModel? SelectedSubject { get; set; }

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageSubject = true;

    private string SearchInput = "";
    private string SelectedSemesterInput = "All";
    private string SelectedSemester = "All";

    // Custom Dropdown Open States
    private bool isSemesterDropdownOpen = false;

    private void ToggleSemesterDropdown()
    {
        isSemesterDropdownOpen = !isSemesterDropdownOpen;
    }

    private void SelectSemester(string? semester)
    {
        SelectedSemesterInput = semester ?? "All";
        isSemesterDropdownOpen = false;
    }

    private void CloseAllDropdowns()
    {
        isSemesterDropdownOpen = false;
    }

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        IsLoading = true;
        StateHasChanged();
        await Task.Delay(300);
        SearchTerm = SearchInput;
        SelectedSemester = SelectedSemesterInput;
        CurrentPage = 1;
        IsLoading = false;
        StateHasChanged();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        IsLoading = true;
        StateHasChanged();
        await Task.Delay(300);
        SearchInput = "";
        SearchTerm = "";
        SelectedSemesterInput = "All";
        SelectedSemester = "All";
        CurrentPage = 1;
        IsLoading = false;
        StateHasChanged();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<SubjectModel> GetFilteredSubjects()
    {
        var list = SubjectList.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            list = list.Where(s => (s.SubjectName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                   (s.SubjectCode?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                   (s.MajorName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                   (s.FacultyName?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (SelectedSemester != "All")
        {
            list = list.Where(s => s.SemesterName == SelectedSemester);
        }
        return list;
    }

    private IEnumerable<SubjectModel> FilteredSubjects
    {
        get
        {
            var allFiltered = GetFilteredSubjects();
            int count = allFiltered.Count();
            int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
            TotalPages = calcPages < 1 ? 1 : calcPages;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    private async Task OnPageChanged(int newPage)
    {
        IsLoading = true;
        StateHasChanged();
        await Task.Delay(300);
        CurrentPage = newPage;
        IsLoading = false;
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadSubjects();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            var savedLang = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_dashboard_lang");
            if (!string.IsNullOrEmpty(savedLang))
            {
                LangService.SetLanguage(savedLang);
            }
        }
        catch { }

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            userPermissions = user.Claims
                                  .Where(c => c.Type == "Permission")
                                  .Select(c => c.Value)
                                  .ToList();
                                  
            canManageSubject = userPermissions.Contains("Subject.Edit") || userPermissions.Contains("Subject.Delete");

            StateHasChanged();
        }
    }

    private async Task LoadSubjects()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<List<SubjectModel>>("subject", EnumHttpMethod.Get);
            await Task.WhenAll(fetchTask, delayTask);
            SubjectList = await fetchTask ?? new();
        }
        catch { }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string statusMessage = string.Empty;
    private bool IsSuccess = false;

    private void OpenDeleteModal(SubjectModel subject)
    {
        SelectedSubject = subject;
        statusMessage = string.Empty;
        IsSuccess = false;
        ShowModal = true;
        StateHasChanged();
    }

    private void CloseDeleteModal()
    {
        SelectedSubject = null;
        statusMessage = string.Empty;
        IsSuccess = false;
        ShowModal = false;
    }

    private async Task DeleteSubject()
    {
        if (SelectedSubject == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<ActionResponseModel>(
                $"subject/{SelectedSubject.SubjectId}",
                EnumHttpMethod.Delete);

            if (response?.IsSuccess == true)
            {
                IsSuccess = true;
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");

                await LoadSubjects();
                await Task.Delay(800);

                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;
                statusMessage = LangService.IsMyanmar ? "ဤဘာသာရပ်ကို ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this subject.");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            statusMessage = LangService.IsMyanmar ? "ဤဘာသာရပ်ကို ဖျက်၍ မရပါ။" : $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}