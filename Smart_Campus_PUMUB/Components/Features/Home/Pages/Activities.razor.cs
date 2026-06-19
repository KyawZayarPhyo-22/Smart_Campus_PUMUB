// using Microsoft.AspNetCore.Components;
// using Smart_Campus_PUMUB.WebApi.Models;
// using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

// namespace Smart_Campus_PUMUB.Components.Pages
// {
//     public class ActivityBase : ComponentBase
//     {
//         [Inject] public HttpClientService HttpClientService { get; set; } = default!;

//         public List<ActivityModel> masterActivities { get; set; } = new();
//         public List<ActivityModel> filteredActivities { get; set; } = new();

//         public string searchQuery { get; set; } = "";
//         public DateTime? FromDate { get; set; }
//         public DateTime? ToDate { get; set; }

//         public bool isPopupOpen { get; set; } = false;
//         public ActivityModel? selectedActivity { get; set; }

//         protected override async Task OnInitializedAsync()
//         {
//             await LoadActivities();
//         }

//         public async Task LoadActivities()
//         {
//             masterActivities = await HttpClientService.ExecuteAsync<List<ActivityModel>>("activity", EnumHttpMethod.Get) ?? new();
//             ApplyFilters();
//         }

//         // public void ApplyFilters()
//         // {
//         //     var query = masterActivities.AsEnumerable();

//         //     if (!string.IsNullOrWhiteSpace(searchQuery))
//         //         query = query.Where(a => a.ActivityTitle != null && a.ActivityTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

//         //     if (FromDate.HasValue)
//         //         query = query.Where(a => a.CreatedAt.Date >= FromDate.Value.Date);

//         //     if (ToDate.HasValue)
//         //         query = query.Where(a => a.CreatedAt.Date <= ToDate.Value.Date);

//         //     filteredActivities = query.OrderByDescending(a => a.CreatedAt).ToList();
//         //     StateHasChanged();
//         // }
//         public void ApplyFilters()
//         {
//             var query = masterActivities.AsEnumerable();

//             // Title နဲ့ ရှာမယ်
//             if (!string.IsNullOrWhiteSpace(searchQuery))
//                 query = query.Where(a => a.ActivityTitle != null &&
//                                          a.ActivityTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

//             // Date နဲ့ ရှာမယ် (FromDate နှင့် ToDate ကို ကိုကိုရွေးထားတဲ့ အချိန်နဲ့ စစ်တာပါ)
//             if (FromDate.HasValue)
//                 query = query.Where(a => a.CreatedAt.Date >= FromDate.Value.Date);

//             if (ToDate.HasValue)
//                 query = query.Where(a => a.CreatedAt.Date <= ToDate.Value.Date);

//             filteredActivities = query.OrderByDescending(a => a.CreatedAt).ToList();
//             StateHasChanged();
//         }

//         public void OpenPopup(ActivityModel activity)
//         {
//             selectedActivity = activity;
//             isPopupOpen = true;
//         }
//         public void ResetFilters()
//         {
//             // Search နှင့် Date တန်ဖိုးများကို အလွတ်ဖြစ်အောင် ပြန်လုပ်သည်
//             searchQuery = "";
//             FromDate = null;
//             ToDate = null;

//             // Data အားလုံးကို ပြန်ခေါ်ပြီး ပြသသည်
//             ApplyFilters();
//         }

//         public void ClosePopup()
//         {
//             isPopupOpen = false;
//             selectedActivity = null;
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
    public class ActivityBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<ActivityModel> masterActivities { get; set; } = new();
        public List<ActivityModel> filteredActivities { get; set; } = new();
        
        // 🌟 Pagination အတွက် UI က သုံးမယ့် List အသစ်
        public List<ActivityModel> pagedActivities { get; set; } = new();

        public string searchQuery { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool isPopupOpen { get; set; } = false;
        public ActivityModel? selectedActivity { get; set; }

        // 🌟 Pagination အတွက် လိုအပ်သော Variables များ
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 2; // 💡 တစ်မျက်နှာမှာ ၆ ခုပဲ ပြသမည် (စိတ်ကြိုက်ပြင်နိုင်သည်)
        public int TotalPages { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            await LoadActivities();
        }

        public async Task LoadActivities()
        {
            masterActivities = await HttpClientService.ExecuteAsync<List<ActivityModel>>("activity", EnumHttpMethod.Get) ?? new();
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            var query = masterActivities.AsEnumerable();

            // Title နဲ့ ရှာမယ်
            if (!string.IsNullOrWhiteSpace(searchQuery))
                query = query.Where(a => a.ActivityTitle != null &&
                                         a.ActivityTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

            // Date နဲ့ ရှာမယ်
            if (FromDate.HasValue)
                query = query.Where(a => a.CreatedAt.Date >= FromDate.Value.Date);

            if (ToDate.HasValue)
                query = query.Where(a => a.CreatedAt.Date <= ToDate.Value.Date);

            // မူလ Filter လုပ်ထားတဲ့ List ထဲ ထည့်သည်
            filteredActivities = query.OrderByDescending(a => a.CreatedAt).ToList();
            
            // 🌟 Filter လုပ်ပြီးတိုင်း Page 1 ကို ပြန်သွားပြီး Pagination တွက်ချက်သည်
            CurrentPage = 1;
            CalculatePagination();
        }

        // 🌟 Pagination ဖြတ်ထုတ်ပေးသည့် Logic Method
        public void CalculatePagination()
        {
            if (filteredActivities == null || !filteredActivities.Any())
            {
                pagedActivities = new List<ActivityModel>();
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            // Total Pages ကို တွက်ချက်ခြင်း
            TotalPages = (int)Math.Ceiling((double)filteredActivities.Count / PageSize);

            // CurrentPage Scope ကို ထိန်းညှိခြင်း
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            // 💡 လက်ရှိ စာမျက်နှာအတွက် Data ကို ဖြတ်ထုတ်ယူခြင်း
            pagedActivities = filteredActivities
                                .Skip((CurrentPage - 1) * PageSize)
                                .Take(PageSize)
                                .ToList();

            StateHasChanged();
        }

        // 🌟 Shared Pagination Component က ခလုတ်နှိပ်ရင် ဒီ Method ကို လှမ်းခေါ်ပါမည်
        public void OnPageChanged(int newPage)
        {
            CurrentPage = newPage;
            CalculatePagination(); // Page ရွှေ့ရင် Data အသစ် ထပ်မံဖြတ်ထုတ်သည်
        }

        public void ResetFilters()
        {
            searchQuery = "";
            FromDate = null;
            ToDate = null;
            ApplyFilters();
        }

        public void OpenPopup(ActivityModel activity)
        {
            selectedActivity = activity;
            isPopupOpen = true;
        }

        public void ClosePopup()
        {
            isPopupOpen = false;
            selectedActivity = null;
        }
    }
}