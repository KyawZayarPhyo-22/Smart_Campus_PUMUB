using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.Components.Features.Services;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentDatabank : ComponentBase
{
    [Inject]
    public HttpClientService HttpClientService { get; set; }

    private List<StudentPersonalInfoResponse> Students { get; set; } = new();
    private List<StudentPersonalInfoResponse> FilteredStudents { get; set; } = new();
    
    private bool IsLoading { get; set; } = true;
    
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
        await LoadStudentsAsync();
    }

    private async Task LoadStudentsAsync()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<StudentPersonalInfoResponse>>("studentpersonalinfo", EnumHttpMethod.Get);
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
                (s.major != null && s.major.ToLowerInvariant().Contains(lowerCaseSearchTerm))
            ).ToList();
        }
    }
}
