using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Book;

public partial class Page_BookList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    public List<BookModel> BookList { get; set; } = new();
    public string SearchTerm { get; set; } = "";
    public bool IsLoading { get; set; } = true;
    public bool IsProcessing { get; set; } = false;
    public bool ShowModal { get; set; } = false;
    public BookModel? SelectedBook { get; set; }

    public IEnumerable<BookModel> FilteredBooks => string.IsNullOrWhiteSpace(SearchTerm)
        ? BookList
        : BookList.Where(b => b.BookName != null && b.BookName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        // OnInitializedAsync တွင် JS ကို လုံးဝမခေါ်ပါနှင့်
        await LoadBooks();
    }

    private async Task LoadBooks()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<BookModel>>("book", EnumHttpMethod.Get);
            if (response != null) BookList = response;
        }
        catch (Exception ex)
        {
            // Error ကို log သာထုတ်ပါ (Prerendering အတွက် အန္တရာယ်မရှိပါ)
            Console.WriteLine($"Error loading books: {ex.Message}");
        }
        finally 
        { 
            IsLoading = false; 
            StateHasChanged(); // UI ပြန်ဆန်းရန် အကြောင်းကြားခြင်း
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