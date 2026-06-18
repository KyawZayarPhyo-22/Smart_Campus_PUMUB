using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class RulesBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<RuleModel> masterRules { get; set; } = new();
        public List<RuleModel> filteredRules { get; set; } = new();

        public string searchQuery { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // Popup အတွက် Variable များ
        public bool isPopupOpen { get; set; } = false;
        public RuleModel? selectedRule { get; set; }

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
            StateHasChanged();
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