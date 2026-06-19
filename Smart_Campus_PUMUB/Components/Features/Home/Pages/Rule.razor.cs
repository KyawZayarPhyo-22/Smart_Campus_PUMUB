using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class RulesBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<RuleModel> masterRules { get; set; } = new();
        public List<RuleModel> filteredRules { get; set; } = new();
        
        // 🌟 Pagination အတွက် UI က သုံးမယ့် List အသစ်
        public List<RuleModel> pagedRules { get; set; } = new();

        public string searchQuery { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Popup အတွက် Variable များ
        public bool isPopupOpen { get; set; } = false;
        public RuleModel? selectedRule { get; set; }

        // 🌟 Pagination အတွက် လိုအပ်သော Variables များ
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 2; // 💡 တစ်မျက်နှာမှာ Rule ၆ ခုစီပြသမည် (စိတ်ကြိုက်ပြင်နိုင်သည်)
        public int TotalPages { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            await LoadRules();
        }

        public async Task LoadRules()
        {
            masterRules = await HttpClientService.ExecuteAsync<List<RuleModel>>("rules", EnumHttpMethod.Get) ?? new();
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            var query = masterRules.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(r => (r.Title != null && r.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                         (r.Description != null && r.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));
            }

            if (FromDate.HasValue) query = query.Where(r => r.CreatedDateTime.Date >= FromDate.Value.Date);
            if (ToDate.HasValue) query = query.Where(r => r.CreatedDateTime.Date <= ToDate.Value.Date);

            filteredRules = query.OrderByDescending(r => r.CreatedDateTime).ToList();
            
            // 🌟 Filter လုပ်ပြီးတိုင်း Page 1 ကို ပြန်သွားပြီး Pagination တွက်ချက်သည်
            CurrentPage = 1;
            CalculatePagination();
        }

        // 🌟 Pagination ဖြတ်ထုတ်ပေးသည့် Logic Method
        public void CalculatePagination()
        {
            if (filteredRules == null || !filteredRules.Any())
            {
                pagedRules = new List<RuleModel>();
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            // Total Pages ကို တွက်ချက်ခြင်း
            TotalPages = (int)Math.Ceiling((double)filteredRules.Count / PageSize);

            // CurrentPage Scope ကို ထိန်းညှိခြင်း
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            // 💡 လက်ရှိ စာမျက်နှာအတွက်ပဲ ဖြတ်ထုတ်ယူခြင်း
            pagedRules = filteredRules
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

        public void ResetFilters()
        {
            searchQuery = "";
            FromDate = null;
            ToDate = null;
            ApplyFilters();
        }

        // Popup Method များ
        public void OpenPopup(RuleModel rule)
        {
            selectedRule = rule;
            isPopupOpen = true;
        }

        public void ClosePopup()
        {
            isPopupOpen = false;
            selectedRule = null;
        }
    }
}