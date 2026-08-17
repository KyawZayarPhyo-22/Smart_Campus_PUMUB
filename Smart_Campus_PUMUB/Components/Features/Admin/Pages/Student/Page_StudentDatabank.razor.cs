using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.Components.Features.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentDatabank : ComponentBase
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<StudentPersonalInfoResponse> Students { get; set; } = new();
    private List<FacultyModel> FacultyList { get; set; } = new();
    private List<MajorModel> MajorList { get; set; } = new();

    private bool IsLoading { get; set; } = true;

    // Filters
    private string SearchInput { get; set; } = "";
    private string SelectedFacultyInput { get; set; } = "All";
    private string SelectedMajorInput { get; set; } = "All";

    private string SearchTerm { get; set; } = "";
    private string SelectedFaculty { get; set; } = "All";
    private string SelectedMajor { get; set; } = "All";

    private bool isFacultyAdminLocked = false;
    private int? _userFacultyId = null;

    // Pagination
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;
    private int TotalCount { get; set; } = 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadMetadataAsync();
        await LoadStudentsAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var roleName = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var roleIdStr = user.FindFirst("RoleId")?.Value;
            bool isSuperAdmin = string.Equals(roleName, "Super Admin", StringComparison.OrdinalIgnoreCase) || roleIdStr == "4";

            if (!isSuperAdmin)
            {
                var facultyIdStr = user.FindFirst("FacultyId")?.Value;
                if (!string.IsNullOrEmpty(facultyIdStr) && int.TryParse(facultyIdStr, out int fid) && fid > 0)
                {
                    _userFacultyId = fid;
                    var fac = FacultyList.FirstOrDefault(f => f.FacultyId == fid);
                    if (fac != null && !string.IsNullOrEmpty(fac.FacultyName))
                    {
                        SelectedFacultyInput = fac.FacultyName;
                        SelectedFaculty = fac.FacultyName;
                        isFacultyAdminLocked = true;
                    }
                    await LoadStudentsAsync();
                    StateHasChanged();
                }
            }
        }
    }

    private async Task LoadMetadataAsync()
    {
        try
        {
            var facultyTask = HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            var majorTask = HttpClientService.ExecuteAsync<List<MajorModel>>("major", EnumHttpMethod.Get);

            await Task.WhenAll(facultyTask, majorTask);

            FacultyList = await facultyTask ?? new();
            MajorList = await majorTask ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading metadata: {ex.Message}");
        }
    }

    private async Task LoadStudentsAsync()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(1000);

            int? queryFacultyId = null;
            if (_userFacultyId.HasValue && _userFacultyId.Value > 0)
            {
                queryFacultyId = _userFacultyId.Value;
            }
            else if (SelectedFaculty != "All")
            {
                var fac = FacultyList.FirstOrDefault(f => string.Equals(f.FacultyName?.Trim(), SelectedFaculty.Trim(), StringComparison.OrdinalIgnoreCase));
                if (fac != null && fac.FacultyId > 0)
                {
                    queryFacultyId = fac.FacultyId;
                }
            }

            var queryParams = new List<string>
            {
                $"pageNumber={CurrentPage}",
                $"pageSize={PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                queryParams.Add($"searchTerm={Uri.EscapeDataString(SearchTerm.Trim())}");
            }

            if (queryFacultyId.HasValue && queryFacultyId.Value > 0)
            {
                queryParams.Add($"facultyId={queryFacultyId.Value}");
            }

            if (SelectedMajor != "All" && !string.IsNullOrWhiteSpace(SelectedMajor))
            {
                queryParams.Add($"major={Uri.EscapeDataString(SelectedMajor.Trim())}");
            }

            var url = $"studentpersonalinfo/paginate?{string.Join("&", queryParams)}";
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<StudentPersonalInfoResponse>>(url, EnumHttpMethod.Get);

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;

            if (response != null)
            {
                Students = response.Items ?? new();
                TotalCount = response.TotalCount;
                TotalPages = response.TotalPages < 1 ? 1 : response.TotalPages;
                CurrentPage = response.PageNumber;
            }
            else
            {
                Students = new();
                TotalCount = 0;
                TotalPages = 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading student databank: {ex.Message}");
            Students = new();
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private IEnumerable<string> AvailableFaculties
    {
        get
        {
            return FacultyList
                .Where(f => !string.IsNullOrWhiteSpace(f.FacultyName))
                .Select(f => f.FacultyName!.Trim())
                .Distinct()
                .OrderBy(f => f);
        }
    }

    private IEnumerable<string> AvailableMajors
    {
        get
        {
            var majorsQuery = MajorList.AsEnumerable();

            if (SelectedFacultyInput != "All")
            {
                majorsQuery = majorsQuery.Where(m =>
                    string.Equals(m.FacultyName?.Trim(), SelectedFacultyInput.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            return majorsQuery
                .Where(m => !string.IsNullOrWhiteSpace(m.MajorName) && !string.Equals(m.MajorName.Trim(), "N/A", StringComparison.OrdinalIgnoreCase))
                .Select(m => m.MajorName!.Trim())
                .Distinct()
                .OrderBy(m => m);
        }
    }

    // Custom Dropdown Open States
    private bool isFacultyDropdownOpen = false;
    private bool isMajorDropdownOpen = false;

    private void ToggleFacultyDropdown()
    {
        if (isFacultyAdminLocked) return;
        isMajorDropdownOpen = false;
        isFacultyDropdownOpen = !isFacultyDropdownOpen;
    }

    private void ToggleMajorDropdown()
    {
        isFacultyDropdownOpen = false;
        isMajorDropdownOpen = !isMajorDropdownOpen;
    }

    private void SelectFaculty(string facultyName)
    {
        SelectedFacultyInput = facultyName;
        SelectedMajorInput = "All";
        isFacultyDropdownOpen = false;
        // Search button ကို နှိပ်မှသာ Filter လုပ်မည် (Auto-search မလုပ်ပါ)
    }

    private void SelectMajor(string majorName)
    {
        SelectedMajorInput = majorName;
        isMajorDropdownOpen = false;
        // Search button ကို နှိပ်မှသာ Filter လုပ်မည် (Auto-search မလုပ်ပါ)
    }

    private void CloseAllDropdowns()
    {
        isFacultyDropdownOpen = false;
        isMajorDropdownOpen = false;
    }

    private void OnFacultyChanged()
    {
        SelectedMajorInput = "All";
    }

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        SearchTerm = SearchInput;
        SelectedFaculty = SelectedFacultyInput;
        SelectedMajor = SelectedMajorInput;
        CurrentPage = 1;
        await LoadStudentsAsync();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        SearchInput = "";
        if (!isFacultyAdminLocked)
        {
            SelectedFacultyInput = "All";
            SelectedFaculty = "All";
        }
        SelectedMajorInput = "All";
        SelectedMajor = "All";
        SearchTerm = "";
        CurrentPage = 1;
        await LoadStudentsAsync();
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private async Task OnPageChanged(int newPage)
    {
        if (newPage >= 1 && newPage <= TotalPages && newPage != CurrentPage)
        {
            CurrentPage = newPage;
            await LoadStudentsAsync();
        }
    }
}
