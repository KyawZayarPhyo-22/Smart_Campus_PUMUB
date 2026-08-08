using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Major;

public partial class Page_MajorEdit
{
    [Parameter] public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm] private MajorUpdateRequestModel majorModel { get; set; } = new();
    private List<FacultyModel> FacultyList { get; set; } = new();
    private bool IsLoading = true;
    private bool isProcessing = false;
    private string statusMessage = "";
    private bool isSuccess = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadFaculties();
        await LoadMajor();
    }

    private async Task LoadFaculties()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
            if (response != null)
                FacultyList = response.Where(f => f.FacultyName != null).ToList();
        }
        catch { }
    }

    private async Task LoadMajor()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<MajorModel>($"major/{Id}", EnumHttpMethod.Get);
            if (response != null)
            {
                majorModel.MajorName = response.MajorName;
                majorModel.FacultyId = response.FacultyId;
            }
        }
        catch (Exception ex) { statusMessage = $"Error: {ex.Message}"; isSuccess = false; }
        finally { IsLoading = false; }
    }

    private async Task UpdateMajor()
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
        statusMessage = "ပြင်ဆင်ချက်များကို သိမ်းဆည်းနေပါသည်...";
        isSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<MajorUpdateResponseModel>($"major/{Id}", EnumHttpMethod.Put, majorModel);
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
