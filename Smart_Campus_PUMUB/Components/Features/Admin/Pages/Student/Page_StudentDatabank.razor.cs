using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.Components.Features.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentDatabank : ComponentBase
{
    [Inject]
    public HttpClientService HttpClientService { get; set; }

    [Inject]
    public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private List<StudentPersonalInfoResponse> Students { get; set; } = new();
    private List<StudentPersonalInfoResponse> FilteredStudents { get; set; } = new();
    
    private bool IsLoading { get; set; } = true;

    private int? _userFacultyId = null;
    
    private string _searchTerm = "";
    private string SearchTerm
    {
        get => _searchTerm;
        set
        {
            _searchTerm = value;
            FilterStudents();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadStudentsAsync(null);
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
                    // Reload with faculty filter applied
                    await LoadStudentsAsync(_userFacultyId);
                    StateHasChanged();
                }
            }
        }
    }

    private async Task LoadStudentsAsync(int? facultyId)
    {
        IsLoading = true;
        try
        {
            var url = "studentpersonalinfo";
            if (facultyId.HasValue && facultyId.Value > 0)
            {
                url += $"?facultyId={facultyId.Value}";
            }

            var response = await HttpClientService.ExecuteAsync<List<StudentPersonalInfoResponse>>(url, EnumHttpMethod.Get);
            if (response != null)
            {
                Students = response;
                FilteredStudents = Students.ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading student databank: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterStudents()
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
        {
            FilteredStudents = Students.ToList();
        }
        else
        {
            var lowerCaseSearchTerm = SearchTerm.ToLowerInvariant();
            FilteredStudents = Students.Where(s => 
                (s.student_name_mm != null && s.student_name_mm.ToLowerInvariant().Contains(lowerCaseSearchTerm)) ||
                (s.student_name_en != null && s.student_name_en.ToLowerInvariant().Contains(lowerCaseSearchTerm)) ||
                (s.roll_no != null && s.roll_no.ToLowerInvariant().Contains(lowerCaseSearchTerm)) ||
                (s.father_name != null && s.father_name.ToLowerInvariant().Contains(lowerCaseSearchTerm)) ||
                (s.major != null && s.major.ToLowerInvariant().Contains(lowerCaseSearchTerm)) ||
                (s.FacultyName != null && s.FacultyName.ToLowerInvariant().Contains(lowerCaseSearchTerm))
            ).ToList();
        }
    }
}
