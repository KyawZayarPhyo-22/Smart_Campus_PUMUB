using Microsoft.AspNetCore.Components;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student
{
    public partial class Page_StudentDirectory : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private List<StudentModel> StudentList { get; set; } = new();
        private List<FacultyModel> FacultyList { get; set; } = new();
        private List<MajorModel> MajorList { get; set; } = new();
        private List<SemesterModel> SemesterList { get; set; } = new();

        // Filters
        private string SearchInput { get; set; } = "";
        private string SelectedFacultyInput { get; set; } = "All";
        private string SelectedMajorInput { get; set; } = "All";
        private string SelectedYearInput { get; set; } = "All";

        private string SearchTerm { get; set; } = "";
        private string SelectedFaculty { get; set; } = "All";
        private string SelectedMajor { get; set; } = "All";
        private string SelectedYear { get; set; } = "All";

        private bool IsLoading { get; set; } = true;

        // Permissions Variables
        private List<string> userPermissions = new();
        private bool canManageStudent = true;

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

        private bool isFacultyAdminLocked = false;
        private int? _userFacultyId = null;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                userPermissions = user.Claims
                                      .Where(c => c.Type == "Permission")
                                      .Select(c => c.Value)
                                      .ToList();
                                      
                canManageStudent = userPermissions.Contains("Student.Edit") || userPermissions.Contains("Student.Delete");

                var roleName = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
                var roleIdStr = user.FindFirst("RoleId")?.Value;

                bool isSuperAdmin = string.Equals(roleName, "Super Admin", StringComparison.OrdinalIgnoreCase) || roleIdStr == "4";

                if (!isSuperAdmin)
                {
                    var userFacultyIdStr = user.FindFirst("FacultyId")?.Value;
                    if (!string.IsNullOrEmpty(userFacultyIdStr) && int.TryParse(userFacultyIdStr, out int userFacultyId) && userFacultyId > 0)
                    {
                        var userFaculty = FacultyList.FirstOrDefault(f => f.FacultyId == userFacultyId);
                        if (userFaculty != null && !string.IsNullOrEmpty(userFaculty.FacultyName))
                        {
                            _userFacultyId = userFacultyId;
                            SelectedFacultyInput = userFaculty.FacultyName;
                            SelectedFaculty = userFaculty.FacultyName;
                            isFacultyAdminLocked = true;
                            // Reload students from API with faculty filter applied
                            await LoadStudents(_userFacultyId);
                        }
                    }
                }

                StateHasChanged();
            }
        }

        private async Task LoadStudents(int? facultyId = null)
        {
            IsLoading = true;
            try
            {
                var studentUrl = "Student";
                if (facultyId.HasValue && facultyId.Value > 0)
                {
                    studentUrl += $"?facultyId={facultyId.Value}";
                }

                var studentTask = HttpClientService.ExecuteAsync<List<StudentModel>>(studentUrl, EnumHttpMethod.Get);
                var facultyTask = HttpClientService.ExecuteAsync<List<FacultyModel>>("faculty", EnumHttpMethod.Get);
                var majorTask = HttpClientService.ExecuteAsync<List<MajorModel>>("major", EnumHttpMethod.Get);
                var semesterTask = HttpClientService.ExecuteAsync<List<SemesterModel>>("Semester", EnumHttpMethod.Get);

                await Task.WhenAll(studentTask, facultyTask, majorTask, semesterTask);

                StudentList = await studentTask ?? new();
                FacultyList = await facultyTask ?? new();
                MajorList = await majorTask ?? new();
                SemesterList = await semesterTask ?? new();
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

        private void OnFacultyChanged()
        {
            SelectedMajorInput = "All";
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            SearchTerm = SearchInput;
            SelectedFaculty = SelectedFacultyInput;
            SelectedMajor = SelectedMajorInput;
            SelectedYear = SelectedYearInput;
            CurrentPage = 1;
            StateHasChanged();
        }

        private void ResetFilter()
        {
            SearchInput = "";
            SelectedFacultyInput = "All";
            SelectedMajorInput = "All";
            SelectedYearInput = "All";

            SearchTerm = "";
            SelectedFaculty = "All";
            SelectedMajor = "All";
            SelectedYear = "All";
            CurrentPage = 1;
            StateHasChanged();
        }

        private IEnumerable<string> AvailableFaculties
        {
            get
            {
                var apiFaculties = FacultyList
                    .Where(f => !string.IsNullOrWhiteSpace(f.FacultyName))
                    .Select(f => f.FacultyName!.Trim());

                var studentFaculties = StudentList
                    .Where(s => !string.IsNullOrWhiteSpace(s.FacultyName))
                    .Select(s => s.FacultyName!.Trim());

                return apiFaculties.Union(studentFaculties).Distinct().OrderBy(f => f);
            }
        }

        private IEnumerable<string> AvailableMajors
        {
            get
            {
                var majorsQuery = MajorList.AsEnumerable();

                if (SelectedFacultyInput != "All")
                {
                    majorsQuery = majorsQuery.Where(m =>
                        string.Equals(m.FacultyName?.Trim(), SelectedFacultyInput.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                var apiMajors = majorsQuery
                    .Where(m => !string.IsNullOrWhiteSpace(m.MajorName) && !string.Equals(m.MajorName.Trim(), "N/A", StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.MajorName!.Trim());

                if (SelectedFacultyInput == "All")
                {
                    var studentMajors = StudentList
                        .Where(s => !string.IsNullOrWhiteSpace(s.CurrentMajor) && !string.Equals(s.CurrentMajor.Trim(), "N/A", StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.CurrentMajor.Trim());

                    return apiMajors.Union(studentMajors).Distinct().OrderBy(m => m);
                }

                return apiMajors.Distinct().OrderBy(m => m);
            }
        }

        private IEnumerable<string> AvailableClassYears
        {
            get
            {
                var semesterNames = SemesterList
                    .Where(s => !string.IsNullOrWhiteSpace(s.SemesterName) 
                             && !string.Equals(s.SemesterName.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(s.SemesterName.Trim(), "First Year", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.SemesterName!.Trim());

                var studentYears = StudentList
                    .Where(s => !string.IsNullOrWhiteSpace(s.CurrentClassYear) 
                             && !string.Equals(s.CurrentClassYear.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(s.CurrentClassYear.Trim(), "First Year", StringComparison.OrdinalIgnoreCase))
                    .Select(s => s.CurrentClassYear.Trim());

                return semesterNames.Union(studentYears).Distinct();
            }
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

            if (SelectedFaculty != "All")
            {
                var majorsInFaculty = MajorList
                    .Where(m => string.Equals(m.FacultyName?.Trim(), SelectedFaculty.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.MajorName?.Trim().ToLower())
                    .Where(m => m != null)
                    .ToHashSet();

                data = data.Where(s =>
                    (s.FacultyName != null && string.Equals(s.FacultyName.Trim(), SelectedFaculty.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                    (s.CurrentMajor != null && (majorsInFaculty.Contains(s.CurrentMajor.Trim().ToLower()) ||
                                               majorsInFaculty.Any(m => s.CurrentMajor.ToLower().Contains(m!))))
                );
            }

            if (SelectedMajor != "All")
            {
                data = data.Where(s => string.Equals(s.CurrentMajor?.Trim(), SelectedMajor.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                       (s.CurrentMajor != null && s.CurrentMajor.ToLower().Contains(SelectedMajor.Trim().ToLower())));
            }

            if (SelectedYear != "All")
            {
                data = data.Where(s => string.Equals(s.CurrentClassYear?.Trim(), SelectedYear.Trim(), StringComparison.OrdinalIgnoreCase));
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
            "Credit_Transferred" => "credit",
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
