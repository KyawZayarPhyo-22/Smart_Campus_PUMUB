using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Pages
{
    // 🌟 [CONNECTED CORE ENGINE]: UI ဘက်မှ @inherits စနစ်ဖြင့် တိုက်ရိုက် လှမ်းချိတ်နိုင်ရန် ဖန်တီးထားပါသည်
    public class ActivityBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;

        // 💡 ကိုကိုပေးထားသော Web API Model ထဲက "ActivityModel" စံနှုန်းအတိုင်း တိကျစွာ ချိတ်ဆက်ခြင်း
        public List<ActivityModel> masterActivities { get; set; } = new();
        public List<ActivityModel> filteredRecentActivities { get; set; } = new();

        public string searchQuery { get; set; } = "";
        public DateTime? selectedFullDate { get; set; }

        public bool isPopupOpen { get; set; } = false;
        public ActivityModel? selectedActivity { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadActivities();
        }

        // 🚀 API ဆီကနေ "activities" (Plural) လမ်းကြောင်းအတိုင်း စနစ်တကျ ဒေတာဆွဲယူခြင်း
        public async Task LoadActivities()
        {
            try
            {
                var response = await HttpClientService.ExecuteAsync<List<ActivityModel>>("activity", EnumHttpMethod.Get);
                if (response != null)
                {
                    // ဒေတာအားလုံးကို View Model စံနှုန်းအတိုင်း သတ်မှတ်ပြီး ရိုးရှင်းစွာ ထည့်သွင်းခြင်း
                    masterActivities = response;
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Transport Error (Activities): {ex.Message}");
                masterActivities = new List<ActivityModel>();
                ApplyFilters();
            }
        }

        // 🔍 စာသားဖြင့် ရှာဖွေမှုများကို စစ်ထုတ်ပေးမည့် ဗဟို Filter စနစ်
        public void ApplyFilters()
        {
            if (masterActivities == null) return;

            var query = masterActivities.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(a => a.ActivityTitle != null &&
                                         a.ActivityTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            filteredRecentActivities = query.ToList();
        }

        public void ResetFilters()
        {
            searchQuery = "";
            selectedFullDate = null;
            ApplyFilters();
        }

        public void OpenPopup(ActivityModel activity)
        {
            selectedActivity = activity;
            isPopupOpen = true;
            StateHasChanged();
        }

        public void ClosePopup()
        {
            isPopupOpen = false;
            selectedActivity = null;
            StateHasChanged();
        }
    }
}