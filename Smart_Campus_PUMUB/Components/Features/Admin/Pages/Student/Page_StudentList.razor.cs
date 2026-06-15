using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<StudentModel> StudentList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private StudentModel? SelectedStudent { get; set; }

    private IEnumerable<StudentModel> FilteredStudents
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
                return StudentList;

            return StudentList.Where(s =>
                (s.CurrentRollNo?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.CurrentMajor?.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ?? false)); // Major ကိုပါ ရှာလို့ရအောင် ထည့်ထားပေးပါ
        }
    }

    protected override async Task OnInitializedAsync() => await LoadStudents();

    private async Task LoadStudents()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<StudentModel>>("student", EnumHttpMethod.Get);
            if (response != null) StudentList = response;
        }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(StudentModel student) { SelectedStudent = student; ShowModal = true; }
    private void CloseDeleteModal() { SelectedStudent = null; ShowModal = false; }

    private async Task DeleteStudent()
    {
        if (SelectedStudent == null) return;
        IsProcessing = true;
        try
        {
            // API တွင် DELETE method ရှိရပါမည်
            var response = await HttpClientService.ExecuteAsync<StudentResponseModel>($"student/{SelectedStudent.StudentId}", EnumHttpMethod.Delete);
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", "Deleted Successfully!");
                CloseDeleteModal();
                await LoadStudents();
            }
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", ex.Message); }
        finally { IsProcessing = false; }
    }
}