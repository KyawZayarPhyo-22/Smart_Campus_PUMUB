// using Microsoft.AspNetCore.Components;
// using Smart_Campus_PUMUB.WebApi.Models;
// using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

// namespace Smart_Campus_PUMUB.Components.Pages
// {
//     public class LibraryBase : ComponentBase
//     {
//         [Inject] public HttpClientService HttpClientService { get; set; } = default!;

//         public List<BookModel> masterBooks { get; set; } = new();
//         public List<BookModel> filteredBooks { get; set; } = new();
//         public List<CategoryModel> Categories { get; set; } = new();

//         public int SelectedCategoryId { get; set; } = 0;
//         public string searchQuery { get; set; } = "";
//         public bool isSearching { get; set; } = false;
//         public bool isOpen { get; set; } = false;

//         // အရေးကြီး: Page တိုင်းမှာ Data အသစ်ပြန်ခေါ်ရန်
//         protected override async Task OnParametersSetAsync()
//         {
//             await LoadBooks();
//         }

//         // public async Task LoadBooks()
//         // {
//         //     masterBooks = await HttpClientService.ExecuteAsync<List<BookModel>>("book", EnumHttpMethod.Get) ?? new();
//         //     Categories = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get) ?? new();
//         //     FilterData();
//         // }
//         public async Task LoadBooks()
//         {
//             masterBooks = await HttpClientService.ExecuteAsync<List<BookModel>>("book", EnumHttpMethod.Get) ?? new();

//             // Debug လုပ်ကြည့်ရန် Console တွင် ကြည့်ပါ
//             foreach (var b in masterBooks)
//             {
//                 Console.WriteLine($"Book: {b.BookName}, CategoryID: {b.CategoryId}, CatName: {b.CategoryName}");
//             }

//             Categories = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get) ?? new();
//             FilterData();
//         }

//         public void ToggleMenu() => isOpen = !isOpen;

//         public void OnCategorySelected(int categoryId)
//         {
//             SelectedCategoryId = categoryId;
//             isOpen = false;
//             FilterData();
//         }

//         // public void FilterData()
//         // {
//         //     var query = masterBooks.AsEnumerable();

//         //     if (SelectedCategoryId != 0)
//         //         query = query.Where(b => b.CategoryId == SelectedCategoryId);

//         //     if (!string.IsNullOrWhiteSpace(searchQuery))
//         //     {
//         //         query = query.Where(b => b.BookName != null &&
//         //                                 b.BookName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
//         //         isSearching = true;
//         //     }
//         //     else
//         //     {
//         //         isSearching = false;
//         //     }

//         //     // အသစ်ထည့်ထားတဲ့ စာအုပ်တွေ အရင်ပေါ်စေရန် ID အကြီးဆုံးကနေ အငယ်ကို စီသည်
//         //     filteredBooks = query.OrderByDescending(b => b.BookId).ToList();

//         //     StateHasChanged();
//         // }
//         public void FilterData()
//         {
//             var query = masterBooks.AsEnumerable();

//             // ID နဲ့ Filter လုပ်တာ မှန်ကန်ပါတယ်
//             if (SelectedCategoryId != 0)
//             {
//                 query = query.Where(b => b.CategoryId == SelectedCategoryId);
//             }

//             // Search Query
//             if (!string.IsNullOrWhiteSpace(searchQuery))
//             {
//                 query = query.Where(b => b.BookName != null &&
//                                         b.BookName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
//             }

//             filteredBooks = query.OrderByDescending(b => b.BookId).ToList();
//             StateHasChanged();
//         }

//         public void ClearSearch()
//         {
//             searchQuery = "";
//             isSearching = false;
//             FilterData();
//         }
//     }
// }
using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class LibraryBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<BookModel> masterBooks { get; set; } = new();
        public List<BookModel> filteredBooks { get; set; } = new();
        public List<CategoryModel> Categories { get; set; } = new();
        
        // 🌟 Pagination အတွက် UI က သုံးမယ့် List အသစ်
        public List<BookModel> pagedBooks { get; set; } = new();

        public int SelectedCategoryId { get; set; } = 0;
        public string searchQuery { get; set; } = "";
        public bool isSearching { get; set; } = false;
        public bool isOpen { get; set; } = false;

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10; // 💡 တစ်မျက်နှာမှာ စာအုပ် ၁၀ အုပ်စီပဲ ပြသမည်
        public int TotalPages { get; set; } = 1;


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
            
            // Category ပြောင်းရင် စာမျက်နှာကို ၁ ကနေ ပြန်စမည်
            CurrentPage = 1;
            FilterData();
        }

        public void FilterData()
        {
            var query = masterBooks.AsEnumerable();

            // ID နဲ့ Filter လုပ်တာ
            if (SelectedCategoryId != 0)
            {
                query = query.Where(b => b.CategoryId == SelectedCategoryId);
            }

            // Search Query
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

            // အသစ်ထည့်ထားတဲ့ စာအုပ်တွေ အရင်ပေါ်စေရန် စီသည်
            filteredBooks = query.OrderByDescending(b => b.BookId).ToList();
            
            // 🌟 Pagination တွက်ချက်သည့်အပိုင်းကို ချိတ်ဆက်ခေါ်ယူသည်
            CalculatePagination();
        }

        // 🌟 Pagination ဖြတ်ထုတ်ပေးသည့် Logic Method
        public void CalculatePagination()
        {
            if (filteredBooks == null || !filteredBooks.Any())
            {
                pagedBooks = new List<BookModel>();
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            // Total Pages ကို တွက်ချက်ခြင်း
            TotalPages = (int)Math.Ceiling((double)filteredBooks.Count / PageSize);

            // CurrentPage Scope ကို ထိန်းညှိခြင်း
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            // 💡 လက်ရှိ စာမျက်နှာအတွက်ပဲ ဖြတ်ထုတ်ယူခြင်း
            pagedBooks = filteredBooks
                            .Skip((CurrentPage - 1) * PageSize)
                            .Take(PageSize)
                            .ToList();

            StateHasChanged();
        }

        // 🌟 Shared Pagination Component က ခလုတ်နှိပ်ရင် ဒီ Method ကို လှမ်းခေါ်ပါမည်
        public void OnPageChanged(int newPage)
        {
            CurrentPage = newPage;
            CalculatePagination();
        }

        public void ClearSearch()
        {
            searchQuery = "";
            isSearching = false;
            CurrentPage = 1;
            FilterData();
        }
    }
}