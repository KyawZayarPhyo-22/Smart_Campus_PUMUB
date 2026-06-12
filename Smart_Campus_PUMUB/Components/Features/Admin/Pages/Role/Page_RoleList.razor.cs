using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Role;

public partial class Page_RoleList
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    // 🔗 API မှ လာမည့် List အား လက်ခံသိမ်းဆည်းမည့် နေရာ
    private List<RoleModel> RoleList { get; set; } = new();

    // Search Box လုပ်ဆောင်ချက်အတွက်
    private string SearchTerm { get; set; } = "";

    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    // Delete Modal Control လုပ်ရန်
    private bool ShowModal { get; set; } = false;
    private RoleModel? SelectedRole { get; set; }

    // 🔍 Search Textbox ရိုက်သည့်အပေါ်မူတည်ပြီး Dynamic Filter စစ်ပေးခြင်း
    private IEnumerable<RoleModel> FilteredRoles => string.IsNullOrWhiteSpace(SearchTerm)
        ? RoleList
        : RoleList.Where(r =>
                              (r.RoleName != null && r.RoleName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)));

    // စာမျက်နှာ စတင်ပွင့်လာချိန်တွင် API အား GET ခေါ်ခြင်း
    protected override async Task OnInitializedAsync()
    {
        await LoadRoles();
    }

    private async Task LoadRoles()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            // 🚀 API ကနေ List<RoleModel> ကို GET Method ဖြင့် ဆွဲယူခြင်း
            // မင်းရဲ့ API Route အတိုင်း "role" သို့မဟုတ် "api/role" လိုအပ်သလို ညှိပေးပါဦးဗျာ
            var response = await HttpClientService.ExecuteAsync<List<RoleModel>>(
                "role",
                EnumHttpMethod.Get
            );

            if (response != null)
            {
                RoleList = response;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားအယွင်းရှိပါသည်။ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OpenDeleteModal(RoleModel role)
    {
        SelectedRole = role;
        ShowModal = true;
    }

    private void CloseDeleteModal()
    {
        SelectedRole = null;
        ShowModal = false;
    }

    private async Task DeleteRole()
    {
        if (SelectedRole == null) return;

        IsProcessing = true;
        try
        {
            // 🚀 API သို့ DELETE Request ပို့ခြင်း (ဥပမာ: role/5)
            var response = await HttpClientService.ExecuteAsync<RoleDeleteResponseModel>(
                $"role/{SelectedRole.RoleId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadRoles(); // 🔄 Table ထဲမှာ စာရင်းအသစ် ချက်ချင်းဖြစ်သွားအောင် ပြန်ခေါ်ခြင်း
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါတကား။");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}");
        }
        finally
        {
            IsProcessing = false;
        }
    }
}