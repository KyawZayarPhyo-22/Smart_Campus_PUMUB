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

    private string roleNo = "";
    private bool isValidatingRoleNo = false;
    private string roleNoValidationMessage = "";
    private bool isTutorProfileExists = false;

    protected override async Task OnInitializedAsync()
    {
        UserList = await HttpClientService.ExecuteAsync<List<UserModels>>("user", EnumHttpMethod.Get) ?? new();
        PositionList = await HttpClientService.ExecuteAsync<List<PositionModel>>("position", EnumHttpMethod.Get) ?? new();
        DepartmentList = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get) ?? new();
        roleModels = await HttpClientService.ExecuteAsync<List<RoleModels>>("role", EnumHttpMethod.Get) ?? new();
    }

    private void HandleFileSelected(InputFileChangeEventArgs e) => selectedFile = e.File;

    private async Task OnRoleNoChanged(ChangeEventArgs e)
    {
        roleNo = e.Value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(roleNo))
        {
            roleNoValidationMessage = "";
            isTutorProfileExists = false;
            return;
        }

        isValidatingRoleNo = true;
        roleNoValidationMessage = "Validating...";
        isTutorProfileExists = false;

        try
        {
            var result = await HttpClientService.ExecuteAsync<RoleNoLookupResult>($"user/roleno/{roleNo}", EnumHttpMethod.Get);
            if (result != null && result.IsSuccess)
            {
                if (result.IsTutorExist)
                {
                    isTutorProfileExists = true;
                    roleNoValidationMessage = "❌ This Tutor profile is already created.";
                    statusMessage = "Tutor profile for this Role No already exists.";
                }
                else
                {
                    roleNoValidationMessage = $"✅ Found: {result.FullName} ({result.RoleName})";
                    createModel.TutorName = result.FullName;
                    createModel.UserId = result.UserId;
                    createModel.RoleId = result.RoleId;
                    statusMessage = "";
                }
            }
            else
            {
                roleNoValidationMessage = "❌ Role No not found in User accounts.";
            }
        }
        catch (Exception ex)
        {
            roleNoValidationMessage = "❌ Error validating Role No.";
            Console.WriteLine(ex.Message);
        }
        finally
        {
            isValidatingRoleNo = false;
        }
    }

    private async Task SaveTutor()
    {
        if (isTutorProfileExists)
        {
            statusMessage = "Cannot save: Tutor profile already exists for this Role No.";
            return;
        }
        if (createModel.UserId == 0)
        {
            statusMessage = "Cannot save: Please assign a valid User or lookup by Role No.";
            return;
        }

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
                //await JSRuntime.InvokeVoidAsync("alert", "Success!");
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

    private class RoleNoLookupResult
    {
        public bool IsSuccess { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? FullName { get; set; }
        public bool IsTutorExist { get; set; }
    }
}