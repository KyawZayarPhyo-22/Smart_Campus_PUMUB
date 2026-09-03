using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.User;

public partial class Page_UserList : IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    private List<UserModel> UserList { get; set; } = new();
    private string SearchTerm { get; set; } = "";

    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageUser = true;

    private string SearchInput = "";
    private string SelectedRoleInput = "All";
    private string SelectedRole = "All";
    private string SelectedFacultyInput = "All";
    private string SelectedFaculty = "All";

    private bool isRoleDropdownOpen = false;
    private bool isFacultyDropdownOpen = false;

    private void ToggleRoleDropdown()
    {
        isRoleDropdownOpen = !isRoleDropdownOpen;
        if (isRoleDropdownOpen) isFacultyDropdownOpen = false;
    }

    private void ToggleFacultyDropdown()
    {
        isFacultyDropdownOpen = !isFacultyDropdownOpen;
        if (isFacultyDropdownOpen) isRoleDropdownOpen = false;
    }

    private void SelectRole(string? roleName)
    {
        SelectedRoleInput = roleName ?? "All";
        isRoleDropdownOpen = false;
    }

    private void SelectFaculty(string? facultyName)
    {
        SelectedFacultyInput = facultyName ?? "All";
        isFacultyDropdownOpen = false;
    }

    private void CloseAllDropdowns()
    {
        isRoleDropdownOpen = false;
        isFacultyDropdownOpen = false;
    }

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        SearchTerm = SearchInput;
        SelectedRole = SelectedRoleInput;
        SelectedFaculty = SelectedFacultyInput;
        CurrentPage = 1;
        await LoadUsers();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        SearchInput = "";
        SearchTerm = "";
        SelectedRoleInput = "All";
        SelectedRole = "All";
        SelectedFacultyInput = "All";
        SelectedFaculty = "All";
        CurrentPage = 1;
        await LoadUsers();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    // Delete Modal Controls
    private bool ShowModal { get; set; } = false;
    private UserModel? SelectedUser { get; set; }
    private string statusMessage = "";
    private bool IsSuccess = false;

    // Status Modal Controls
    private bool ShowStatusModal { get; set; } = false;
    private UserModel? SelectedUserForStatus { get; set; }
    private string TargetStatusString => (SelectedUserForStatus?.Status == "Active") ? "Inactive" : "Active";

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;
    private int TotalCount { get; set; } = 0;

    private IEnumerable<UserModel> FilteredUsers => UserList;
    private List<RoleModel> RoleList { get; set; } = new();
    private List<FacultyModel> FacultyList { get; set; } = new();

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadUsers();
    }

    private async Task LoadRoles()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<RoleModel>>("role", EnumHttpMethod.Get);
            if (response != null)
            {
                RoleList = response;
            }
        }
        catch { }
    }

    private async Task LoadFaculties()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            if (response != null)
            {
                FacultyList = response;
            }
        }
        catch { }
    }

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadRoles();
        await LoadFaculties();
        await LoadUsers();
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
                                  
            canManageUser = userPermissions.Contains("User.Edit") || userPermissions.Contains("User.Delete");

            StateHasChanged();
        }
    }

    private async Task LoadUsers()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<UserModel>>(
                $"user/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}&roleName={Uri.EscapeDataString(SelectedRole)}&facultyName={Uri.EscapeDataString(SelectedFaculty)}",
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;

            if (response != null)
            {
                UserList = response.Items;
                TotalCount = response.TotalCount;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = LangService.IsMyanmar ? $"ဒေတာဆွဲယူရာတွင် အမှားအယွင်းရှိပါသည်။ Error: {ex.Message}" : $"Failed to load data. Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void OpenDeleteModal(UserModel user)
    {
        SelectedUser = user;
        ShowModal = true;
        statusMessage = "";
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedUser = null;
        ShowModal = false;
        statusMessage = "";
        IsSuccess = false;
    }

    private async Task DeleteUser()
    {
        if (SelectedUser == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<UserDeleteResponseModel>(
                $"user/{SelectedUser.UserId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                IsSuccess = true;

                await LoadUsers();
                await Task.Delay(800);
                CloseDeleteModal();
            }
            else
            {
                statusMessage = LangService.IsMyanmar ? "ဤ အသုံးပြုသူ ကို ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this user.");
                IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void ToggleUserStatus(UserModel user)
    {
        SelectedUserForStatus = user;
        ShowStatusModal = true;
    }

    private void CloseStatusModal()
    {
        SelectedUserForStatus = null;
        ShowStatusModal = false;
    }

    private async Task ConfirmToggleUserStatus()
    {
        if (SelectedUserForStatus == null) return;
        IsProcessing = true;

        try
        {
            var response = await HttpClientService.ExecuteAsync<ToggleStatusResponse>(
                $"user/toggle-status/{SelectedUserForStatus.UserId}",
                EnumHttpMethod.Patch
            );

            if (response != null && response.IsSuccess)
            {
                SelectedUserForStatus.Status = response.Status;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling user status: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
            CloseStatusModal();
        }
    }

    public class ToggleStatusResponse
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public string? Status { get; set; }
    }

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}