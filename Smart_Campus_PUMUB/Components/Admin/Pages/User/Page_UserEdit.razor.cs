using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.User;

public partial class Page_UserEdit
{
    [Parameter] public int UserId { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Navigation { get; set; } = null!;

    private UserUpdateRequestModel Request = new(); // Edit အတွက် သီးသန့် Model သုံးပါ
    private List<RoleModel> RoleList = new();
    private string statusMessage = "";
    private bool IsSuccess = false;
    private bool IsProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        // 1. Role စာရင်းဆွဲယူခြင်း
        RoleList = await HttpClientService.ExecuteAsync<List<RoleModel>>("role", EnumHttpMethod.Get) ?? new();

        // 2. လက်ရှိ User Data ကို ဆွဲယူခြင်း
        var user = await HttpClientService.ExecuteAsync<UserEditResponseModel>($"user/{UserId}", EnumHttpMethod.Get);
        if (user != null)
        {
            Request = new UserUpdateRequestModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                UserName = user.UserName,
                RoleId = user.RoleId,
                RoleNo = user.RoleNo
            };
        }
    }

    private async Task UpdateUser()
    {
        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<UserCreateResponseModel>($"user/{UserId}", EnumHttpMethod.Put, Request);

            if (response != null && response.IsSuccess)
            {
                statusMessage = "User အချက်အလက် ပြင်ဆင်ခြင်း အောင်မြင်ပါသည်။";
                IsSuccess = true;
                await Task.Delay(200);
                Navigation.NavigateTo("/admin/users");
            }
            else
            {
                statusMessage = response?.Message ?? "ပြင်ဆင်၍ မရပါ။";
                IsSuccess = false;
            }
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("400") || !ex.Message.Contains("Bad Request"))
            {
                statusMessage = "Password အနည်းဆုံး 8 လုံးရှိရပါမည်။";
            }
         
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}