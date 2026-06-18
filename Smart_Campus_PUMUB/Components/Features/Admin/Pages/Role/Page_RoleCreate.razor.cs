using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.Components.Admin.Pages.Role;
using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Role;

public partial class Page_RoleCreate
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromForm]
    private RoleCreateRequestModel roleModel { get; set; } = new RoleCreateRequestModel();

    private string statusMessage = "";
    private bool isProcessing = false;



    //private async Task SaveRole()
    //{
    //    if (string.IsNullOrWhiteSpace(roleModel.RoleName))
    //    {
    //        statusMessage = "Role Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
    //        return;
    //    }

    //    isProcessing = true;
    //    statusMessage = "သိမ်းဆည်းနေပါသည်...";

    //    try
    //    {
    //        var response = await HttpClientService.ExecuteAsync<RoleCreateResponseModel>(
    //            "role",
    //            EnumHttpMethod.Post,
    //            roleModel
    //        );

    //        if (response != null && response.IsSuccess)
    //        {
    //            statusMessage = response.Message ?? "Role ဖန်တီးမှု အောင်မြင်ပါသည်။";
    //            await JSRuntime.InvokeVoidAsync("alert", statusMessage);
    //            NavigationManager.NavigateTo("/admin/roles");
    //        }
    //        else
    //        {
    //            statusMessage = response?.Message ?? "တစ်စုံတစ်ခု မှားယွင်းနေပါသည်။";
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        // ✨ API က BadRequest (400) ပြန်လာပြီး နာမည်တူနေတဲ့ အခြေအနေကို အမိအရ ဖမ်းယူခြင်း
    //        if (ex.Message.Contains("BadRequest") || ex.Message.Contains("400"))
    //        {
    //            // 💡 မင်း အလိုအပ်ဆုံး မြန်မာစာသားကို တိုက်ရိုက် သတ်မှတ်ပေးလိုက်ပါပြီ
    //            statusMessage = "Role အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
    //        }
    //        else
    //        {
    //            // အခြား Network ပြတ်တောက်မှု သို့မဟုတ် Server Error များအတွက်သာ စနစ် Error ပြပါမည်
    //            statusMessage = $"Error: {ex.Message}";
    //        }
    //    }
    //    finally
    //    {
    //        isProcessing = false;
    //    }
    //}

    private async Task SaveRole()
    {
        if (string.IsNullOrWhiteSpace(roleModel.RoleName))
        {
            statusMessage = "Role Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
            return;
        }

        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";

        try
        {
            var response = await HttpClientService.ExecuteAsync<RoleCreateResponseModel>(
                "role",
                EnumHttpMethod.Post,
                roleModel
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = response.Message ?? "Role ဖန်တီးမှု အောင်မြင်ပါသည်။";
                NavigationManager.NavigateTo("/admin/roles");
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
                statusMessage = "Role အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။";
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