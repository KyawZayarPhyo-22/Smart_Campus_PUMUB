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

    // Delete Modal Controls
    private bool ShowModal { get; set; } = false;
    private UserModel? SelectedUser { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<UserModel> GetFilteredUsers() => string.IsNullOrWhiteSpace(SearchTerm)
        ? UserList
        : UserList.Where(u => (u.FullName != null && u.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                              (u.UserName != null && u.UserName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)));

    private IEnumerable<UserModel> FilteredUsers
    {
        get
        {
            var allFiltered = GetFilteredUsers();
            int count = allFiltered.Count();
            int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
            TotalPages = calcPages < 1 ? 1 : calcPages;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    private void OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadUsers();
    }

    // 🚀 GET Method ဖြင့် API မှ User စာရင်းအားလုံး ဆွဲယူခြင်း
    private async Task LoadUsers()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            // API route သည် သင့် Controller သတ်မှတ်ချက်အတိုင်း "user" သို့မဟုတ် "api/user" ဖြစ်ရပါမည်
            var response = await HttpClientService.ExecuteAsync<List<UserModel>>(
                "user",
                EnumHttpMethod.Get
            );

            if (response != null)
            {
                UserList = response;
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