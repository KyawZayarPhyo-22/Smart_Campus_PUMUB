using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Position;

public partial class Page_PositionEdit
{
    [Parameter]
    public int Id { get; set; } // URL လမ်းကြောင်းမှ /edit/{id} ကို ဖမ်းယူခြင်း

    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm]
    private PositionUpdateRequestModel positionModel { get; set; } = new PositionUpdateRequestModel();

    private string statusMessage = "";
    private bool IsLoading = true;
    private bool isProcessing = false;

    // စာမျက်နှာ စတင်ပွင့်လာချိန်တွင် သက်ဆိုင်ရာ Position ဒေတာအား API မှ ဆွဲယူပြသခြင်း
    protected override async Task OnInitializedAsync()
    {
        try
        {
            // API လမ်းကြောင်း ပုံစံ (ဥပမာ: position/5)
            var response = await HttpClientService.ExecuteAsync<PositionModel>($"position/{Id}", EnumHttpMethod.Get);
            if (response != null)
            {
                positionModel.PositionName = response.PositionName;
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"ဒေတာရယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // 🚀 PUT Method ဖြင့် ပြင်ဆင်ချက်များအား API သို့ ပေးပို့ခြင်း
    private async Task UpdatePosition()
    {
        if (string.IsNullOrWhiteSpace(positionModel.PositionName))
        {
            statusMessage = "Position Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
            return;
        }

        isProcessing = true;
        statusMessage = "ပြင်ဆင်ချက်များကို သိမ်းဆည်းနေပါသည်...";

        try
        {
            var response = await HttpClientService.ExecuteAsync<PositionUpdateResponseModel>(
                $"position/{Id}",
                EnumHttpMethod.Put,
                positionModel
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = response.Message ?? "Position ပြင်ဆင်မှု အောင်မြင်ပါသည်။";
                await JSRuntime.InvokeVoidAsync("alert", statusMessage);
                NavigationManager.NavigateTo("/admin/positions");
            }
            else
            {
                statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။";
            }
        }
        catch (Exception ex)
        {
            // ✨ API က BadRequest (400) ပြန်လာပြီး ပြင်လိုက်တဲ့နာမည်က တခြား Position နာမည်နဲ့ သွားတူနေတဲ့ အခြေအနေကို ဖမ်းယူခြင်း
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