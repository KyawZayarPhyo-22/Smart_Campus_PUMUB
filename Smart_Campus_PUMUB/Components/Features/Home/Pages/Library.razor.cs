using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Pages
{
    // 🌟 [CONNECTED INTERFACE]: HTML View ဘက်မှ @inherits စနစ်ဖြင့် ကွက်တိ လှမ်းချိတ်နိုင်ရန် ဖန်တီးထားပါသည်
    public class LibraryBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;

        // 💡 ကိုကိုပေးထားသော Web API View Model "BookModel" စံနှုန်းအတိုင်း တိကျစွာ သတ်မှတ်ခြင်း
        public List<BookModel> masterBooks { get; set; } = new();
        public List<BookModel> filteredBooks { get; set; } = new();
        public string searchQuery { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadBooks();
        }

        // 🚀 API ဆီကနေ "books" လမ်းကြောင်းအတိုင်း HttpClient ဖြင့် ဒေတာဆွဲယူခြင်းစနစ်
        public async Task LoadBooks()
        {
            try
            {
                var response = await HttpClientService.ExecuteAsync<List<BookModel>>("book", EnumHttpMethod.Get);
                if (response != null)
                {
                    masterBooks = response;
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Transport Error (Library): {ex.Message}");
                masterBooks = new List<BookModel>();
                ApplyFilter();
            }
        }

        // 🔍 စာအုပ်ခေါင်းစဉ်အလိုက် ချက်ချင်းစစ်ထုတ်ပေးမည့် Dynamic Filter Logic
        public void ApplyFilter()
        {
            if (masterBooks == null) return;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                filteredBooks = masterBooks;
            }
            else
            {
                filteredBooks = masterBooks
                    .Where(b => b.BookName != null && b.BookName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        public void ResetFilter()
        {
            searchQuery = "";
            ApplyFilter();
        }
    }
}