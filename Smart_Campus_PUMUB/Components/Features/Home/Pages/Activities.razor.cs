using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class ActivityBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<ActivityModel> masterActivities { get; set; } = new();
        public List<ActivityModel> filteredActivities { get; set; } = new();

        public string searchQuery { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool isPopupOpen { get; set; } = false;
        public ActivityModel? selectedActivity { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadActivities();
        }

        public async Task LoadActivities()
        {
            masterActivities = await HttpClientService.ExecuteAsync<List<ActivityModel>>("activity", EnumHttpMethod.Get) ?? new();
            ApplyFilters();
        }

        // public void ApplyFilters()
        // {
        //     var query = masterActivities.AsEnumerable();

        //     if (!string.IsNullOrWhiteSpace(searchQuery))
        //         query = query.Where(a => a.ActivityTitle != null && a.ActivityTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

        //     if (FromDate.HasValue)
        //         query = query.Where(a => a.CreatedAt.Date >= FromDate.Value.Date);

        //     if (ToDate.HasValue)
        //         query = query.Where(a => a.CreatedAt.Date <= ToDate.Value.Date);

        //     filteredActivities = query.OrderByDescending(a => a.CreatedAt).ToList();
        //     StateHasChanged();
        // }
        public void ApplyFilters()
        {
            var query = masterActivities.AsEnumerable();

            // Title နဲ့ ရှာမယ်
            if (!string.IsNullOrWhiteSpace(searchQuery))
                query = query.Where(a => a.ActivityTitle != null &&
                                         a.ActivityTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

            // Date နဲ့ ရှာမယ် (FromDate နှင့် ToDate ကို ကိုကိုရွေးထားတဲ့ အချိန်နဲ့ စစ်တာပါ)
            if (FromDate.HasValue)
                query = query.Where(a => a.CreatedAt.Date >= FromDate.Value.Date);

            if (ToDate.HasValue)
                query = query.Where(a => a.CreatedAt.Date <= ToDate.Value.Date);

            filteredActivities = query.OrderByDescending(a => a.CreatedAt).ToList();
            StateHasChanged();
        }

        public void OpenPopup(ActivityModel activity)
        {
            selectedActivity = activity;
            isPopupOpen = true;
        }
        public void ResetFilters()
        {
            // Search နှင့် Date တန်ဖိုးများကို အလွတ်ဖြစ်အောင် ပြန်လုပ်သည်
            searchQuery = "";
            FromDate = null;
            ToDate = null;

            // Data အားလုံးကို ပြန်ခေါ်ပြီး ပြသသည်
            ApplyFilters();
        }

        public void ClosePopup()
        {
            isPopupOpen = false;
            selectedActivity = null;
        }
    }
}