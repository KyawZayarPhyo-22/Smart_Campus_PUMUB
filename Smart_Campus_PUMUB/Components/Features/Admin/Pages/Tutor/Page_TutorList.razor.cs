// using Microsoft.AspNetCore.Components;
// using Microsoft.JSInterop;
// using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
// using Smart_Campus_PUMUB.WebApi.Models;

// namespace Smart_Campus_PUMUB.Components.Admin.Pages.Tutor;

// public partial class Page_TutorList
// {
//     [Inject] public HttpClientService HttpClientService { get; set; } = null!;
//     [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

//     private List<TutorModel> TutorList = new();
//     private string _searchTerm = "";
//     private bool IsLoading = true;
//     private bool IsProcessing = false;
    
//     // Delete Modal
//     private bool ShowModal = false;
//     private TutorModel? SelectedTutor;

//     // Search Logic with Property Setter
//     public string SearchTerm 
//     { 
//         get => _searchTerm; 
//         set { _searchTerm = value; StateHasChanged(); } 
//     }

//     private string SearchInput = "";
//     private string SelectedRoleInput = "All";
//     private string SelectedRole = "All";

//     private void ApplyFilter()
//     {
//         SearchTerm = SearchInput;
//         SelectedRole = SelectedRoleInput;
//         CurrentPage = 1;
//         StateHasChanged();
//     }

//     private void ResetFilter()
//     {
//         SearchInput = "";
//         SearchTerm = "";
//         SelectedRoleInput = "All";
//         SelectedRole = "All";
//         CurrentPage = 1;
//         StateHasChanged();
//     }

//     private void HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
//     {
//         if (e.Key == "Enter")
//         {
//             ApplyFilter();
//         }
//     }

//     protected override async Task OnInitializedAsync() => await LoadTutors();

//     private async Task LoadTutors()
//     {
//         IsLoading = true;
//         TutorList = await HttpClientService.ExecuteAsync<List<TutorModel>>("tutor", EnumHttpMethod.Get) ?? new();
//         IsLoading = false;
//     }

//     // Pagination Variables
//     private int CurrentPage { get; set; } = 1;
//     private int PageSize { get; set; } = 10;
//     private int TotalPages { get; set; } = 1;

//     private IEnumerable<TutorModel> GetFilteredTutors()
//     {
//         var list = TutorList.AsEnumerable();
//         if (!string.IsNullOrWhiteSpace(SearchTerm))
//         {
//             list = list.Where(t => t.TutorName != null && t.TutorName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
//         }
//         if (SelectedRole != "All")
//         {
//             list = list.Where(t => t.RoleName == SelectedRole);
//         }
//         return list;
//     }

//     private IEnumerable<TutorModel> FilteredTutors
//     {
//         get
//         {
//             var allFiltered = GetFilteredTutors();
//             int count = allFiltered.Count();
//             int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
//             TotalPages = calcPages < 1 ? 1 : calcPages;
//             if (CurrentPage > TotalPages) CurrentPage = TotalPages;
//             return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
//         }
//     }

//     private void OnPageChanged(int newPage)
//     {
//         CurrentPage = newPage;
//         StateHasChanged();
//     }

//     private void OpenDeleteModal(TutorModel tutor)
//     {
//         SelectedTutor = tutor;
//         ShowModal = true;
//     }

//     private async Task DeleteTutor()
//     {
//         if (SelectedTutor == null) return;
//         IsProcessing = true;

//         var response = await HttpClientService.ExecuteAsync<TutorDeleteResponseModel>(
//             $"tutor/{SelectedTutor.TutorId}", EnumHttpMethod.Delete);

//         if (response?.IsSuccess == true)
//         {
//             await LoadTutors();
//             ShowModal = false;
//         }
//         else
//         {
//             await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "Delete failed.");
//         }
//         IsProcessing = false;
//     }
// }
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Tutor;

