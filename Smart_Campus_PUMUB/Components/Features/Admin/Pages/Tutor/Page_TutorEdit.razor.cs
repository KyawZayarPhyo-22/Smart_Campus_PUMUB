using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Tutor;

public partial class Page_TutorEdit
{
    [Parameter] public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    private TutorUpdateRequestModel createModel = new();
    private List<PositionModel> PositionList = new();
    private List<DepartmentModel> DepartmentList = new();
    private List<UserModels> UserList = new();

    private IBrowserFile? selectedFile;
    private bool IsLoading = true;
    private bool isProcessing = false;
    private string statusMessage = "";

    // protected override async Task OnInitializedAsync()
    // {
    //     // ၁။ Dropdown များအတွက် Data ဆွဲယူခြင်း
    //     UserList = await HttpClientService.ExecuteAsync<List<UserModels>>("user", EnumHttpMethod.Get) ?? new();
    //     PositionList = await HttpClientService.ExecuteAsync<List<PositionModel>>("position", EnumHttpMethod.Get) ?? new();
    //     DepartmentList = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get) ?? new();

    //     // ၂။ Edit လုပ်မည့် Tutor အချက်အလက်များ အရင်ဆွဲယူခြင်း
    //     var existingTutor = await HttpClientService.ExecuteAsync<TutorModel>($"tutor/{Id}", EnumHttpMethod.Get);

    //     if (existingTutor != null)
    //     {
    //         createModel = new TutorUpdateRequestModel
    //         {
    //             Tutor_Name = existingTutor.Tutor_Name,
    //             Email = existingTutor.Email,
    //             Phone = existingTutor.Phone,
    //             About = existingTutor.About,
    //             DepartmentId = existingTutor.Department_Id,
    //             PositionId = existingTutor.Position_Id,
    //             UserId = existingTutor.UserId
    //         };
    //     }
    // }
    protected override async Task OnParametersSetAsync()
    {
        IsLoading = true;

        // Id တန်ဖိုး အမှန်တကယ် ရောက်ရှိလာမှသာ Data ဆွဲယူပါ
        if (Id > 0)
        {
            UserList = await HttpClientService.ExecuteAsync<List<UserModels>>("user", EnumHttpMethod.Get) ?? new();
            PositionList = await HttpClientService.ExecuteAsync<List<PositionModel>>("position", EnumHttpMethod.Get) ?? new();
            DepartmentList = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get) ?? new();

            // API လမ်းကြောင်းကို "api/tutor/{Id}" ဟု အပြည့်အစုံ ရေးပေးပါ
            var existingTutor = await HttpClientService.ExecuteAsync<TutorModel>($"tutor/{Id}", EnumHttpMethod.Get);

            if (existingTutor != null)
            {
                createModel = new TutorUpdateRequestModel
                {
                    TutorName = existingTutor.TutorName,
                    Email = existingTutor.Email,
                    Phone = existingTutor.Phone,
                    About = existingTutor.About,
                    DepartmentId = existingTutor.Department_Id,
                    PositionId = existingTutor.Position_Id,
                    UserId = existingTutor.UserId // existingTutor.User_Id ဟု သေချာစစ်ပါ
                };
            }
        }

        IsLoading = false;
    }

    private void HandleFileSelected(InputFileChangeEventArgs e) => selectedFile = e.File;

    private async Task UpdateTutor()
    {
        isProcessing = true;
        statusMessage = "Updating...";

        try
        {
            // Edit လုပ်သည့်အခါ ပုံပါ/မပါ စစ်ဆေးပြီး Multipart သုံးခြင်း
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(createModel.TutorName ?? ""), "TutorName");
            content.Add(new StringContent(createModel.Email ?? ""), "Email");
            content.Add(new StringContent(createModel.Phone ?? ""), "Phone");
            content.Add(new StringContent(createModel.About ?? ""), "About");
            content.Add(new StringContent(createModel.DepartmentId.ToString()), "DepartmentId");
            content.Add(new StringContent(createModel.PositionId.ToString()), "PositionId");
            content.Add(new StringContent(createModel.UserId.ToString()), "UserId");

            if (selectedFile != null)
            {
                var fileContent = new StreamContent(selectedFile.OpenReadStream(1024 * 1024 * 5));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(selectedFile.ContentType);
                content.Add(fileContent, "PhotoFile", selectedFile.Name);
            }

            var response = await HttpClientService.ExecuteMultipartAsync<TutorUpdateResponseModel>($"tutor/{Id}", content);

            if (response?.IsSuccess == true)
            {
                Nav.NavigateTo("/admin/tutors");
            }
            else
            {
                statusMessage = response?.Message ?? "ပြင်ဆင်၍မရပါ။";
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
