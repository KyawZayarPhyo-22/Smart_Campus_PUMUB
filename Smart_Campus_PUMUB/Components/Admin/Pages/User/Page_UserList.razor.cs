using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.User;

public partial class Page_UserList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<UserModel> UserList { get; set; } = new();
    private string SearchTerm { get; set; } = "";

    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    private string SearchInput = "";
    private string SelectedRoleInput = "All";
    private string SelectedRole = "All";

    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        SelectedRole = SelectedRoleInput;
        CurrentPage = 1;
        await LoadUsers();
    }

    private async Task ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        SelectedRoleInput = "All";
        SelectedRole = "All";
        CurrentPage = 1;
        await LoadUsers();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    // Delete Modal Controls
    private bool ShowModal { get; set; } = false;
    private UserModel? SelectedUser { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;
    private int TotalCount { get; set; } = 0;

    private IEnumerable<UserModel> FilteredUsers => UserList;
    private List<RoleModel> RoleList { get; set; } = new();

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadUsers();
    }

    private async Task LoadRoles()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<RoleModel>>("role", EnumHttpMethod.Get);
            if (response != null)
            {
                RoleList = response;
            }
        }
        catch { }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadRoles();
        await LoadUsers();
    }

    // 🚀 GET Method ဖြင့် API မှ User စာရင်းအားလုံး ဆွဲယူခြင်း
    private async Task LoadUsers()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var response = await HttpClientService.ExecuteAsync<PagedResult<UserModel>>(
                $"user/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}&roleName={Uri.EscapeDataString(SelectedRole)}",
                EnumHttpMethod.Get
            );

            if (response != null)
            {
                UserList = response.Items;
                TotalCount = response.TotalCount;
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
        }
    }

    private void OpenDeleteModal(UserModel user)
    {
        SelectedUser = user;
        ShowModal = true;
    }

    private void CloseDeleteModal()
    {
        SelectedUser = null;
        ShowModal = false;
    }

    // 🚀 DELETE Method ဖြင့် User ID အား ဖျက်ချခြင်း
    private async Task DeleteUser()
    {
        if (SelectedUser == null) return;

        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<UserDeleteResponseModel>(
                $"user/{SelectedUser.UserId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                //await JSRuntime.InvokeVoidAsync("alert", response.Message ?? "User အား ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadUsers(); // 🔄 စာရင်းအသစ် ပြန်တင်ခြင်း
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။");
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