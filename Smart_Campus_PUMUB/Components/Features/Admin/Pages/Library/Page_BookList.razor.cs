using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Book;

public partial class Page_BookList : ComponentBase, IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

    public List<BookModel> BookList { get; set; } = new();
    public string SearchTerm { get; set; } = "";
    public bool IsLoading { get; set; } = true;
    public bool IsProcessing { get; set; } = false;
    public bool ShowModal { get; set; } = false;
    public BookModel? SelectedBook { get; set; }

    // Permissions Variables
    private List<string> userPermissions = new();
    private bool canManageBook = true;

    public List<CategoryModel> CategoryList { get; set; } = new();
    public string SearchInput = "";
    public int SelectedCategoryIdInput = 0;
    public int SelectedCategoryId = 0;

    // Custom Dropdown Open States
    private bool isCategoryDropdownOpen = false;

    private void ToggleCategoryDropdown()
    {
        isCategoryDropdownOpen = !isCategoryDropdownOpen;
    }

    private void SelectCategory(int categoryId)
    {
        SelectedCategoryIdInput = categoryId;
        isCategoryDropdownOpen = false;
    }

    private void CloseAllDropdowns()
    {
        isCategoryDropdownOpen = false;
    }

    private string GetSelectedCategoryName()
    {
        if (SelectedCategoryIdInput == 0) return LangService.IsMyanmar ? "အမျိုးအစား အားလုံး" : "All Categories";
        var cat = CategoryList.FirstOrDefault(c => c.CategoryId == SelectedCategoryIdInput);
        return cat?.CategoryName ?? (LangService.IsMyanmar ? "အမျိုးအစား အားလုံး" : "All Categories");
    }

    private async Task ApplyFilter()
    {
        CloseAllDropdowns();
        SearchTerm = SearchInput;
        SelectedCategoryId = SelectedCategoryIdInput;
        CurrentPage = 1;
        await LoadBooks();
    }

    private async Task ResetFilter()
    {
        CloseAllDropdowns();
        SearchInput = "";
        SearchTerm = "";
        SelectedCategoryIdInput = 0;
        SelectedCategoryId = 0;
        CurrentPage = 1;
        await LoadBooks();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;

    public IEnumerable<BookModel> FilteredBooks => BookList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadBooks();
    }

    private bool canViewBook = true;
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
                                  
            canViewBook = isSuperAdmin || userPermissions.Contains("Book.View");
            canManageBook = isSuperAdmin || userPermissions.Contains("Book.Create") || userPermissions.Contains("Book.Edit") || userPermissions.Contains("Book.Delete");
        }
        else
        {
            canViewBook = false;
        }

        isAuthLoaded = true;

        if (canViewBook)
        {
            await LoadBooks();
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

    private async Task LoadBooks()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(500);
            var fetchTask = HttpClientService.ExecuteAsync<PagedResult<BookModel>>(
                $"book/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}&categoryId={SelectedCategoryId}", 
                EnumHttpMethod.Get
            );

            await Task.WhenAll(fetchTask, delayTask);
            var response = await fetchTask;
            if (response != null)
            {
                BookList = response.Items;
                TotalPages = response.TotalPages;
            }

            var catResponse = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get);
            if (catResponse != null) CategoryList = catResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading books: {ex.Message}");
        }
        finally 
        { 
            IsLoading = false; 
            StateHasChanged();
        }
    }

    private string statusMessage = string.Empty;
    private bool IsSuccess = false;

    public void OpenDeleteModal(BookModel book)
    {
        SelectedBook = book;
        ShowModal = true;
        statusMessage = string.Empty;
        IsSuccess = false;
    }

    public void CloseDeleteModal()
    {
        SelectedBook = null;
        ShowModal = false;
        statusMessage = string.Empty;
        IsSuccess = false;
    }

    public async Task DeleteBook()
    {
        if (SelectedBook == null) return;

        IsProcessing = true;
        statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<BookResponseModel>(
                $"book/{SelectedBook.BookId}",
                EnumHttpMethod.Delete);

            if (response?.IsSuccess == true)
            {
                IsSuccess = true;
                statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");

                await LoadBooks();

                await Task.Delay(800);
                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;
                statusMessage = LangService.IsMyanmar ? "ဤ စာအုပ် ကို ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this book.");
            }
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            statusMessage = $"Error: {ex.Message}";
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