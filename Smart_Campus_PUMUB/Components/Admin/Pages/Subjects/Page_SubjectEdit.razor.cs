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
    private bool IsProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        // Load lookups in parallel
        var semTask = HttpClientService.ExecuteAsync<List<SemesterModel>>("semester", EnumHttpMethod.Get);
        var facTask = HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
        var dataTask = HttpClientService.ExecuteAsync<SubjectModel>($"subject/{SubjectId}", EnumHttpMethod.Get);

        await Task.WhenAll(semTask, facTask, dataTask);

        SemesterList = semTask.Result ?? new();
        FacultyList  = facTask.Result ?? new();

        var data = dataTask.Result;
        if (data != null)
        {
            subject = new SubjectUpdateRequestModel
            {
                SemesterId  = data.SemesterId,
                FacultyId   = data.FacultyId,
                SubjectName = data.SubjectName,
                SubjectCode = data.SubjectCode
            };
        }
    }

    private async Task UpdateSubject()
    {
        if (subject is null) return;
        ErrorMessage = null;

        IsProcessing = true;
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