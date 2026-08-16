using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Book;

public partial class Page_BookList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

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
        // Search button နှိပ်မှသာ Filter ဖြစ်မည်
    }

    private void CloseAllDropdowns()
    {
        isCategoryDropdownOpen = false;
    }

    private string GetSelectedCategoryName()
    {
        if (SelectedCategoryIdInput == 0) return "All Categories";
        var cat = CategoryList.FirstOrDefault(c => c.CategoryId == SelectedCategoryIdInput);
        return cat?.CategoryName ?? "All Categories";
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

    protected override async Task OnInitializedAsync()
    {
        // OnInitializedAsync တွင် JS ကို လုံးဝမခေါ်ပါနှင့်
        await LoadBooks();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            userPermissions = user.Claims
                                  .Where(c => c.Type == "Permission")
                                  .Select(c => c.Value)
                                  .ToList();
                                  
            canManageBook = userPermissions.Contains("Book.Edit") || userPermissions.Contains("Book.Delete");

            StateHasChanged();
        }
    }

    private async Task LoadBooks()
    {
        IsLoading = true;
        StateHasChanged();
        try
        {
            var delayTask = Task.Delay(1000);
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

        statusMessage = "ဖျက်သိမ်းနေပါသည်...";
        IsSuccess = false;

        try
        {
            var response = await HttpClientService.ExecuteAsync<BookResponseModel>(
                $"book/{SelectedBook.BookId}",
                EnumHttpMethod.Delete);

            if (response?.IsSuccess == true)
            {
                IsSuccess = true;
                statusMessage = response.Message ?? "စာအုပ်ကို အောင်မြင်စွာ ဖျက်ပြီးပါပြီ။";

                await LoadBooks();

                await Task.Delay(800);
                CloseDeleteModal();
            }
            else
            {
                IsSuccess = false;
                statusMessage = response?.Message ?? "စာအုပ်ကို ဖျက်၍ မရပါ။";
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
}