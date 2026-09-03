using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Category;

public partial class Page_CategoryList : IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    private List<CategoryModel> CategoryList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private string ErrorMessage { get; set; } = "";
    private bool IsProcessing { get; set; } = false;

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageCategory = true;

    private string SearchInput = "";

    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        CurrentPage = 1;
        await LoadCategories();
    }

    private async Task ResetFilter()
    {
        SearchInput = "";
        SearchTerm = "";
        CurrentPage = 1;
        await LoadCategories();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private string statusMessage = "";

    public bool IsSuccess { get; private set; }
    private bool ShowModal { get; set; } = false;
    private CategoryModel? SelectedCategory { get; set; }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    private IEnumerable<CategoryModel> FilteredCategories => CategoryList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadCategories();
    }

    private bool canViewCategory = true;
    private bool isAuthLoaded = false;

    protected override async Task OnInitializedAsync()
    {
        LangService.OnLanguageChanged += StateHasChanged;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var roleName = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var roleId = user.FindFirst("RoleId")?.Value ?? "";
            bool isSuperAdmin = string.Equals(roleName, "Super Admin", StringComparison.OrdinalIgnoreCase) || roleId == "4";

            userPermissions = user.Claims
                                  .Where(c => c.Type == "Permission")
                                  .Select(c => c.Value)
                                  .ToList();
                                  
            canViewCategory = isSuperAdmin || userPermissions.Contains("Category.View");
            canManageCategory = isSuperAdmin || userPermissions.Contains("Category.Create") || userPermissions.Contains("Category.Edit") || userPermissions.Contains("Category.Delete");
        }
        else
        {
            canViewCategory = false;
        }

        isAuthLoaded = true;

        if (canViewCategory)
        {
            await LoadCategories();
        }
        else
        {
            IsLoading = false;
        }
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

    private async Task LoadCategories()
    {
        IsLoading = true;
        ErrorMessage = "";
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<CategoryModel>>(
                $"category/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}", 
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;
            if (response != null)
            {
                CategoryList = response.Items;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = LangService.IsMyanmar ? $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}" : $"Failed to load data. Error: {ex.Message}";
        }
        finally 
        { 
            IsLoading = false; 
            StateHasChanged();
        }
    }

    private void OpenDeleteModal(CategoryModel category)
    {
        SelectedCategory = category;
        ShowModal = true;
        statusMessage = "";
        IsSuccess = false;
    }

    private void CloseDeleteModal()
    {
        SelectedCategory = null;
        ShowModal = false;
        statusMessage = "";
        IsSuccess = false;
    }

    private async Task DeleteCategory()
    {
        if (SelectedCategory == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<CategoryDeleteResponseModel>(
                $"category/{SelectedCategory.CategoryId}",
                EnumHttpMethod.Delete
            );

            if (response != null && response.IsSuccess)
            {
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                IsSuccess = true;

                await Task.Delay(800);
                CloseDeleteModal();
                await LoadCategories();
            }
            else
            {
                statusMessage = LangService.IsMyanmar ? "ဤ ကဏ္ဍ ကို အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this category because it is in use.");
                IsSuccess = false;
            }
        }
        catch (Exception)
        {
            statusMessage = LangService.IsMyanmar ? "ဤ ကဏ္ဍ ကို အသုံးပြုနေသောကြောင့် ဖျက်၍ မရပါ။" : "Cannot delete this category.";
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