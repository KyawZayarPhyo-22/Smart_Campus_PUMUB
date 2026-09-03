using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class ActivityBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;
        [Inject] public IConfiguration Configuration { get; set; } = default!;

        public List<ActivityModel> masterActivities { get; set; } = new();
        public List<ActivityModel> filteredActivities { get; set; } = new();
        public List<ActivityModel> pagedActivities { get; set; } = new();

        public string searchQuery { get; set; } = "";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool isPopupOpen { get; set; } = false;
        public ActivityModel? selectedActivity { get; set; }

        // Lightbox viewer state
        public bool isLightboxOpen { get; set; } = false;
        public int lightboxIndex { get; set; } = 0;

        // Loading state
        public bool isLoading { get; set; } = true;

        // Pagination Variables
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalPages { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            await LoadActivities();
        }

        public async Task LoadActivities()
        {
            isLoading = true;
            StateHasChanged();
            masterActivities = await HttpClientService.ExecuteAsync<List<ActivityModel>>("activity", EnumHttpMethod.Get) ?? new();
            isLoading = false;
            ApplyFilters();
        }

        public void ApplyFilters()
        {
            var query = masterActivities.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim();
                query = query.Where(a => (a.ActivityTitle != null && a.ActivityTitle.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                                         (a.Location != null && a.Location.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            if (FromDate.HasValue)
            {
                query = query.Where(a => {
                    var d = a.ActivityDate ?? (a.CreatedAt.Year > 1 ? a.CreatedAt : (DateTime?)null);
                    return d.HasValue && d.Value.Date >= FromDate.Value.Date;
                });
            }

            if (ToDate.HasValue)
            {
                query = query.Where(a => {
                    var d = a.ActivityDate ?? (a.CreatedAt.Year > 1 ? a.CreatedAt : (DateTime?)null);
                    return d.HasValue && d.Value.Date <= ToDate.Value.Date;
                });
            }

            filteredActivities = query.OrderByDescending(a => a.ActivityDate ?? a.CreatedAt).ToList();
            
            CurrentPage = 1;
            CalculatePagination();
        }

        public void CalculatePagination()
        {
            if (filteredActivities == null || !filteredActivities.Any())
            {
                pagedActivities = new List<ActivityModel>();
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            TotalPages = (int)Math.Ceiling((double)filteredActivities.Count / PageSize);

            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            pagedActivities = filteredActivities
                                .Skip((CurrentPage - 1) * PageSize)
                                .Take(PageSize)
                                .ToList();

            StateHasChanged();
        }

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

        public void OpenPopup(ActivityModel activity)
        {
            selectedActivity = activity;
            isLightboxOpen = false;
            lightboxIndex = 0;
            isPopupOpen = true;
        }

        public void ClosePopup()
        {
            isPopupOpen = false;
            isLightboxOpen = false;
            selectedActivity = null;
            lightboxIndex = 0;
        }

        public void OpenLightbox(int index)
        {
            lightboxIndex = index;
            isLightboxOpen = true;
        }

        public void CloseLightbox()
        {
            isLightboxOpen = false;
        }

        public void NextLightbox()
        {
            if (selectedActivity != null && selectedActivity.ImageList.Any())
            {
                lightboxIndex = (lightboxIndex + 1) % selectedActivity.ImageList.Count;
            }
        }

        public void PrevLightbox()
        {
            if (selectedActivity != null && selectedActivity.ImageList.Any())
            {
                lightboxIndex = (lightboxIndex - 1 + selectedActivity.ImageList.Count) % selectedActivity.ImageList.Count;
            }
        }

        public string GetFullImageUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Equals("string", StringComparison.OrdinalIgnoreCase)) 
                return "https://via.placeholder.com/600x400";
            if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return path;
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return path;

            var baseUrl = Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5077";
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }
    }
}