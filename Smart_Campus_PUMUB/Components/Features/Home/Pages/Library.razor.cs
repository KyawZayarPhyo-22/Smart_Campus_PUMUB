using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class LibraryBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<BookModel> masterBooks { get; set; } = new();
        public List<BookModel> filteredBooks { get; set; } = new();
        public List<CategoryModel> Categories { get; set; } = new();

        public int SelectedCategoryId { get; set; } = 0;
        public string searchQuery { get; set; } = "";
        public bool isSearching { get; set; } = false;
        public bool isOpen { get; set; } = false;

        // အရေးကြီး: Page တိုင်းမှာ Data အသစ်ပြန်ခေါ်ရန်
        protected override async Task OnParametersSetAsync()
        {
            await LoadBooks();
        }

        public async Task LoadBooks()
        {
            masterBooks = await HttpClientService.ExecuteAsync<List<BookModel>>("book", EnumHttpMethod.Get) ?? new();
            Categories = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get) ?? new();
            FilterData();
        }

        public void ToggleMenu() => isOpen = !isOpen;

        public void OnCategorySelected(int categoryId)
        {
            SelectedCategoryId = categoryId;
            isOpen = false;
            FilterData();
        }

        public void FilterData()
        {
            var query = masterBooks.AsEnumerable();

            if (SelectedCategoryId != 0)
                query = query.Where(b => b.CategoryId == SelectedCategoryId);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(b => b.BookName != null && 
                                        b.BookName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
                isSearching = true;
            }
            else
            {
                isSearching = false;
            }

            // အသစ်ထည့်ထားတဲ့ စာအုပ်တွေ အရင်ပေါ်စေရန် ID အကြီးဆုံးကနေ အငယ်ကို စီသည်
            filteredBooks = query.OrderByDescending(b => b.BookId).ToList();
            
            StateHasChanged();
        }

        public void ClearSearch()
        {
            searchQuery = "";
            isSearching = false;
            FilterData();
        }
    }
}