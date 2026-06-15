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
                "position", // မင်းရဲ့ API Position Route အတိုင်း လိုအပ်သလို ပြောင်းလဲပေးနိုင်ပါတယ်
                EnumHttpMethod.Post,
                positionModel
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = response.Message ?? "Position ဖန်တီးမှု အောင်မြင်ပါသည်။";
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
            // ✨ API က BadRequest (400) ပြန်လာပြီး နာမည်တူရှိနေတဲ့ အခြေအနေကို ဖမ်းယူခြင်း
            if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
            {
                // 💡 မင်းအလိုချင်ဆုံး မြန်မာစာသားကို တိုက်ရိုက်ပြောင်းလဲပေးလိုက်ပါတယ်
                statusMessage = "Position အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
            }
            else
            {
                // အခြား Network သို့မဟုတ် စနစ် Error များအတွက်သာ ပြသပါမည်
                statusMessage = $"Error: {ex.Message}";
            }
        }
        finally
        {
            isProcessing = false;
        }
    }
}