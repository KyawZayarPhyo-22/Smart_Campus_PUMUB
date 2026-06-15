using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Pages
{
    // 🌟 [FIXED BY KHIN KHIN]: Department လုံးဝမလိုဘဲ ရိုးရှင်းစွာ တည်ဆောက်ထားသော C# Logic ဖြစ်ပါတယ် ကိုကို
    public class Faculty : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;

        public List<FacultyModel> masterFaculties { get; set; } = new();
        public List<FacultyModel> filteredFaculties { get; set; } = new();
        public string searchQuery { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            await LoadFaculties();
        }

        public async Task LoadFaculties()
        {
            try
            {
                var response = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
                if (response != null)
                {
                    masterFaculties = response;
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Transport Error (Faculties): {ex.Message}");
                masterFaculties = new List<FacultyModel>();
                ApplyFilter();
            }
        }

        public void ApplyFilter()
        {
            if (masterFaculties == null) return;

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                filteredFaculties = masterFaculties;
            }
            else
            {
                filteredFaculties = masterFaculties
                    .Where(f => f.FacultyName != null && f.FacultyName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        public void ResetFilter()
        {
            searchQuery = "";
            ApplyFilter();
        }

        public string GetFacultyIcon(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "fas fa-university";
            name = name.ToLower();
            if (name.Contains("civil")) return "fas fa-drafting-compass";
            if (name.Contains("mechanical")) return "fas fa-cog";
            if (name.Contains("electrical")) return "fas fa-bolt";
            if (name.Contains("electronic")) return "fas fa-microchip";
            if (name.Contains("information") || name.Contains("it") || name.Contains("computing")) return "fas fa-laptop-code";
            if (name.Contains("chemical")) return "fas fa-flask";
            return "fas fa-graduation-cap";
        }

        public string GetDefaultFacultyImage(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=600&q=80";
            name = name.ToLower();
            if (name.Contains("civil")) return "https://images.unsplash.com/photo-1504328345606-18bbc8c9d7d1?w=600&q=80";
            if (name.Contains("mechanical")) return "https://images.unsplash.com/photo-1581094794329-c8112a89af12?w=600&q=80";
            if (name.Contains("electrical")) return "https://images.unsplash.com/photo-1473341304170-971dccb5ac1e?w=600&q=80";
            if (name.Contains("electronic")) return "https://images.unsplash.com/photo-1518770660439-4636190af475?w=600&q=80";
            if (name.Contains("information") || name.Contains("it") || name.Contains("computing")) return "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=600&q=80";
            if (name.Contains("chemical")) return "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=600&q=80";
            return "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=600&q=80";
        }
    }
}