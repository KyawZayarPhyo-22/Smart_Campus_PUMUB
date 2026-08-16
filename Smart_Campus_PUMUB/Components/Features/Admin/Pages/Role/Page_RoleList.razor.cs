using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
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

    private string SearchInput = "";

    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        CurrentPage = 1;
        await LoadRoles();
    }

    private async Task ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        CurrentPage = 1;
        await LoadRoles();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }
    public bool IsSuccess { get; private set; }

    private string statusMessage = "";

    // Delete Modal Control လုပ်ရန်
    private bool ShowModal { get; set; } = false;
    private RoleModel? SelectedRole { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<RoleModel> FilteredRoles => RoleList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadRoles();
    }

    // စာမျက်နှာ စတင်ပွင့်လာချိန်တွင် API အား GET ခေါ်ခြင်း
    protected override async Task OnInitializedAsync()
    {
        await LoadRoles();
    }

    private async Task LoadRoles()
    {
        IsLoading = true;
        ErrorMessage = "";
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(1000);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<RoleModel>>(
                $"role/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}", 
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;

            if (response != null)
            {
                RoleList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားအယွင်းရှိပါသည်။ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void OpenDeleteModal(RoleModel role)
    {
        SelectedRole = role;
        ShowModal = true;

        // 💡 ဒီနေရာမှာ ထည့်ပေးပါ - Modal အသစ်ဖွင့်တိုင်း Message ကို Clear လုပ်မယ်
        statusMessage = "";
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedRole = null;
        ShowModal = false;
    }

    //private async Task DeleteRole()
    //{
    //    if (SelectedRole == null) return;

    //    IsProcessing = true;
    //    statusMessage = "ဖျက်သိမ်းနေပါသည်...";

    //    try
    //    {
    //        var response = await HttpClientService.ExecuteAsync<RoleDeleteResponseModel>(
    //            $"role/{SelectedRole.RoleId}",
    //            EnumHttpMethod.Delete
    //        );

    //        if (response?.IsSuccess == true)
    //        {
    //            statusMessage = response.Message ?? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
    //            CloseDeleteModal();
    //            await LoadRoles(); // refresh table
    //        }
    //        else
    //        {
    //            statusMessage = response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။";
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        statusMessage = $"Error: {ex.Message}";
    //    }
    //    finally
    //    {
    //        IsProcessing = false;
    //    }
    //}
    //private void CloseDeleteModal()
    //{
    //    SelectedRole = null;
    //    ShowModal = false;
    //    statusMessage = ""; // Reset message
    //    IsSuccess = false;
    //}
    private async Task DeleteRole()
    {
        if (SelectedRole == null) return;

        IsProcessing = true;
        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<RoleDeleteResponseModel>(
                $"role/{SelectedRole.RoleId}",
                EnumHttpMethod.Delete
            );
          //  var response = await HttpClientService.ExecuteAsync<SemesterDeleteResponseModel>(
          //    $"semester/{SelectedSemester.SemesterId}",
          //    EnumHttpMethod.Delete
          //);
            if (response != null && response.IsSuccess)
            {
                statusMessage = "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
                IsSuccess = true;

                await Task.Delay(1500);
                CloseDeleteModal();
                await LoadRoles();
            }
            else
            {
                // API ကနေ response အမှားပြန်လာရင် ဒီစာသားကို ပြပါ
                statusMessage = "ဤ Role ကို User များက အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";
                IsSuccess = false;
            }
        }
        catch (Exception)
        {
            // Exception ဖြစ်တဲ့အခါမှာလည်း ဒီစာသားပဲ ပြပေးလိုက်ပါ
            statusMessage = "ဤ Role ကို User များက အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။";
            IsSuccess = false;
        }
        finally
        {
            IsProcessing = false;
        }
    }


}