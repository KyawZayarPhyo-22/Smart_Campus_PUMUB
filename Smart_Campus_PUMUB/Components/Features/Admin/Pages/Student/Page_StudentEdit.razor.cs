using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentEdit : ComponentBase
{
    [Parameter] public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    private StudentModel? studentModel;
    private StudentUpdateRequestModel updateModel = new();
    private List<UserModels> UserList = new();
    private bool isProcessing = false;
    private string statusMessage = "";

    // protected override async Task OnParametersSetAsync()
    // {
    //     if (Id > 0)
    //     {
    //         // 1. User စာရင်းနှင့် ကျောင်းသားအချက်အလက် ဆွဲယူခြင်း
    //         UserList = await HttpClientService.ExecuteAsync<List<UserModels>>("user", EnumHttpMethod.Get) ?? new();
    //         studentModel = await HttpClientService.ExecuteAsync<StudentModel>($"student/{Id}", EnumHttpMethod.Get);

    //         if (studentModel != null)
    //         {
    //             updateModel = new StudentUpdateRequestModel
    //             {
    //                 UserName = studentModel.UserName,
    //                 CurrentRollNo = studentModel.CurrentRollNo,
    //                 CurrentClassYear = studentModel.CurrentClassYear,
    //                 CurrentMajor = studentModel.CurrentMajor,
    //                 Status = studentModel.Status
    //             };
    //         }
    //     }
    // }
    protected override async Task OnParametersSetAsync()
    {
        if (Id > 0)
        {
            // 1. User စာရင်းနှင့် ကျောင်းသားအချက်အလက် ဆွဲယူခြင်း
            UserList = await HttpClientService.ExecuteAsync<List<UserModels>>("user", EnumHttpMethod.Get) ?? new();
            studentModel = await HttpClientService.ExecuteAsync<StudentModel>($"student/{Id}", EnumHttpMethod.Get);

            if (studentModel != null)
            {
                updateModel = new StudentUpdateRequestModel
                {
                    // UserId ကိုပါ ထည့်ပေးမှ API က ဘယ်သူလဲဆိုတာ သိမှာပါ
                    UserId = studentModel.UserId,
                    UserName = studentModel.UserName,
                    CurrentRollNo = studentModel.CurrentRollNo,
                    CurrentClassYear = studentModel.CurrentClassYear,
                    CurrentMajor = studentModel.CurrentMajor,
                    Status = studentModel.Status
                };
            }
        }
    }
    private async Task OnUserChanged(string userIdStr)
    {
        if (int.TryParse(userIdStr, out int userId))
        {
            updateModel.UserId = userId;

            // UserList ထဲမှ ရွေးလိုက်သော User ကိုရှာပြီး Update လုပ်ခြင်း
            var selectedUser = UserList.FirstOrDefault(u => u.UserId == userId);
            if (selectedUser != null)
            {
                // သင်၏ StudentUpdateRequestModel တွင် UserName property ရှိရန် လိုအပ်သည်
                // အကယ်၍ မရှိပါက ဤလိုင်းကို ဖျက်လိုက်ပါ
                updateModel.UserName = selectedUser.UserName;
            }
        }
    }

    private async Task UpdateStudent()
    {

        isProcessing = true;
        try
        {
            // Student Edit အတွက် PUT သို့မဟုတ် POST အသုံးပြုပါ
            var response = await HttpClientService.ExecuteAsync<StudentResponseModel>($"student/{Id}", EnumHttpMethod.Put, updateModel);

            if (response?.IsSuccess == true)
            {
                Nav.NavigateTo("/admin/student");
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