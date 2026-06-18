using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Department;

public partial class Page_DepartmentEdit : ComponentBase
{
    [Parameter] public int Id { get; set; }

    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private DepartmentUpdateRequestModel departmentModel = new();
    private List<FacultyModel> FacultyList { get; set; } = new();
    private bool isProcessing = false;
    private string statusMessage = "";

    protected override async Task OnInitializedAsync()
    {
        await LoadFaculties();
        await LoadDepartment();
    }

    private async Task LoadFaculties()
    {
        FacultyList = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get) ?? new();
    }

    private async Task LoadDepartment()
    {
        var response = await HttpClientService.ExecuteAsync<DepartmentModel>($"department/{Id}", EnumHttpMethod.Get);
        if (response != null)
        {
            departmentModel = new DepartmentUpdateRequestModel 
            { 
                FacultyId = response.FacultyId,
                DepartmentName = response.DepartmentName 
            };
        }
    }

    private async Task HandleUpdate()
    {
        isProcessing = true;
        statusMessage = "ပြင်ဆင်နေပါသည်...";

        try
        {
            var response = await HttpClientService.ExecuteAsync<DepartmentResponseModel>(
                $"department/{Id}", 
                EnumHttpMethod.Put, 
                departmentModel);

            if (response != null && response.IsSuccess)
            {
                //await JSRuntime.InvokeVoidAsync("alert", "ပြင်ဆင်ခြင်း အောင်မြင်ပါသည်။");
                NavigationManager.NavigateTo("/admin/departments");
            }
            else
            {
                statusMessage = response?.Message ?? "ပြင်ဆင်၍မရပါ။";
            }
        }
        catch (Exception ex)
        {
            statusMessage = ex.Message.Contains("400") ? "ဤအမည်ဖြင့် အခြားဌာနရှိနှင့်ပြီးဖြစ်ပါသည်။" : $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }
}