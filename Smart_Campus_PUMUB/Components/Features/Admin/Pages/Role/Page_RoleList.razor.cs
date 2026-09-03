using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Role;

public partial class Page_RoleList : IDisposable
{
    [Inject]
    public HttpClientService HttpClientService { get; set; } = null!;

    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;

    [Inject] 
    public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

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
        LangService.OnLanguageChanged += StateHasChanged;
        await LoadRoles();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            var savedLang = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_dashboard_lang");
            if (!string.IsNullOrEmpty(savedLang))
            {
                LangService.SetLanguage(savedLang);
            }
        }
        catch { }
    }

    private async Task LoadRoles()
    {
        IsLoading = true;
        ErrorMessage = "";
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
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
            ErrorMessage = LangService.IsMyanmar ? $"ဒေတာဆွဲယူရာတွင် အမှားအယွင်းရှိပါသည်။ Error: {ex.Message}" : $"Failed to load data. Error: {ex.Message}";
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

        statusMessage = "";
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedRole = null;
        ShowModal = false;
        statusMessage = "";
        IsSuccess = false;
    }

    private async Task DeleteRole()
    {
        if (SelectedRole == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<RoleDeleteResponseModel>(
                $"role/{SelectedRole.RoleId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                IsSuccess = true;

                await Task.Delay(800);
                CloseDeleteModal();
                await LoadRoles();
            }
            else
            {
                statusMessage = LangService.IsMyanmar ? "ဤ Role ကို User များက အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this role because it is in use.");
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

    public void Dispose()
    {
        LangService.OnLanguageChanged -= StateHasChanged;
    }
}