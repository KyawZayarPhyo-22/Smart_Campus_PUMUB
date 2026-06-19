using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student
{
    public partial class Page_StudentDirectory : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;

        private List<StudentModel> StudentList { get; set; } = new();

        // Filters
        private string SearchInput { get; set; } = "";
        private string SelectedMajorInput { get; set; } = "All";
        private string SelectedYearInput { get; set; } = "All";

        private string SearchTerm { get; set; } = "";
        private string SelectedMajor { get; set; } = "All";
        private string SelectedYear { get; set; } = "All";

        private bool IsLoading { get; set; } = true;

        // Toast properties
        private bool ShowToast { get; set; } = false;
        private string ToastMessage { get; set; } = "";

        // Pagination
        private int CurrentPage { get; set; } = 1;
        private int PageSize { get; set; } = 10;
        private int TotalPages { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            await LoadStudents();
        }

        private async Task LoadStudents()
        {
            IsLoading = true;
            try
            {
                var response = await HttpClientService.ExecuteAsync<List<StudentModel>>("Student", EnumHttpMethod.Get);
                if (response != null)
                {
                    StudentList = response;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading student directory: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            SearchTerm = SearchInput;
            SelectedMajor = SelectedMajorInput;
            SelectedYear = SelectedYearInput;
            CurrentPage = 1;
            StateHasChanged();
        }

        private void ResetFilter()
        {
            SearchInput = "";
            SelectedMajorInput = "All";
            SelectedYearInput = "All";

            SearchTerm = "";
            SelectedMajor = "All";
            SelectedYear = "All";
            CurrentPage = 1;
            StateHasChanged();
        }

        private void HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                ApplyFilter();
            }
        }

        private void OnPageChanged(int newPage)
        {
            CurrentPage = newPage;
            StateHasChanged();
        }

        private IEnumerable<StudentModel> GetFilteredStudents()
        {
            var data = StudentList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                data = data.Where(s => (s.CurrentRollNo ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       (s.FullName ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedMajor != "All")
            {
                data = data.Where(s => string.Equals(s.CurrentMajor, SelectedMajor, StringComparison.OrdinalIgnoreCase));
            }

            if (SelectedYear != "All")
            {
                data = data.Where(s => string.Equals(s.CurrentClassYear, SelectedYear, StringComparison.OrdinalIgnoreCase));
            }

            return data.ToList();
        }

        private IEnumerable<StudentModel> FilteredStudents
        {
            get
            {
                var allFiltered = GetFilteredStudents();

                int count = allFiltered.Count();
                int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
                TotalPages = calcPages < 1 ? 1 : calcPages;

                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }

                return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            }
        }

        private string GetDropdownClass(string? result) => result switch
        {
            "Pass" => "pass",
            "Fail" => "fail",
            _ => "none"
        };

        private async Task UpdateStatus(StudentModel student, string? newStatus)
        {
            if (string.IsNullOrEmpty(newStatus)) return;

            try
            {
                var patchModel = new StudentPatchRequestModel { Status = newStatus };
                var response = await HttpClientService.ExecuteAsync<StudentResponseModel>($"Student/{student.StudentId}", EnumHttpMethod.Patch, patchModel);

                if (response?.IsSuccess == true)
                {
                    student.Status = newStatus;
                    TriggerToast($"Successfully changed {student.FullName}'s status to {newStatus}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating status: {ex.Message}");
            }
        }

        private async Task UpdateResult(StudentModel student, int semesterNum, string? newResult)
        {
            try
            {
                var patchModel = new StudentPatchRequestModel();
                string displayResult = string.IsNullOrEmpty(newResult) || newResult == "None" ? "Cleared" : newResult;

                switch (semesterNum)
                {
                    case 1: patchModel.Sem1_Result = newResult; break;
                    case 2: patchModel.Sem2_Result = newResult; break;
                    case 3: patchModel.Sem3_Result = newResult; break;
                    case 4: patchModel.Sem4_Result = newResult; break;
                    case 5: patchModel.Sem5_Result = newResult; break;
                    case 6: patchModel.Sem6_Result = newResult; break;
                    case 7: patchModel.Sem7_Result = newResult; break;
                    case 8: patchModel.Sem8_Result = newResult; break;
                    case 9: patchModel.Sem9_Result = newResult; break;
                }

                var response = await HttpClientService.ExecuteAsync<StudentResponseModel>($"Student/{student.StudentId}", EnumHttpMethod.Patch, patchModel);

                if (response?.IsSuccess == true)
                {
                    switch (semesterNum)
                    {
                        case 1: student.Sem1_Result = newResult == "None" ? null : newResult; break;
                        case 2: student.Sem2_Result = newResult == "None" ? null : newResult; break;
                        case 3: student.Sem3_Result = newResult == "None" ? null : newResult; break;
                        case 4: student.Sem4_Result = newResult == "None" ? null : newResult; break;
                        case 5: student.Sem5_Result = newResult == "None" ? null : newResult; break;
                        case 6: student.Sem6_Result = newResult == "None" ? null : newResult; break;
                        case 7: student.Sem7_Result = newResult == "None" ? null : newResult; break;
                        case 8: student.Sem8_Result = newResult == "None" ? null : newResult; break;
                        case 9: student.Sem9_Result = newResult == "None" ? null : newResult; break;
                    }
                    TriggerToast($"Successfully set Semester {semesterNum} result to {displayResult} for {student.FullName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating result: {ex.Message}");
            }
        }

        private void TriggerToast(string message)
        {
            ToastMessage = message;
            ShowToast = true;
            StateHasChanged();

            Task.Delay(3000).ContinueWith(_ =>
            {
                ShowToast = false;
                InvokeAsync(StateHasChanged);
            });
        }
    }
}
