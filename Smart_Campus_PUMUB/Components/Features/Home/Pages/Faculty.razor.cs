using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class DepartmentBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<FacultyModel> Faculties { get; set; } = new();
        public List<DepartmentModel> Departments { get; set; } = new();
        public List<DepartmentModel> FilteredDepartments { get; set; } = new();
        
        // 🌟 Pagination အတွက် UI က သုံးမယ့် List အသစ်
        public List<DepartmentModel> PagedDepartments { get; set; } = new();

        public int SelectedFacultyId { get; set; } = 0;
        public string searchQuery { get; set; } = "";
        public bool isSearching { get; set; } = false; // Icon ပြောင်းဖို့အတွက်

        // 🌟 Pagination အတွက် လိုအပ်သော Variables များ
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 6; // 💡 တစ်မျက်နှာမှာ ဌာန ၆ ခုစီပြသမည် (စိတ်ကြိုက်ပြင်နိုင်သည်)
        public int TotalPages { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            Faculties = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get) ?? new();
            Departments = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get) ?? new();
            FilterData();
        }

        public void OnFacultySelected(int facultyId)
        {
            SelectedFacultyId = facultyId;
            
            // Faculty Tab ပြောင်းရင် စာမျက်နှာကို ၁ ကနေ ပြန်စမည်
            CurrentPage = 1;
            FilterData();
        }

        public void FilterData()
        {
            var query = Departments.AsEnumerable();

            if (SelectedFacultyId != 0)
                query = query.Where(d => d.FacultyId == SelectedFacultyId);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                query = query.Where(d => d.DepartmentName != null &&
                                        d.DepartmentName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
                isSearching = true; // ရှာလိုက်ပြီဆိုရင် Cross icon ပြမယ်
            }
            else
            {
                isSearching = false;
            }

            FilteredDepartments = query.ToList();
            
            // 🌟 Pagination တွက်ချက်သည့်အပိုင်းကို ချိတ်ဆက်ခေါ်ယူသည်
            CalculatePagination();
        }

        // 🌟 Pagination ဖြတ်ထုတ်ပေးသည့် Logic Method
        public void CalculatePagination()
        {
            if (FilteredDepartments == null || !FilteredDepartments.Any())
            {
                PagedDepartments = new List<DepartmentModel>();
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            // Total Pages ကို တွက်ချက်ခြင်း
            TotalPages = (int)Math.Ceiling((double)FilteredDepartments.Count / PageSize);

            // CurrentPage Scope ကို ထိန်းညှိခြင်း
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            // 💡 လက်ရှိ စာမျက်နှာအတွက်ပဲ ဖြတ်ထုတ်ယူခြင်း
            PagedDepartments = FilteredDepartments
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