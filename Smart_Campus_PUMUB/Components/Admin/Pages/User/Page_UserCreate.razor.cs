using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Diagnostics.Eventing.Reader;
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
        // 1. Username Validation
        if (string.IsNullOrWhiteSpace(Request.UserName))
        {
            statusMessage = "Username ဖြည့်ရန် လိုအပ်ပါသည်။";
            IsSuccess = false;
            return;
        }

        // 2. Password Validation (custom rules)
        if (!IsValidPassword(Request.Password, out string passwordError))
        {
            statusMessage = passwordError;
            IsSuccess = false;
            return;
        }

        // 3. Role Validation
        if (Request.RoleId == 0)
        {
            statusMessage = "Role ကို ရွေးချယ်ပေးရန် လိုအပ်ပါသည်။";
            IsSuccess = false;
            return;
        }

        IsProcessing = true;
        //statusMessage = "User ဖန်တီးနေပါသည်...";

        try
        {
            var response = await HttpClientService.ExecuteAsync<UserCreateResponseModel>(
                "user",
                EnumHttpMethod.Post,
                Request
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = "User အသစ် ဖန်တီးခြင်း အောင်မြင်ပါသည်။";
                IsSuccess = true;

                await Task.Delay(1200);
                Navigation.NavigateTo("/admin/users");
            }
            else
            {
                statusMessage = response?.Message ?? "User ဖန်တီး၍ မရပါ။ Username ရှိပြီးသား ဖြစ်နိုင်ပါသည်။";
                IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            var msg = ex.Message.ToLower();

            if (msg.Contains("400") || msg.Contains("bad request"))
            {
                statusMessage = "ထည့်သွင်းထားသော data မှားယွင်းနေပါသည်။ Username သို့မဟုတ် Password စစ်ဆေးပါ။";
            }
            else if (msg.Contains("timeout"))
            {
                statusMessage = "Server response နှေးနေပါသည်။ နောက်မှ ပြန်ကြိုးစားပါ။";
            }
            else
            {
                statusMessage = "စနစ်အတွင်း အမှားတစ်ခု ဖြစ်ပေါ်နေပါသည်။";
            }

            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private bool IsValidPassword(string password, out string errorMessage)
    {
        errorMessage = "";

        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Password ဖြည့်ရန် လိုအပ်ပါသည်။";
            return false;
        }

        if (password.Length < 8)
        {
            errorMessage = "Password အနည်းဆုံး 8 လုံးရှိရပါမည်။";
            return false;
        }

        if (!password.Any(char.IsUpper))
        {
            errorMessage = "Password တွင် အနည်းဆုံး Capital letter (A-Z) ပါရပါမည်။";
            return false;
        }

        if (!password.Any(ch => "!@#$%^&*()_+-=[]{}|;:',.<>?/".Contains(ch)))
        {
            errorMessage = "Password တွင် အနည်းဆုံး Special character (!@#$%) ပါရပါမည်။";
            return false;
        }

        return true;
    }
}