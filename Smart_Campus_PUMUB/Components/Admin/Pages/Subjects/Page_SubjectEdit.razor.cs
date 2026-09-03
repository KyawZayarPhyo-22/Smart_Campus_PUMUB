using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Subject;

public partial class Page_SubjectEdit
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;
    [Parameter] public int SubjectId { get; set; }

    private string? ErrorMessage;
    private SubjectUpdateRequestModel? subject;
    private List<SemesterModel> SemesterList = new();
    private List<FacultyModel> FacultyList = new();
    private List<MajorModel> MajorList = new();
    private List<SubjectModel> SubjectList = new();
    private int SelectedPrerequisiteId = 0;

    private IEnumerable<MajorModel> FilteredMajors =>
        subject != null && subject.FacultyId.HasValue && subject.FacultyId > 0
            ? MajorList.Where(m => m.FacultyId == subject.FacultyId.Value)
            : MajorList;

    private bool IsProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        // Load lookups in parallel
        var semTask = HttpClientService.ExecuteAsync<List<SemesterModel>>("semester", EnumHttpMethod.Get);
        var facTask = HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
        var majTask = HttpClientService.ExecuteAsync<List<MajorModel>>("major", EnumHttpMethod.Get);
        var subTask = HttpClientService.ExecuteAsync<List<SubjectModel>>("subject", EnumHttpMethod.Get);
        var dataTask = HttpClientService.ExecuteAsync<SubjectModel>($"subject/{SubjectId}", EnumHttpMethod.Get);

        await Task.WhenAll(semTask, facTask, majTask, subTask, dataTask);

        SemesterList = semTask.Result ?? new();
        FacultyList  = facTask.Result ?? new();
        MajorList    = majTask.Result ?? new();
        SubjectList  = subTask.Result ?? new();

        var data = dataTask.Result;
        if (data != null)
        {
            if (data.PrerequisiteSubjectIds != null && data.PrerequisiteSubjectIds.Any())
            {
                SelectedPrerequisiteId = data.PrerequisiteSubjectIds.First();
            }
            else
            {
                SelectedPrerequisiteId = 0;
            }
            subject = new SubjectUpdateRequestModel
            {
                SemesterId  = data.SemesterId,
                FacultyId   = data.FacultyId,
                MajorId     = data.MajorId,
                SubjectName = data.SubjectName,
                SubjectCode = data.SubjectCode,
                Credit      = data.Credit,
                SubjectType = data.SubjectType
            };
        }
    }

    private async Task UpdateSubject()
    {
        if (subject is null) return;
        ErrorMessage = null;

        IsProcessing = true;
        
        if (SelectedPrerequisiteId > 0)
        {
            subject.PrerequisiteSubjectIds = new List<int> { SelectedPrerequisiteId };
        }
        else
        {
            subject.PrerequisiteSubjectIds = new List<int>();
        }

        var response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"subject/{SubjectId}", EnumHttpMethod.Put, subject);

        if (response?.IsSuccess == true)
        {
            Nav.NavigateTo("/admin/subjects");
        }
        else
        {
            ErrorMessage = response?.Message ?? "Update လုပ်၍မရပါ။";
            IsProcessing = false;
        }
    }
}