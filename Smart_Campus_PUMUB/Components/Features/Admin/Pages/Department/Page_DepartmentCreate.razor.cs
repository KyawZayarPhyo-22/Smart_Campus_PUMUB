using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Department;

public partial class Page_DepartmentCreate : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private DepartmentCreateRequestModel departmentModel = new();
    private bool isProcessing = false;
    
    // 💡 Status Message ကို UI မှာ ပြရန်အတွက်
    private string statusMessage = ""; 
    private List<FacultyModel> FacultyList { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadFaculties();
    }

    private async Task LoadFaculties()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            if (response != null)
            {
                FacultyList = response;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
        }
    }

    private async Task HandleSubmit()
    {
        if (departmentModel.FacultyId <= 0)
        {
            statusMessage = "ကျေးဇူးပြု၍ Faculty တစ်ခုကို ရွေးချယ်ပေးပါ။";
            return;
        }

        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်..."; // Loading message

        try
        {
            var response = await HttpClientService.ExecuteAsync<DepartmentResponseModel>(
                "department",
                EnumHttpMethod.Post,
                departmentModel);

            if (response != null && response.IsSuccess)
            {
                //await JSRuntime.InvokeVoidAsync("alert", "Department အသစ်ဖန်တီးခြင်း အောင်မြင်ပါသည်။");
                NavigationManager.NavigateTo("/admin/departments");
            }
            else
            {
                statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။";
            }
        }
        catch (Exception ex)
        {
            // 💡 Duplicate error ကို အမိအရ ဖမ်းယူခြင်း
            if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
            {
                statusMessage = "ပေးထားသော Faculty အောက်တွင် ဤ Department အမည် ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
            }
            else
            {
                statusMessage = $"Error: {ex.Message}";
            }
        }
        finally
        {
            isProcessing = false;
        }
    }
}