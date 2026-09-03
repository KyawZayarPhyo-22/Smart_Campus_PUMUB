using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.Components.Admin.Services;
using Smart_Campus_PUMUB.Components.Features.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student
{
    public partial class Page_StudentRanking : ComponentBase, IDisposable
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public AdminLanguageService LangService { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        // Active Tab: "subject", "semester", "master"
        private string ActiveTab { get; set; } = "subject";

        // Filter State
        private string _selectedFaculty = "All";
        private string SelectedFaculty
        {
            get => _selectedFaculty;
            set
            {
                if (_selectedFaculty != value)
                {
                    _selectedFaculty = value;
                    OnFacultyChanged();
                }
            }
        }

        private string _selectedMajor = "All";
        private string SelectedMajor
        {
            get => _selectedMajor;
            set
            {
                if (_selectedMajor != value)
                {
                    _selectedMajor = value;
                    OnMajorChanged();
                }
            }
        }

        private string SelectedAcademicYear { get; set; } = "All";

        private string _selectedSemester = "All";
        private string SelectedSemester
        {
            get => _selectedSemester;
            set
            {
                if (_selectedSemester != value)
                {
                    _selectedSemester = value;
                    OnSemesterChanged();
                }
            }
        }

        private string SelectedSubjectCode { get; set; } = "All";
        private string SelectedEligibility { get; set; } = "EligibleOnly";
        private string SelectedMasterStatus { get; set; } = "All";
        private int SelectedTopN { get; set; } = 0; // 0 = All, 5, 10, 20, 50, 100
        private string SearchTerm { get; set; } = "";

        // Subject Search within Dropdown
        private string SubjectDropdownSearch { get; set; } = "";
        private bool IsSubjectDropdownOpen { get; set; } = false;

        // Pagination State
        private int CurrentPage { get; set; } = 1;
        private int PageSize { get; set; } = 10;
        private int TotalCount { get; set; } = 0;
        private int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        private bool HasPreviousPage => CurrentPage > 1;
        private bool HasNextPage => CurrentPage < TotalPages;

        // Data Lists
        private List<StudentSubjectRankItemModel> SubjectRankList { get; set; } = new();
        private List<StudentSemesterRankItemModel> SemesterRankList { get; set; } = new();
        private List<StudentMasterEligibilityItemModel> MasterEligibilityList { get; set; } = new();
        private StudentRankingFilterOptionsDto FilterOptions { get; set; } = new();

        // Modal States
        private bool ShowDetailModal { get; set; } = false;
        private StudentSemesterRankItemModel? SelectedSemesterStudent { get; set; } = null;

        private bool ShowMasterModal { get; set; } = false;
        private StudentMasterEligibilityItemModel? SelectedMasterStudent { get; set; } = null;

        private bool IsLoading { get; set; } = false;

        private bool HasAnyFilterApplied =>
            (SelectedFaculty != "All" && !string.IsNullOrWhiteSpace(SelectedFaculty)) ||
            (SelectedMajor != "All" && !string.IsNullOrWhiteSpace(SelectedMajor)) ||
            (SelectedAcademicYear != "All" && !string.IsNullOrWhiteSpace(SelectedAcademicYear)) ||
            (SelectedSemester != "All" && !string.IsNullOrWhiteSpace(SelectedSemester)) ||
            (SelectedSubjectCode != "All" && !string.IsNullOrWhiteSpace(SelectedSubjectCode)) ||
            (SelectedMasterStatus != "All" && !string.IsNullOrWhiteSpace(SelectedMasterStatus)) ||
            (SelectedTopN > 0) ||
            !string.IsNullOrWhiteSpace(SearchTerm);

        protected override async Task OnInitializedAsync()
        {
            LangService.OnLanguageChanged += HandleLanguageChanged;
            await LoadFilterOptions();
        }

        private void HandleLanguageChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            LangService.OnLanguageChanged -= HandleLanguageChanged;
        }

        private async Task LoadFilterOptions()
        {
            try
            {
                var options = await HttpClientService.ExecuteAsync<StudentRankingFilterOptionsDto>("StudentRanking/filter-options", EnumHttpMethod.Get);
                if (options != null)
                {
                    FilterOptions = options;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading filter options: {ex.Message}");
            }
        }

        /// <summary>
        /// Cascading Filter: Only majors belonging to the selected faculty
        /// </summary>
        private IEnumerable<MajorDropdownItemDto> AvailableMajorsForFilter
        {
            get
            {
                if (FilterOptions.Majors == null || !FilterOptions.Majors.Any())
                    return Enumerable.Empty<MajorDropdownItemDto>();

                if (string.IsNullOrWhiteSpace(SelectedFaculty) || SelectedFaculty == "All")
                {
                    return FilterOptions.Majors;
                }

                return FilterOptions.Majors.Where(m =>
                    !string.IsNullOrEmpty(m.FacultyName) &&
                    m.FacultyName.Equals(SelectedFaculty, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Cascading Filter: Only subjects belonging to the selected faculty, major, and EXACT semester
        /// </summary>
        private IEnumerable<SubjectDropdownItemDto> AvailableSubjectsForFilter
        {
            get
            {
                if (FilterOptions.Subjects == null || !FilterOptions.Subjects.Any())
                    return Enumerable.Empty<SubjectDropdownItemDto>();

                var list = FilterOptions.Subjects.AsEnumerable();

                // 1. Filter by Faculty
                if (!string.IsNullOrWhiteSpace(SelectedFaculty) && SelectedFaculty != "All")
                {
                    bool isComputing = SelectedFaculty.Contains("Computing", StringComparison.OrdinalIgnoreCase);
                    list = list.Where(s =>
                    {
                        if (!string.IsNullOrEmpty(s.FacultyName) && s.FacultyName.Equals(SelectedFaculty, StringComparison.OrdinalIgnoreCase))
                            return true;

                        if (!string.IsNullOrEmpty(s.MajorName))
                        {
                            var matchedMajor = FilterOptions.Majors.FirstOrDefault(m => m.MajorName.Equals(s.MajorName, StringComparison.OrdinalIgnoreCase));
                            if (matchedMajor != null && !string.IsNullOrEmpty(matchedMajor.FacultyName) && matchedMajor.FacultyName.Equals(SelectedFaculty, StringComparison.OrdinalIgnoreCase))
                                return true;
                        }

                        if (isComputing && (s.SubjectCode.StartsWith("CST-", StringComparison.OrdinalIgnoreCase) ||
                                            s.SubjectCode.StartsWith("CS-", StringComparison.OrdinalIgnoreCase) ||
                                            s.SubjectCode.StartsWith("CT-", StringComparison.OrdinalIgnoreCase) ||
                                            s.SubjectCode.StartsWith("E-", StringComparison.OrdinalIgnoreCase) ||
                                            s.SubjectCode.StartsWith("P-", StringComparison.OrdinalIgnoreCase) ||
                                            s.SubjectCode.StartsWith("M-", StringComparison.OrdinalIgnoreCase)))
                            return true;

                        return false;
                    });
                }

                // 2. Filter by Major (Only when in tabs other than subject ranking)
                if (ActiveTab != "subject" && !string.IsNullOrWhiteSpace(SelectedMajor) && SelectedMajor != "All")
                {
                    list = list.Where(s =>
                        string.IsNullOrEmpty(s.MajorName) ||
                        s.MajorName.Equals(SelectedMajor, StringComparison.OrdinalIgnoreCase) ||
                        s.SubjectCode.StartsWith("CST-", StringComparison.OrdinalIgnoreCase));
                }

                // 3. Filter by EXACT Semester (Only when in tabs other than subject ranking)
                if (ActiveTab != "subject" && !string.IsNullOrWhiteSpace(SelectedSemester) && SelectedSemester != "All")
                {
                    var cleanTargetSem = SelectedSemester.Trim();
                    list = list.Where(s =>
                        !string.IsNullOrEmpty(s.SemesterName) &&
                        s.SemesterName.Trim().Equals(cleanTargetSem, StringComparison.OrdinalIgnoreCase));
                }

                // 4. Filter by Dropdown Search box if user typed text
                if (!string.IsNullOrWhiteSpace(SubjectDropdownSearch))
                {
                    var term = SubjectDropdownSearch.Trim();
                    list = list.Where(s =>
                        s.SubjectCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        s.SubjectName.Contains(term, StringComparison.OrdinalIgnoreCase));
                }

                return list;
            }
        }

        private void ToggleSubjectDropdown()
        {
            IsSubjectDropdownOpen = !IsSubjectDropdownOpen;
            if (IsSubjectDropdownOpen)
            {
                SubjectDropdownSearch = "";
            }
        }

        private void SelectSubjectFromDropdown(string subjectCode)
        {
            SelectedSubjectCode = subjectCode;
            IsSubjectDropdownOpen = false;
            SubjectDropdownSearch = "";
        }

        private string GetSelectedSubjectDisplay()
        {
            if (string.IsNullOrEmpty(SelectedSubjectCode) || SelectedSubjectCode == "All")
            {
                return LangService.IsMyanmar ? "-- ဘာသာရပ် အားလုံး (All Subjects) --" : "-- All Subjects --";
            }

            var sub = FilterOptions.Subjects.FirstOrDefault(s => s.SubjectCode.Equals(SelectedSubjectCode, StringComparison.OrdinalIgnoreCase));
            if (sub != null)
            {
                return $"{sub.SubjectCode} - {sub.SubjectName}";
            }
            return SelectedSubjectCode;
        }

        private void OnFacultyChanged()
        {
            var validMajors = AvailableMajorsForFilter.Select(m => m.MajorName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (SelectedMajor != "All" && !validMajors.Contains(SelectedMajor))
            {
                _selectedMajor = "All";
            }

            var validSubjects = AvailableSubjectsForFilter.Select(s => s.SubjectCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (SelectedSubjectCode != "All" && !validSubjects.Contains(SelectedSubjectCode))
            {
                SelectedSubjectCode = "All";
            }
        }

        private void OnMajorChanged()
        {
            var validSubjects = AvailableSubjectsForFilter.Select(s => s.SubjectCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (SelectedSubjectCode != "All" && !validSubjects.Contains(SelectedSubjectCode))
            {
                SelectedSubjectCode = "All";
            }
        }

        private void OnSemesterChanged()
        {
            var validSubjects = AvailableSubjectsForFilter.Select(s => s.SubjectCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (SelectedSubjectCode != "All" && !validSubjects.Contains(SelectedSubjectCode))
            {
                SelectedSubjectCode = "All";
            }
        }

        private async Task ExecuteFilter()
        {
            CurrentPage = 1;
            await ApplyFilters();
        }

        private async Task OnPageChanged(int page)
        {
            if (page >= 1 && page <= TotalPages && page != CurrentPage)
            {
                CurrentPage = page;
                await ApplyFilters();
            }
        }

        private void SwitchTab(string tab)
        {
            ActiveTab = tab;
            ResetFilters();
        }

        private async Task ApplyFilters()
        {
            if (!HasAnyFilterApplied)
            {
                SubjectRankList = new();
                SemesterRankList = new();
                MasterEligibilityList = new();
                TotalCount = 0;
                IsLoading = false;
                StateHasChanged();
                return;
            }

            IsLoading = true;
            StateHasChanged();

            try
            {

                if (ActiveTab == "subject")
                {
                    var url = $"StudentRanking/subject-ranking?facultyName={Uri.EscapeDataString(SelectedFaculty)}" +
                              $"&majorName=All" +
                              $"&subjectCode={Uri.EscapeDataString(SelectedSubjectCode)}" +
                              $"&semesterName=All" +
                              $"&academicYear={Uri.EscapeDataString(SelectedAcademicYear)}" +
                              $"&eligibilityFilter={Uri.EscapeDataString(SelectedEligibility)}" +
                              $"&topN={SelectedTopN}" +
                              $"&searchTerm={Uri.EscapeDataString(SearchTerm)}" +
                              $"&pageNumber={CurrentPage}" +
                              $"&pageSize={PageSize}";

                    var paged = await HttpClientService.ExecuteAsync<PagedResultDto<StudentSubjectRankItemModel>>(url, EnumHttpMethod.Get);
                    if (paged != null)
                    {
                        SubjectRankList = paged.Items;
                        TotalCount = paged.TotalCount;
                    }
                    else
                    {
                        SubjectRankList = new();
                        TotalCount = 0;
                    }
                }
                else if (ActiveTab == "semester")
                {
                    var url = $"StudentRanking/semester-ranking?facultyName={Uri.EscapeDataString(SelectedFaculty)}" +
                              $"&majorName={Uri.EscapeDataString(SelectedMajor)}" +
                              $"&semesterName={Uri.EscapeDataString(SelectedSemester)}" +
                              $"&academicYear={Uri.EscapeDataString(SelectedAcademicYear)}" +
                              $"&eligibilityFilter={Uri.EscapeDataString(SelectedEligibility)}" +
                              $"&topN={SelectedTopN}" +
                              $"&searchTerm={Uri.EscapeDataString(SearchTerm)}" +
                              $"&pageNumber={CurrentPage}" +
                              $"&pageSize={PageSize}";

                    var paged = await HttpClientService.ExecuteAsync<PagedResultDto<StudentSemesterRankItemModel>>(url, EnumHttpMethod.Get);
                    if (paged != null)
                    {
                        SemesterRankList = paged.Items;
                        TotalCount = paged.TotalCount;
                    }
                    else
                    {
                        SemesterRankList = new();
                        TotalCount = 0;
                    }
                }
                else if (ActiveTab == "master")
                {
                    var url = $"StudentRanking/master-eligibility?facultyName={Uri.EscapeDataString(SelectedFaculty)}" +
                              $"&majorName={Uri.EscapeDataString(SelectedMajor)}" +
                              $"&academicYear={Uri.EscapeDataString(SelectedAcademicYear)}" +
                              $"&statusFilter={Uri.EscapeDataString(SelectedMasterStatus)}" +
                              $"&topN={SelectedTopN}" +
                              $"&searchTerm={Uri.EscapeDataString(SearchTerm)}" +
                              $"&pageNumber={CurrentPage}" +
                              $"&pageSize={PageSize}";

                    var paged = await HttpClientService.ExecuteAsync<PagedResultDto<StudentMasterEligibilityItemModel>>(url, EnumHttpMethod.Get);
                    if (paged != null)
                    {
                        MasterEligibilityList = paged.Items;
                        TotalCount = paged.TotalCount;
                    }
                    else
                    {
                        MasterEligibilityList = new();
                        TotalCount = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying rankings: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task ChangePage(int newPage)
        {
            if (newPage < 1 || (TotalPages > 0 && newPage > TotalPages) || newPage == CurrentPage) return;
            CurrentPage = newPage;
            await ApplyFilters();
        }

        private async Task OnPageSizeChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int size))
            {
                PageSize = size;
                CurrentPage = 1;
                await ApplyFilters();
            }
        }

        private void ResetFilters()
        {
            _selectedFaculty = "All";
            _selectedMajor = "All";
            SelectedAcademicYear = "All";
            SelectedSemester = "All";
            SelectedSubjectCode = "All";
            SelectedEligibility = "EligibleOnly";
            SelectedMasterStatus = "All";
            SelectedTopN = 0;
            SearchTerm = "";
            SubjectDropdownSearch = "";
            IsSubjectDropdownOpen = false;
            CurrentPage = 1;
            SubjectRankList = new();
            SemesterRankList = new();
            MasterEligibilityList = new();
            TotalCount = 0;
            IsLoading = false;
            StateHasChanged();
        }

        private async Task HandleKeyUp(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                CurrentPage = 1;
                await ApplyFilters();
            }
        }

        private int GetActiveListCount()
        {
            if (ActiveTab == "subject") return SubjectRankList.Count;
            if (ActiveTab == "semester") return SemesterRankList.Count;
            if (ActiveTab == "master") return MasterEligibilityList.Count;
            return 0;
        }

        private string GetGradePillClass(string? grade)
        {
            if (string.IsNullOrEmpty(grade)) return "grade-pill-pass";
            var clean = grade.Trim().ToUpperInvariant();
            if (clean == "A+" || clean == "A" || clean == "A-") return "grade-pill-dist";
            if (clean == "B+" || clean == "B" || clean == "B-") return "grade-pill-good";
            if (clean == "C+" || clean == "C") return "grade-pill-pass";
            return "grade-pill-fail";
        }

        private void OpenSemesterDetailModal(StudentSemesterRankItemModel student)
        {
            SelectedSemesterStudent = student;
            ShowDetailModal = true;
        }

        private void OpenMasterDetailModal(StudentMasterEligibilityItemModel student)
        {
            SelectedMasterStudent = student;
            ShowMasterModal = true;
        }

        private void CloseModal()
        {
            ShowDetailModal = false;
            ShowMasterModal = false;
            SelectedSemesterStudent = null;
            SelectedMasterStudent = null;
        }

        private async Task ExportToCsv()
        {
            try
            {
                var sb = new StringBuilder();

                if (ActiveTab == "subject")
                {
                    var exportUrl = $"StudentRanking/subject-ranking?facultyName={Uri.EscapeDataString(SelectedFaculty)}&majorName={Uri.EscapeDataString(SelectedMajor)}&subjectCode={Uri.EscapeDataString(SelectedSubjectCode)}&semesterName={Uri.EscapeDataString(SelectedSemester)}&academicYear={Uri.EscapeDataString(SelectedAcademicYear)}&eligibilityFilter={Uri.EscapeDataString(SelectedEligibility)}&topN={SelectedTopN}&searchTerm={Uri.EscapeDataString(SearchTerm)}&pageNumber=1&pageSize=100000";
                    var exportRes = await HttpClientService.ExecuteAsync<PagedResultDto<StudentSubjectRankItemModel>>(exportUrl, EnumHttpMethod.Get);
                    var items = exportRes?.Items ?? SubjectRankList;

                    sb.AppendLine("Rank,Roll No,Student Name,Faculty,Major,Academic Year,Semester,Subject Code,Subject Name,Credits,Marks Obtained,Grade,Grade Point,Degree Eligible,Status");
                    foreach (var r in items)
                    {
                        sb.AppendLine($"\"{r.Rank}\",\"{r.RollNo}\",\"{r.StudentName}\",\"{r.FacultyName}\",\"{r.MajorName}\",\"{r.AcademicYear}\",\"{r.SemesterName}\",\"{r.SubjectCode}\",\"{r.SubjectName}\",\"{r.CreditUnit}\",\"{r.MarksObtained}\",\"{r.Grade}\",\"{r.GradePoint}\",\"{(r.IsDegreeEligible ? "Eligible" : "Non-Degree")}\",\"{(r.IsPass ? "Passed" : "Failed")}\"");
                    }
                }
                else if (ActiveTab == "semester")
                {
                    var exportUrl = $"StudentRanking/semester-ranking?facultyName={Uri.EscapeDataString(SelectedFaculty)}&majorName={Uri.EscapeDataString(SelectedMajor)}&semesterName={Uri.EscapeDataString(SelectedSemester)}&academicYear={Uri.EscapeDataString(SelectedAcademicYear)}&eligibilityFilter={Uri.EscapeDataString(SelectedEligibility)}&topN={SelectedTopN}&searchTerm={Uri.EscapeDataString(SearchTerm)}&pageNumber=1&pageSize=100000";
                    var exportRes = await HttpClientService.ExecuteAsync<PagedResultDto<StudentSemesterRankItemModel>>(exportUrl, EnumHttpMethod.Get);
                    var items = exportRes?.Items ?? SemesterRankList;

                    sb.AppendLine("Rank,Roll No,Student Name,Faculty,Major,Academic Year,Semester,Subjects Count,Total Credits,Total Marks,Average Marks,Semester GPA,Degree Eligible,Status");
                    foreach (var r in items)
                    {
                        sb.AppendLine($"\"{r.Rank}\",\"{r.RollNo}\",\"{r.StudentName}\",\"{r.FacultyName}\",\"{r.MajorName}\",\"{r.AcademicYear}\",\"{r.SemesterName}\",\"{r.TotalSubjectsCount}\",\"{r.TotalCredits}\",\"{r.TotalMarks}\",\"{r.AverageMarks}\",\"{r.SemesterGPA}\",\"{(r.IsDegreeEligible ? "Eligible" : "Non-Degree")}\",\"{(r.IsPassAll ? "Passed All" : $"{r.FailedSubjectsCount} Failed")}\"");
                    }
                }
                else if (ActiveTab == "master")
                {
                    var exportUrl = $"StudentRanking/master-eligibility?facultyName={Uri.EscapeDataString(SelectedFaculty)}&majorName={Uri.EscapeDataString(SelectedMajor)}&academicYear={Uri.EscapeDataString(SelectedAcademicYear)}&statusFilter={Uri.EscapeDataString(SelectedMasterStatus)}&topN={SelectedTopN}&searchTerm={Uri.EscapeDataString(SearchTerm)}&pageNumber=1&pageSize=100000";
                    var exportRes = await HttpClientService.ExecuteAsync<PagedResultDto<StudentMasterEligibilityItemModel>>(exportUrl, EnumHttpMethod.Get);
                    var items = exportRes?.Items ?? MasterEligibilityList;

                    sb.AppendLine("Rank,Roll No,Student Name,Faculty,Major,Academic Year,Completed Semesters,Total Credits,Total Cumulative Marks,CGPA,Master Eligibility");
                    foreach (var r in items)
                    {
                        sb.AppendLine($"\"{r.Rank}\",\"{r.RollNo}\",\"{r.StudentName}\",\"{r.FacultyName}\",\"{r.MajorName}\",\"{r.AcademicYear}\",\"{r.CompletedSemestersCount}\",\"{r.TotalCompletedCredits}\",\"{r.TotalCumulativeMarks}\",\"{r.CumulativeGPA}\",\"{(r.IsMasterEligible ? "Master Eligible" : "Bachelor Only")}\"");
                    }
                }

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var base64 = Convert.ToBase64String(bytes);
                var fileName = $"Student_Ranking_{ActiveTab}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                await JS.InvokeVoidAsync("eval", $@"
                    var link = document.createElement('a');
                    link.download = '{fileName}';
                    link.href = 'data:text/csv;charset=utf-8;base64,{base64}';
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting CSV: {ex.Message}");
            }
        }

        private async Task PrintReport()
        {
            try
            {
                if (!HasAnyFilterApplied)
                {
                    await ExecuteFilter();
                    StateHasChanged();
                    await Task.Delay(300);
                }
                await JS.InvokeVoidAsync("window.print");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error printing report: {ex.Message}");
            }
        }
    }
}
