using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Position;

public partial class Page_PositionCreate
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm]
    private PositionCreateRequestModel positionModel { get; set; } = new PositionCreateRequestModel();

    private string statusMessage = "";
    private bool isProcessing = false;

    // 🚀 POST Method ဖြင့် Position အသစ်အား API သို့ လှမ်းပို့သိမ်းဆည်းခြင်း
    private async Task SavePosition()
    {
        if (string.IsNullOrWhiteSpace(positionModel.PositionName))
        {
            statusMessage = "Position Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
            return;
        }

        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";

        try
        {
            var response = await HttpClientService.ExecuteAsync<PositionCreateResponseModel>(
                "position",
                EnumHttpMethod.Post,
                positionModel
            );

            if (response?.IsSuccess == true)
            {
                statusMessage = response.Message ?? "Position ဖန်တီးမှု အောင်မြင်ပါသည်။";
                NavigationManager.NavigateTo("/admin/positions");
            }
            else
            {
                statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။";
            }
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
            {
                statusMessage = "Position အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
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