public partial class Page_TutorList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<TutorModel> TutorList = new();
    private string _searchTerm = "";
    private bool IsLoading = true;
    private bool IsProcessing = false;
    
    // Delete Modal
    private bool ShowModal = false;
    private TutorModel? SelectedTutor;

    // Search, Faculty & Position Filter Logic
    public string SearchTerm 
    { 
        get => _searchTerm; 
        set { _searchTerm = value; StateHasChanged(); } 
    }

    private string SearchInput = "";
    private string SelectedFacultyInput = "All";
    private string SelectedFaculty = "All";
    private string SelectedPositionInput = "All";
    private string SelectedPosition = "All";
    private List<FacultyModel> FacultyList = new();
    private List<PositionModel> PositionList = new();
    private bool isFacultyAdminLocked = false;

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageTutor = true;

    // Faculty-based scoping
    private int? _userFacultyId = null;

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    protected override async Task OnInitializedAsync()
    {
        // Only load data here - auth state is NOT yet readable during SSR
        await LoadFaculties();
        await LoadPositions();
        await LoadTutors();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        // Re-read auth AFTER the SignalR circuit connects (ProtectedSessionStorage is now readable)
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            userPermissions = user.Claims
                                  .Where(c => c.Type == "Permission")
                                  .Select(c => c.Value)
                                  .ToList();
                                  
            // Permission ကို စစ်ဆေးပြီး Action Buttons များကို ထိန်းချုပ်ရန်
            canManageTutor = userPermissions.Contains("Tutor.Edit") || userPermissions.Contains("Tutor.Delete");

            // Faculty-based scoping: read FacultyId claim for FC/FE Admins
            var roleName = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var roleIdStr = user.FindFirst("RoleId")?.Value;
            bool isSuperAdmin = string.Equals(roleName, "Super Admin", StringComparison.OrdinalIgnoreCase) || roleIdStr == "4";

            if (!isSuperAdmin)
            {
                var facultyIdStr = user.FindFirst("FacultyId")?.Value;
                if (!string.IsNullOrEmpty(facultyIdStr) && int.TryParse(facultyIdStr, out int fid) && fid > 0)
                {
                    _userFacultyId = fid;
                    isFacultyAdminLocked = true;
                    var myFaculty = FacultyList.FirstOrDefault(f => f.FacultyId == fid);
                    if (myFaculty != null)
                    {
                        SelectedFacultyInput = myFaculty.FacultyName;
                        SelectedFaculty = myFaculty.FacultyName;
                    }
                    // Reload with faculty filter applied
                    CurrentPage = 1;
                    await LoadTutors();
                }
            }

            // Trigger re-render so the table header and action buttons update
            StateHasChanged();
        }
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading faculties: {ex.Message}");
        }
    }

    private async Task LoadPositions()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<PositionModel>>("position", EnumHttpMethod.Get);
            if (response != null)
            {
                PositionList = response;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading positions: {ex.Message}");
        }
    }

    private async Task LoadTutors()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(1000);
            string facultyParam = SelectedFaculty != "All" ? $"&facultyName={Uri.EscapeDataString(SelectedFaculty)}" : "";
            string positionParam = SelectedPosition != "All" ? $"&positionName={Uri.EscapeDataString(SelectedPosition)}" : "";
            string scopeParam = (_userFacultyId.HasValue && _userFacultyId.Value > 0) ? $"&facultyId={_userFacultyId.Value}" : "";

            var url = $"tutor/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}{facultyParam}{positionParam}{scopeParam}";

            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<TutorModel>>(
                url,
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;

            if (response != null)
            {
                TutorList = response.Items ?? new();
                TotalPages = response.TotalPages < 1 ? 1 : response.TotalPages;
            }
            else
            {
                TutorList = new();
                TotalPages = 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tutors: {ex.Message}");
            TutorList = new();
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    // Custom Dropdown Open States
    private bool isFacultyDropdownOpen = false;
    private bool isPositionDropdownOpen = false;

    private void ToggleFacultyDropdown()
    {
        if (isFacultyAdminLocked) return;
        isPositionDropdownOpen = false;
        isFacultyDropdownOpen = !isFacultyDropdownOpen;
    }

    private void TogglePositionDropdown()
    {
        isFacultyDropdownOpen = false;
        isPositionDropdownOpen = !isPositionDropdownOpen;
    }

    private void SelectFaculty(string? facultyName)
    {
        SelectedFacultyInput = facultyName ?? "All";
        isFacultyDropdownOpen = false;
    }

    private void SelectPosition(string? positionName)
    {
        SelectedPositionInput = positionName ?? "All";
        isPositionDropdownOpen = false;
    }

    private void CloseAllDropdowns()
    {
        isFacultyDropdownOpen = false;
        isPositionDropdownOpen = false;
    }

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        SearchTerm = SearchInput;
        SelectedFaculty = SelectedFacultyInput;
        SelectedPosition = SelectedPositionInput;
        CurrentPage = 1;
        await LoadTutors();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        SearchInput = "";
        SearchTerm = "";
        if (!isFacultyAdminLocked)
        {
            SelectedFacultyInput = "All";
            SelectedFaculty = "All";
        }
        SelectedPositionInput = "All";
        SelectedPosition = "All";
        CurrentPage = 1;
        await LoadTutors();
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private IEnumerable<TutorModel> FilteredTutors => TutorList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadTutors();
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