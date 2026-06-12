using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Text.RegularExpressions; // Regex အတွက်

namespace Smart_Campus_PUMUB.Components.Admin.Pages.User;

public partial class Page_UserCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Navigation { get; set; } = null!;

    private UserCreateRequestModel Request = new();
    private List<RoleModel> RoleList = new();
    private string statusMessage = "";
    private bool IsSuccess = false;
    private bool IsProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        RoleList = await HttpClientService.ExecuteAsync<List<RoleModel>>("role", EnumHttpMethod.Get) ?? new();
    }

    // Username ကို Space ဖြုတ် Underscore တပ်ပြီး Special Character များ ဖယ်ရှားခြင်း
    private void FormatUsername()
    {
        if (string.IsNullOrEmpty(Request.UserName)) return;

        // ၁။ Space အားလုံးကို Underscore ပြောင်းခြင်း
        string formatted = Request.UserName.Replace(" ", "_");

        // ၂။ Underscore နှင့် စာလုံး/ဂဏန်းမှလွဲပြီး ကျန် Special Characters များအားလုံး ဖယ်ရှားခြင်း
        formatted = Regex.Replace(formatted, @"[^a-zA-Z0-9_]", "");

        Request.UserName = formatted;
    }

    private async Task CreateUser()
    {
        // ၁။ Username Validation
        if (string.IsNullOrWhiteSpace(Request.UserName))
        {
            statusMessage = "Username ဖြည့်ရန် လိုအပ်ပါသည်။";
            IsSuccess = false; return;
        }

        // ၂။ Password Validation (အနည်းဆုံး 8 လုံး)
        if (string.IsNullOrEmpty(Request.Password) || Request.Password.Length < 8)
        {
            statusMessage = "Password အနည်းဆုံး 8 လုံးရှိရပါမည်။";
            IsSuccess = false; return;
        }

        // ၃။ Role Validation
        if (Request.RoleId == 0)
        {
            statusMessage = "Role ကို ရွေးချယ်ပေးရန် လိုအပ်ပါသည်။";
            IsSuccess = false; return;
        }

        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<UserCreateResponseModel>("user", EnumHttpMethod.Post, Request);

            if (response != null && response.IsSuccess)
            {
                statusMessage = "User အသစ် ဖန်တီးခြင်း အောင်မြင်ပါသည်။";
                IsSuccess = true;
                await Task.Delay(1500);
                Navigation.NavigateTo("/admin/users");
            }
            else
            {
                statusMessage = response?.Message ?? "User ဖန်တီး၍ မရပါ။ (Username ရှိနှင့်ပြီးသား ဖြစ်နိုင်ပါသည်။)";
                IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("400") || !ex.Message.Contains("Bad Request"))
            {
                statusMessage = "Username ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။ ကျေးဇူးပြု၍ အခြားတစ်ခု ပြောင်းသုံးပါ။";
            }
            else
            {
                statusMessage = "စနစ်တွင် အမှားတစ်ခုခု ဖြစ်ပေါ်နေပါသည်။ နောက်မှ ပြန်ကြိုးစားပါ။";
            }
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}