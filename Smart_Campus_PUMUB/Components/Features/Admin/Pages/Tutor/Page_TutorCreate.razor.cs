using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Tutor;

public partial class Page_TutorCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    private TutorCreateRequestModel createModel = new();
    private List<PositionModel> PositionList = new();
    private List<DepartmentModel> DepartmentList = new();
    private List<UserModels> UserList = new();
    
    private IBrowserFile? selectedFile;
    private bool isProcessing = false;
    private string statusMessage = "";

    protected override async Task OnInitializedAsync()
    {
        UserList = await HttpClientService.ExecuteAsync<List<UserModels>>("user", EnumHttpMethod.Get) ?? new();
        PositionList = await HttpClientService.ExecuteAsync<List<PositionModel>>("position", EnumHttpMethod.Get) ?? new();
        DepartmentList = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get) ?? new();
    }

    private void HandleFileSelected(InputFileChangeEventArgs e) => selectedFile = e.File;

    private async Task SaveTutor()
    {
        isProcessing = true;
        statusMessage = "Saving...";

        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(createModel.Tutor_Name ?? ""), "Tutor_Name");
            content.Add(new StringContent(createModel.Email ?? ""), "Email");
            content.Add(new StringContent(createModel.Phone ?? ""), "Phone");
            content.Add(new StringContent(createModel.About ?? ""), "About");
            content.Add(new StringContent(createModel.Department_Id.ToString()), "Department_Id");
            content.Add(new StringContent(createModel.Position_Id.ToString()), "Position_id");
            content.Add(new StringContent(createModel.User_Id.ToString()), "User_Id");

            if (selectedFile != null)
            {
                var fileContent = new StreamContent(selectedFile.OpenReadStream(1024 * 1024 * 5));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(selectedFile.ContentType);
                content.Add(fileContent, "PhotoFile", selectedFile.Name);
            }

            var response = await HttpClientService.ExecuteMultipartAsync<TutorCreateResponseModel>("tutor", content);

            if (response?.IsSuccess == true)
            {
                await JSRuntime.InvokeVoidAsync("alert", "Success!");
                Nav.NavigateTo("/admin/tutors");
            }
            else
            {
                statusMessage = "jijdj";
            }
        }
        catch (Exception )
        {
            statusMessage = "hiiiii";
        }
        finally
        {
            isProcessing = false;
        }
    }
}