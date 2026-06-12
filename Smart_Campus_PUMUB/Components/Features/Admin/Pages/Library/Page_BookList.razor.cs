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

    public void OpenDeleteModal(BookModel book) { SelectedBook = book; ShowModal = true; }
    public void CloseDeleteModal() { ShowModal = false; }

    public async Task DeleteBook()
    {
        if (SelectedBook == null) return;
        
        IsProcessing = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<BookResponseModel>($"book/{SelectedBook.BookId}", EnumHttpMethod.Delete);
            
            if (response != null && response.IsSuccess)
            {
                // Delete လုပ်ပြီးမှသာ JS alert ကို သုံးပါ (ဘာလို့လဲဆိုတော့ User က button နှိပ်ပြီးမှ ဖြစ်တာမို့)
                await JSRuntime.InvokeVoidAsync("alert", "စာအုပ်ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadBooks();
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