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
    private List<RoleModels> roleModels = new();

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

            // Data များ Add လုပ်ခြင်း

            content.Add(new StringContent(createModel.TutorName ?? ""), "TutorName"); // Model အသစ်အတိုင်း
            content.Add(new StringContent(createModel.UserId.ToString()), "UserId");
            content.Add(new StringContent(createModel.RoleId.ToString()), "RoleId");
            content.Add(new StringContent(createModel.DepartmentId.ToString()), "DepartmentId");
            content.Add(new StringContent(createModel.PositionId.ToString()), "PositionId");
            content.Add(new StringContent(createModel.Email ?? ""), "Email");
            content.Add(new StringContent(createModel.Phone ?? ""), "Phone");
            content.Add(new StringContent(createModel.About ?? ""), "About");

            // ဖိုင် Upload
            if (selectedFile != null)
            {
                var stream = selectedFile.OpenReadStream(1024 * 1024 * 5); // 5MB limit
                var fileContent = new StreamContent(stream);
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
                statusMessage = response?.Message ?? "သိမ်းဆည်းမှု မအောင်မြင်ပါ။";
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }
}