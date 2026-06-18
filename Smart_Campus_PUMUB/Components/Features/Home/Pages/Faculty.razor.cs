using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.WebApi.Models;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;

namespace Smart_Campus_PUMUB.Components.Pages
{
    public class DepartmentBase : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = default!;

        public List<FacultyModel> Faculties { get; set; } = new();
        public List<DepartmentModel> Departments { get; set; } = new();
        public List<DepartmentModel> FilteredDepartments { get; set; } = new();

        public int SelectedFacultyId { get; set; } = 0;
        public string searchQuery { get; set; } = "";
        public bool isSearching { get; set; } = false; // Icon ပြောင်းဖို့အတွက်

        protected override async Task OnInitializedAsync()
        {
            Faculties = await HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get) ?? new();
            Departments = await HttpClientService.ExecuteAsync<List<DepartmentModel>>("department", EnumHttpMethod.Get) ?? new();
            FilterData();
        }

        public void OnFacultySelected(int facultyId)
        {
            SelectedFacultyId = facultyId;
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
            StateHasChanged();
        }

        public void ClearSearch()
        {
            searchQuery = "";
            isSearching = false;
            FilterData();
        }
    }
}