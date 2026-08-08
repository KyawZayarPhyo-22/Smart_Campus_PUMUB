using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Major;

public partial class Page_MajorCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm] private MajorCreateRequestModel majorModel { get; set; } = new();
    private List<FacultyModel> FacultyList { get; set; } = new();
    private bool IsLoadingFaculties = true;
    private string statusMessage = "";
    private bool isProcessing = false;
    private bool isSuccess = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadFaculties();
    }

    private async Task LoadFaculties()
    {
        IsLoadingFaculties = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            if (response != null)
                FacultyList = response.Where(f => f.FacultyName != null).ToList();
        }
        catch { }
        finally { IsLoadingFaculties = false; }
    }

    private async Task SaveMajor()
    {
        if (string.IsNullOrWhiteSpace(majorModel.MajorName))
        {
            statusMessage = "Major Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
            isSuccess = false;
            return;
        }
        if (majorModel.FacultyId <= 0)
        {
            statusMessage = "Faculty ရွေးချယ်ရန် လိုအပ်ပါသည်။";
            isSuccess = false;
            return;
        }

        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";
        isSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<MajorCreateResponseModel>("major", EnumHttpMethod.Post, majorModel);
            if (response != null && response.IsSuccess)
            {
                NavigationManager.NavigateTo("/admin/majors");
            }
            else
            {
                statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။";
                isSuccess = false;
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
                statusMessage = "Major အမည်မှာ ဤ Faculty အောက်တွင် ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
            else
                statusMessage = $"Error: {ex.Message}";
            isSuccess = false;
        }
        finally { isProcessing = false; }
    }
}
