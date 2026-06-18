
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Department;

public partial class Page_DepartmentList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<DepartmentModel> DepartmentList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;

    private string SearchInput = "";
    private string SelectedFacultyInput = "All";
    private string SelectedFaculty = "All";

    private void ApplyFilter()
    {
        SearchTerm = SearchInput;
        SelectedFaculty = SelectedFacultyInput;
        CurrentPage = 1;
        StateHasChanged();
    }

    private void ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        SelectedFacultyInput = "All";
        SelectedFaculty = "All";
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

    private string statusMessage;

    public bool IsSuccess { get; private set; }
    private bool ShowModal { get; set; } = false;
    private DepartmentModel? SelectedDepartment { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<DepartmentModel> GetFilteredDepartments()
    {
        var list = DepartmentList.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            list = list.Where(d => d.DepartmentName != null && d.DepartmentName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
        }
        if (SelectedFaculty != "All")
        {
            list = list.Where(d => d.FacultyName == SelectedFaculty);
        }
        return list;
    }

    private IEnumerable<DepartmentModel> FilteredDepartments
    {
        get
        {
            var allFiltered = GetFilteredDepartments();
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

    protected override async Task OnInitializedAsync() => await LoadDepartments();

    private async Task LoadDepartments()
    {
        IsLoading = true;
        try
        {
            // API Endpoint (သင့် Controller တွင် 'department' ဟု သတ်မှတ်ထားသည်ဟု ယူဆပါသည်)
            var response = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get);
            if (response != null) DepartmentList = response;
        }
        catch { }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(DepartmentModel dept)
    {
        SelectedDepartment = dept;

        // 🔥 RESET HERE
        statusMessage = "";
        IsSuccess = false;

        ShowModal = true;
    }
    private void CloseDeleteModal()
    {
        SelectedDepartment = null;
        ShowModal = false;

        // 🔥 CLEAR MESSAGE
        statusMessage = "";
        IsSuccess = false;
    }

    private async Task DeleteDepartment()
    {
        if (SelectedDepartment == null) return;

        IsProcessing = true;
        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<DepartmentResponseModel>(
                $"department/{SelectedDepartment.DepartmentId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
                IsSuccess = true;

                await Task.Delay(1500);

                CloseDeleteModal();
                await LoadDepartments();
            }
            else
            {
                statusMessage = response?.Message
                    ?? "ဤ Department ကို အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";

                IsSuccess = false;
            }
        }
        catch
        {
            statusMessage = "ဤ Department ကို အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}