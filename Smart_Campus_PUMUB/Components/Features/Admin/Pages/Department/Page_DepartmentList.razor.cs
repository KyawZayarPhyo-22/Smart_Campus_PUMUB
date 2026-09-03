
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Department;

public partial class Page_DepartmentList : ComponentBase, IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    private List<DepartmentModel> DepartmentList { get; set; } = new();
    private List<Smart_Campus_PUMUB.Database.AppDbContext.Faculty> FacultyList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageDepartment = true;

    private string SearchInput = "";
    private string SelectedFacultyInput = "All";
    private string SelectedFaculty = "All";

    // Custom Dropdown Open States
    private bool isFacultyDropdownOpen = false;

    private void ToggleFacultyDropdown()
    {
        isFacultyDropdownOpen = !isFacultyDropdownOpen;
    }

    private void SelectFaculty(string? facultyName)
    {
        SelectedFacultyInput = facultyName ?? "All";
        isFacultyDropdownOpen = false;
    }

    private void CloseAllDropdowns()
    {
        isFacultyDropdownOpen = false;
    }

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        SearchTerm = SearchInput;
        SelectedFaculty = SelectedFacultyInput;
        CurrentPage = 1;
        await LoadDepartments();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        SearchInput = "";
        SearchTerm = "";
        SelectedFacultyInput = "All";
        SelectedFaculty = "All";
        CurrentPage = 1;
        await LoadDepartments();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private string statusMessage = "";

    public bool IsSuccess { get; private set; }
    private bool ShowModal { get; set; } = false;
    private DepartmentModel? SelectedDepartment { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<DepartmentModel> FilteredDepartments => DepartmentList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadDepartments();
    }

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadFaculties();
        await LoadDepartments();
    }

    private async Task LoadFaculties()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<Smart_Campus_PUMUB.Database.AppDbContext.Faculty>>("faculty", EnumHttpMethod.Get);
            if (response != null) FacultyList = response;
        }
        catch { }
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
                                  
            canManageDepartment = userPermissions.Contains("Department.Edit") || userPermissions.Contains("Department.Delete");

            StateHasChanged();
        }
    }

    private async Task LoadDepartments()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<DepartmentModel>>(
                $"department/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}&facultyName={Uri.EscapeDataString(SelectedFaculty)}", 
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;
            if (response != null)
            {
                DepartmentList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch { }
        finally 
        { 
            IsLoading = false; 
            StateHasChanged();
        }
    }

    private void OpenDeleteModal(DepartmentModel dept)
    {
        SelectedDepartment = dept;
        statusMessage = "";
        IsSuccess = false;
        ShowModal = true;
    }

    private void CloseDeleteModal()
    {
        SelectedDepartment = null;
        ShowModal = false;
        statusMessage = "";
        IsSuccess = false;
    }

    private async Task DeleteDepartment()
    {
        if (SelectedDepartment == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<DepartmentResponseModel>(
                $"department/{SelectedDepartment.DepartmentId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                IsSuccess = true;

                await Task.Delay(1000);

                CloseDeleteModal();
                await LoadDepartments();
            }
            else
            {
                statusMessage = LangService.IsMyanmar ? "ဤ Department ကို အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this department because it is in use.");
                IsSuccess = false;
            }
        }
        catch
        {
            statusMessage = LangService.IsMyanmar ? "ဤ Department ကို အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။" : "Cannot delete this department because it is in use.";
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}