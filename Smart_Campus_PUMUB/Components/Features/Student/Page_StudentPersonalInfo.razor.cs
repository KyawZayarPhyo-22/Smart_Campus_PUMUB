using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Smart_Campus_PUMUB.Components.Features.Student
{
    public partial class Page_StudentPersonalInfo : ComponentBase, IDisposable
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public NavigationManager Nav { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
        [Inject] public StudentRegistrationNotifierService NotifierService { get; set; } = null!;
        [Inject] public IConfiguration Configuration { get; set; } = null!;

        [Parameter] public int? AdminViewUserId { get; set; }
        [Parameter] public int? AdminViewNewStudentAccId { get; set; }
        public bool IsAdminView => AdminViewUserId.HasValue || AdminViewNewStudentAccId.HasValue;

        private const string PendingConfirmationStatus = "Pending Confirmation";
        private const string LegacyPendingStatus = "Pending";
        private const string ApprovedStatus = "Approved";
        private const string RejectedStatus = "Rejected";

        public StudentPersonalInfoRequest requestModel { get; set; } = new()
        {
            nationality_status = "တိုင်းရင်းသား",
            stipend_requested = false,
            gender_relation = "Male",
            blood_type = "O",
            academic_year_range = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}"
            // admission_year intentionally left null — filled from DB or user input
        };

        public bool ShowModal { get; set; } = false;
        public string ModalMessage { get; set; } = "";
        public bool IsSuccessModal { get; set; } = false;
        public bool ShowRegistrationStatusPanel { get; set; } = false;
        public string RegistrationReviewStatus { get; set; } = "";
        public bool CanProceedToPayment { get; set; } = false;
        public bool IsRefreshingStatus { get; set; } = false;
        public int SubmittedRegistrationId { get; set; }
        public int? SubmittedUserId { get; set; }

        // --- Graduation / Semester Progression ---
        public bool IsGraduated { get; set; } = false;
        public int TotalRequiredSemesters { get; set; } = 8;
        public int AllowedSemesterSequence { get; set; } = 1;
        public string? AllowedSemesterName { get; set; }

        //public void CloseModal()
        //{
        //    ShowModal = false;
        //    if (IsSuccessModal) Nav.NavigateTo("/student/payment");
        //}

        public int CurrentStep { get; set; } = 1;
        private const int TotalSteps = 4;
        public bool IsSubmitting { get; set; } = false;

        public DateTime? DobDate { get; set; }
        public string MaxAllowedDobString => DateTime.Today.AddYears(-16).ToString("yyyy-MM-dd");
        public DateTime? CovidDate { get; set; }
        public DateTime? SignDate { get; set; } = DateTime.Today;

        public string? PreviewImageUrl { get; set; }
        public IBrowserFile? SelectedPhotoFile { get; set; }
        public byte[]? SelectedPhotoBytes { get; set; }

        public string? PreviewNrcFrontUrl { get; set; }
        public IBrowserFile? SelectedNrcFrontFile { get; set; }
        public byte[]? SelectedNrcFrontBytes { get; set; }

        public string? PreviewNrcBackUrl { get; set; }
        public IBrowserFile? SelectedNrcBackFile { get; set; }
        public byte[]? SelectedNrcBackBytes { get; set; }

        public string? PreviewCensusUrl { get; set; }
        public IBrowserFile? SelectedCensusFile { get; set; }
        public byte[]? SelectedCensusBytes { get; set; }

        // Parent NRC images
        public string? PreviewFatherNrcFrontUrl { get; set; }
        public IBrowserFile? SelectedFatherNrcFrontFile { get; set; }
        public byte[]? SelectedFatherNrcFrontBytes { get; set; }

        public string? PreviewFatherNrcBackUrl { get; set; }
        public IBrowserFile? SelectedFatherNrcBackFile { get; set; }
        public byte[]? SelectedFatherNrcBackBytes { get; set; }

        public string? PreviewMotherNrcFrontUrl { get; set; }
        public IBrowserFile? SelectedMotherNrcFrontFile { get; set; }
        public byte[]? SelectedMotherNrcFrontBytes { get; set; }

        public string? PreviewMotherNrcBackUrl { get; set; }
        public IBrowserFile? SelectedMotherNrcBackFile { get; set; }
        public byte[]? SelectedMotherNrcBackBytes { get; set; }

        public List<SemesterModel> SemesterList { get; set; } = new();

        // --- Faculty & Filtered Major ---
        public List<FacultyModel> FacultyList { get; set; } = new();
        public List<MajorModel> MajorList { get; set; } = new();

        // String-backed property to avoid int?/string type mismatch with <select @bind>
        private int? _selectedFacultyId;
        public int? SelectedFacultyId
        {
            get => _selectedFacultyId;
            set => _selectedFacultyId = value;
        }
        public string SelectedFacultyIdStr
        {
            get => _selectedFacultyId?.ToString() ?? "";
            set
            {
                if (int.TryParse(value, out var id))
                    _selectedFacultyId = id;
                else
                    _selectedFacultyId = null;
            }
        }

        public List<string> FilteredMajors { get; set; } = new();

        public void OnFacultyChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var facId))
            {
                SelectedFacultyId = facId;
            }
            else
            {
                SelectedFacultyId = null;
            }
            UpdateFilteredMajors();
            StateHasChanged();
        }

        // Called after @bind updates SelectedFacultyId — filters majors accordingly
        public void OnFacultyChangedAfterBind()
        {
            UpdateFilteredMajors();
            StateHasChanged();
        }

        private void UpdateFilteredMajors()
        {
            if (SelectedFacultyId.HasValue && SelectedFacultyId.Value > 0)
            {
                // 1. Direct match by FacultyId from DB MajorList
                var majorsForFaculty = MajorList
                    .Where(m => m.FacultyId == SelectedFacultyId.Value)
                    .Select(m => m.MajorName?.Trim())
                    .Where(m => !string.IsNullOrEmpty(m))
                    .Select(m => m!)
                    .ToList();

                // 2. Fallback heuristic matching if needed
                if (!majorsForFaculty.Any())
                {
                    var fac = FacultyList.FirstOrDefault(f => f.FacultyId == SelectedFacultyId.Value);
                    var facName = fac?.FacultyName?.Trim() ?? "";

                    if (facName.Contains("Comput", StringComparison.OrdinalIgnoreCase) ||
                        facName.Contains("IT", StringComparison.OrdinalIgnoreCase) ||
                        facName.Contains("Information", StringComparison.OrdinalIgnoreCase))
                    {
                        majorsForFaculty = MajorList
                            .Where(m => m.MajorName != null && (m.MajorName.Contains("Computer", StringComparison.OrdinalIgnoreCase) || m.MajorName.Contains("Information", StringComparison.OrdinalIgnoreCase)))
                            .Select(m => m.MajorName!.Trim())
                            .ToList();

                        if (!majorsForFaculty.Any())
                        {
                            majorsForFaculty = new() { "Computer Science", "Computer Technology", "Information Technology" };
                        }
                    }
                    else if (facName.Contains("Engineer", StringComparison.OrdinalIgnoreCase) ||
                             facName.Contains("Civil", StringComparison.OrdinalIgnoreCase) ||
                             facName.Contains("Electrical", StringComparison.OrdinalIgnoreCase) ||
                             facName.Contains("Mechanical", StringComparison.OrdinalIgnoreCase))
                    {
                        majorsForFaculty = MajorList
                            .Where(m => m.MajorName != null && m.MajorName.Contains("Engineering", StringComparison.OrdinalIgnoreCase))
                            .Select(m => m.MajorName!.Trim())
                            .ToList();

                        if (!majorsForFaculty.Any())
                        {
                            majorsForFaculty = new() { "Civil Engineering", "Electrical Engineering", "Mechanical Engineering", "Mechatronic Engineering", "Electronic Engineering", "Electrical Power Engineering" };
                        }
                    }
                }

                FilteredMajors = majorsForFaculty.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // Reset major selection if the currently selected major is not in the filtered list
                if (!string.IsNullOrEmpty(requestModel.major) && !FilteredMajors.Contains(requestModel.major, StringComparer.OrdinalIgnoreCase))
                {
                    requestModel.major = null;
                }
            }
            else
            {
                FilteredMajors = MajorList.Any()
                    ? MajorList.Select(m => m.MajorName?.Trim()).Where(m => !string.IsNullOrEmpty(m)).Select(m => m!).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                    : GetAllMajors();
            }
        }

        private void AutoSelectFacultyForMajor(string? major)
        {
            if (string.IsNullOrEmpty(major)) return;

            var matchedMajor = MajorList.FirstOrDefault(m => string.Equals(m.MajorName?.Trim(), major.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matchedMajor?.FacultyId != null && matchedMajor.FacultyId > 0)
            {
                SelectedFacultyId = matchedMajor.FacultyId;
                UpdateFilteredMajors();
                return;
            }

            if (major.Contains("Computer", StringComparison.OrdinalIgnoreCase) || major.Contains("Information", StringComparison.OrdinalIgnoreCase) || major.Equals("CS", StringComparison.OrdinalIgnoreCase) || major.Equals("CT", StringComparison.OrdinalIgnoreCase))
            {
                var compFac = FacultyList.FirstOrDefault(f => f.FacultyName != null && (f.FacultyName.Contains("Comput", StringComparison.OrdinalIgnoreCase) || f.FacultyName.Contains("Information", StringComparison.OrdinalIgnoreCase)));
                if (compFac != null)
                {
                    SelectedFacultyId = compFac.FacultyId;
                    UpdateFilteredMajors();
                    return;
                }
            }
            else if (major.Contains("Engineer", StringComparison.OrdinalIgnoreCase))
            {
                var engFac = FacultyList.FirstOrDefault(f => f.FacultyName != null && f.FacultyName.Contains("Engineer", StringComparison.OrdinalIgnoreCase));
                if (engFac != null)
                {
                    SelectedFacultyId = engFac.FacultyId;
                    UpdateFilteredMajors();
                    return;
                }
            }
        }

        private static List<string> GetAllMajors() => new()
        {
            "Computer Science", "Computer Technology", "Information Technology",
            "Civil Engineering", "Electronic Engineering", "Electrical Power Engineering",
            "Mechanical Engineering", "Mechatronic Engineering"
        };
        public string PastExamSemester { get; set; } = "";
        public DateTime? PastExamDate { get; set; }

        public string NrcType { get; set; } = "(နိုင်)";
        public List<string> CurrentTownshipList { get; set; } = new();

        public string? GuardianNrcState { get; set; }
        public string? GuardianNrcTownship { get; set; }
        public string GuardianNrcType { get; set; } = "(နိုင်)";
        public string? GuardianNrcNumber { get; set; }
        public List<string> GuardianTownshipList { get; set; } = new();

        public string? FatherNrcState { get; set; }
        public string? FatherNrcTownship { get; set; }
        public string FatherNrcType { get; set; } = "(နိုင်)";
        public string? FatherNrcNumber { get; set; }
        public List<string> FatherTownshipList { get; set; } = new();

        private readonly Dictionary<string, List<string>> NrcTownshipsByState = NrcDataHelper.TownshipsByState;

        private StudentModel? LoggedInStudent { get; set; }

        protected override async Task OnInitializedAsync()
        {
            NotifierService.OnRegistrationStatusChanged += HandleRegistrationStatusChanged;

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                // 💡 UserModel ရှိ "UserId" ကို အဓိကထား၍ Auto Fill ဆွဲယူမည်
                var userIdString = user.FindFirst("UserId")?.Value
                                ?? user.FindFirst("User_Id")?.Value
                                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                ?? user.FindFirst("id")?.Value;

                var newStudentAccIdString = user.FindFirst("NewStudentAccId")?.Value;

                Console.WriteLine("User Claims:");
                foreach (var claim in user.Claims)
                {
                    Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                }

                int parsedUserId = 0;
                int parsedNewStudentAccId = 0;
                bool isUserResolved = false;

                if (IsAdminView)
                {
                    if (AdminViewUserId.HasValue && AdminViewUserId.Value > 0)
                    {
                        parsedUserId = AdminViewUserId.Value;
                        requestModel.UserId = parsedUserId;
                        isUserResolved = true;
                        Console.WriteLine($"Admin viewing UserId: {parsedUserId}");
                    }
                    else if (AdminViewNewStudentAccId.HasValue && AdminViewNewStudentAccId.Value > 0)
                    {
                        parsedNewStudentAccId = AdminViewNewStudentAccId.Value;
                        requestModel.NewStudentAccId = parsedNewStudentAccId;
                        isUserResolved = true;
                        Console.WriteLine($"Admin viewing NewStudentAccId: {parsedNewStudentAccId}");
                    }
                }
                else if (user.IsInRole("NewStudent") && int.TryParse(newStudentAccIdString, out parsedNewStudentAccId))
                {
                    requestModel.NewStudentAccId = parsedNewStudentAccId;
                    requestModel.UserId = 0;
                    isUserResolved = true;
                    Console.WriteLine($"Logged in as NewStudent with AccId: {parsedNewStudentAccId}");
                }
                else if (int.TryParse(userIdString, out parsedUserId))
                {
                    requestModel.UserId = parsedUserId;
                    isUserResolved = true;
                }

                if (isUserResolved)
                {
                    if (requestModel.UserId.HasValue && requestModel.UserId > 0)
                    {
                        Console.WriteLine($"Using UserId: {requestModel.UserId}");
                    }
                    else
                    {
                        Console.WriteLine($"Using NewStudentAccId: {requestModel.NewStudentAccId}");
                    }

                    try
                    {
                        if (requestModel.UserId.HasValue && requestModel.UserId > 0)
                        {
                            var studentData = await HttpClientService.ExecuteAsync<StudentModel>($"Student/user/{parsedUserId}", EnumHttpMethod.Get);
                            if (studentData != null)
                            {
                                LoggedInStudent = studentData;
                                requestModel.roll_no = LoggedInStudent.CurrentRollNo;
                                Console.WriteLine($"Loaded student details for user: {parsedUserId}. Roll No auto-filled: {requestModel.roll_no}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading student details: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to parse UserId from string: '{userIdString}'");
                }
            }
            else
            {
                Console.WriteLine("User is not authenticated");
            }

            var semTask = HttpClientService.ExecuteAsync<List<SemesterModel>>("Semester", EnumHttpMethod.Get);
            var facTask = HttpClientService.ExecuteAsync<List<FacultyModel>>("Faculty", EnumHttpMethod.Get);
            var majorTask = HttpClientService.ExecuteAsync<List<MajorModel>>("Major", EnumHttpMethod.Get);

            Task? personalInfoTask = null;
            if (requestModel.NewStudentAccId.HasValue && requestModel.NewStudentAccId > 0)
            {
                personalInfoTask = LoadStudentPersonalInfoForNewStudent(requestModel.NewStudentAccId.Value);
            }
            else if (requestModel.UserId.HasValue && requestModel.UserId > 0)
            {
                personalInfoTask = LoadStudentPersonalInfo(requestModel.UserId.Value);
            }

            var initTasks = new List<Task> { semTask, facTask, majorTask };
            if (personalInfoTask != null) initTasks.Add(personalInfoTask);

            try
            {
                await Task.WhenAll(initTasks);
                
                var semRes = semTask.Result;
                if (semRes != null && semRes.Any())
                    SemesterList = semRes;
                else
                    LoadDefaultSemesters();

                var facRes = facTask.Result;
                if (facRes != null && facRes.Any())
                    FacultyList = facRes;

                var majorRes = majorTask.Result;
                if (majorRes != null && majorRes.Any())
                    MajorList = majorRes;
            }
            catch
            {
                LoadDefaultSemesters();
            }

            UpdateFilteredMajors();

            // --- Compute allowed semester based on academic history ---
            ComputeAllowedSemester();
        }

        private async Task LoadStudentPersonalInfo(int userId)
        {
            await LoadStudentPersonalInfoInternal($"studentpersonalinfo/{userId}", userId, 0);
        }

        private async Task LoadStudentPersonalInfoForNewStudent(int newStudentAccId)
        {
            await LoadStudentPersonalInfoInternal($"studentpersonalinfo/newstudent/{newStudentAccId}", 0, newStudentAccId);
        }

        private async Task LoadStudentPersonalInfoInternal(string endpoint, int userId, int newStudentAccId)
        {
            try
            {
                var info = await HttpClientService.ExecuteAsync<StudentPersonalInfoResponse>(
                    endpoint, EnumHttpMethod.Get);

                if (info != null && info.Id > 0)
                {
                    isUpdate = true;
                    IsFormDisabled = true;  // show data in read-only mode; user clicks "ပြင်ဆင်မည်" to edit
                    
                    requestModel.AdmissionSerialNo = info.AdmissionSerialNo;
                    requestModel.academic_year_range = info.academic_year_range;
                    requestModel.academic_year_level = info.academic_year_level;
                    requestModel.major = info.major;
                    requestModel.roll_no = info.roll_no;
                    requestModel.university_reg_no = info.university_reg_no;
                    requestModel.admission_year = info.admission_year;
                    requestModel.student_name_mm = info.student_name_mm;
                    requestModel.student_name_en = info.student_name_en;
                    requestModel.mother_name = info.mother_name;
                    requestModel.father_name = info.father_name;
                    requestModel.gender_relation = info.gender_relation;
                    requestModel.ethnicity = info.ethnicity;
                    requestModel.religion = info.religion;
                    requestModel.pob = info.pob;
                    requestModel.birth_place_region = info.birth_place_region;
                    requestModel.student_nrc_no = info.student_nrc_no;
                    requestModel.nationality_status = info.nationality_status;
                    requestModel.dob = info.dob;
                    requestModel.email = info.email;
                    requestModel.blood_type = info.blood_type;
                    requestModel.covid_vaccine_status = info.covid_vaccine_status;
                    requestModel.current_address = info.current_address;
                    requestModel.permanent_address_mm = info.permanent_address_mm;
                    requestModel.permanent_address_en = info.permanent_address_en;
                    requestModel.matric_roll_no = info.matric_roll_no;
                    requestModel.matric_passed_year = info.matric_passed_year;
                    requestModel.exam_center = info.exam_center;
                    requestModel.father_occupation = info.father_occupation;
                    requestModel.mother_occupation = info.mother_occupation;
                    requestModel.past_exam_major = info.past_exam_major;
                    requestModel.past_exam_roll_no = info.past_exam_roll_no;
                    requestModel.past_exam_year = info.past_exam_year;
                    requestModel.past_exam_status = info.past_exam_status;
                    requestModel.previous_year_roll_no = info.previous_year_roll_no;
                    requestModel.guardian_name = info.guardian_name;
                    requestModel.guardian_relationship = info.guardian_relationship;
                    requestModel.guardian_occupation = info.guardian_occupation;
                    requestModel.guardian_address_phone = info.guardian_address_phone;
                    requestModel.app_guardian_name = info.app_guardian_name;
                    requestModel.app_guardian_nrc = info.app_guardian_nrc;
                    requestModel.app_guardian_phone = info.app_guardian_phone;
                    requestModel.app_guardian_address = info.app_guardian_address;
                    requestModel.app_student_name = info.app_student_name;
                    requestModel.app_student_phone = info.app_student_phone;
                    requestModel.stipend_requested = info.stipend_requested;
                    requestModel.nrc_state = info.nrc_state;
                    requestModel.nrc_township = info.nrc_township;
                    requestModel.nrc_type = info.nrc_type;
                    requestModel.nrc_number = ToMyanmarDigits(info.nrc_number ?? "");

                    requestModel.nrc_front_image = info.nrc_front_image;
                    requestModel.nrc_back_image = info.nrc_back_image;
                    requestModel.census_image = info.census_image;

                    if (!string.IsNullOrEmpty(info.nrc_front_image)) PreviewNrcFrontUrl = GetImageUrl(info.nrc_front_image);
                    if (!string.IsNullOrEmpty(info.nrc_back_image)) PreviewNrcBackUrl = GetImageUrl(info.nrc_back_image);
                    if (!string.IsNullOrEmpty(info.census_image)) PreviewCensusUrl = GetImageUrl(info.census_image);

                    // Passport photo
                    requestModel.student_image = info.student_image;
                    if (!string.IsNullOrEmpty(info.student_image)) PreviewImageUrl = GetImageUrl(info.student_image);

                    // Parent NRC images
                    requestModel.father_nrc_front_image = info.father_nrc_front_image;
                    requestModel.father_nrc_back_image = info.father_nrc_back_image;
                    requestModel.mother_nrc_front_image = info.mother_nrc_front_image;
                    requestModel.mother_nrc_back_image = info.mother_nrc_back_image;
                    if (!string.IsNullOrEmpty(info.father_nrc_front_image)) PreviewFatherNrcFrontUrl = GetImageUrl(info.father_nrc_front_image);
                    if (!string.IsNullOrEmpty(info.father_nrc_back_image)) PreviewFatherNrcBackUrl = GetImageUrl(info.father_nrc_back_image);
                    if (!string.IsNullOrEmpty(info.mother_nrc_front_image)) PreviewMotherNrcFrontUrl = GetImageUrl(info.mother_nrc_front_image);
                    if (!string.IsNullOrEmpty(info.mother_nrc_back_image)) PreviewMotherNrcBackUrl = GetImageUrl(info.mother_nrc_back_image);

                    // Set Faculty & Majors from loaded info
                    if (info.FacultyId.HasValue && info.FacultyId.Value > 0)
                    {
                        SelectedFacultyId = info.FacultyId.Value;
                        UpdateFilteredMajors();
                    }
                    else if (!string.IsNullOrEmpty(info.major))
                    {
                        AutoSelectFacultyForMajor(info.major);
                    }

                    // UI helper fields
                    if (info.dob.HasValue)
                        DobDate = info.dob.Value;

                    if (!string.IsNullOrEmpty(info.covid_vaccine_status) && info.covid_vaccine_status != "-")
                    {
                        if (DateTime.TryParseExact(info.covid_vaccine_status, "dd-MM-yyyy",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var covidParsed))
                        {
                            CovidDate = covidParsed;
                        }
                        else if (DateTime.TryParse(info.covid_vaccine_status, out var covidParsed2))
                        {
                            CovidDate = covidParsed2;
                        }
                    }

                    if (!string.IsNullOrEmpty(info.nrc_state))
                    {
                        NrcType = info.nrc_type ?? "(နိုင်)";
                        if (NrcTownshipsByState.ContainsKey(info.nrc_state))
                            CurrentTownshipList = NrcTownshipsByState[info.nrc_state];
                    }
                    else if (!string.IsNullOrEmpty(info.student_nrc_no) && info.student_nrc_no != "-")
                    {
                        var sNrcRaw = info.student_nrc_no.Trim();
                        var slashIdx = sNrcRaw.IndexOf('/');
                        if (slashIdx > 0)
                        {
                            var state = sNrcRaw[..slashIdx].Trim();
                            var rest = sNrcRaw[(slashIdx + 1)..].Trim();
                            string? township = null;
                            string type = "(နိုင်)";
                            string? number = null;

                            var openParen  = rest.IndexOf('(');
                            var closeParen = rest.IndexOf(')');
                            if (openParen >= 0 && closeParen > openParen)
                            {
                                township = rest[..openParen].Trim();
                                type     = rest[openParen..(closeParen + 1)].Trim();
                                number   = rest[(closeParen + 1)..].Trim();
                            }
                            else
                            {
                                township = rest;
                            }

                            requestModel.nrc_state    = state;
                            requestModel.nrc_township = township;
                            NrcType                   = type;
                            requestModel.nrc_number   = ToMyanmarDigits(number ?? "");

                            if (!string.IsNullOrEmpty(state) && NrcTownshipsByState.ContainsKey(state))
                                CurrentTownshipList = NrcTownshipsByState[state];
                        }
                    }

                    // Guardian & Father NRC
                    if (!string.IsNullOrEmpty(info.app_guardian_nrc) && info.app_guardian_nrc != "-")
                    {
                        var gNrcRaw = info.app_guardian_nrc;
                        var slashIdx = gNrcRaw.IndexOf('/');
                        if (slashIdx > 0)
                        {
                            var state = gNrcRaw[..slashIdx];
                            var rest = gNrcRaw[(slashIdx + 1)..];
                            string? township = null;
                            string type = "(နိုင်)";
                            string? number = null;

                            var openParen  = rest.IndexOf('(');
                            var closeParen = rest.IndexOf(')');
                            if (openParen >= 0 && closeParen > openParen)
                            {
                                township = rest[..openParen];
                                type     = rest[openParen..(closeParen + 1)];
                                number   = rest[(closeParen + 1)..];
                            }
                            else
                            {
                                township = rest;
                            }

                            GuardianNrcState = state;
                            GuardianNrcTownship = township;
                            GuardianNrcType = type;
                            GuardianNrcNumber = ToMyanmarDigits(number ?? "");

                            FatherNrcState = state;
                            FatherNrcTownship = township;
                            FatherNrcType = type;
                            FatherNrcNumber = ToMyanmarDigits(number ?? "");

                            if (!string.IsNullOrEmpty(state) && NrcTownshipsByState.ContainsKey(state))
                            {
                                var list = NrcTownshipsByState[state];
                                GuardianTownshipList = list;
                                FatherTownshipList = list;
                            }
                        }
                    }

                    Console.WriteLine("Loaded existing personal info successfully.");
                }
                else
                {
                    isUpdate = false;
                    if (userId > 0)
                    {
                        await AutoFillFromPreviousRegistration(userId);
                    }
                }
            }
            catch (Exception ex)
            {
                isUpdate = false;
                Console.WriteLine($"Error loading existing personal info: {ex.Message}");
                if (userId > 0)
                {
                    await AutoFillFromPreviousRegistration(userId);
                }
            }
        }

        private static string NormalizeRegistrationStatus(string? status)
        {
            return string.Equals(status, LegacyPendingStatus, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(status)
                ? PendingConfirmationStatus
                : status;
        }

        private bool IsApproved => string.Equals(RegistrationReviewStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase);
        private bool IsRejected => string.Equals(RegistrationReviewStatus, RejectedStatus, StringComparison.OrdinalIgnoreCase);

        private string GetReviewStatusClass()
        {
            if (IsApproved) return "approved";
            if (IsRejected) return "rejected";
            return "pending";
        }

        private string GetReviewStatusMessage()
        {
            if (IsApproved)
            {
                return "ကျောင်းဘက်မှ အချက်အလက်များအား အတည်ပြုပြီးပါပြီ။ ငွေပေးချေမှုအဆင့်သို့ ဆက်လက်လုပ်ဆောင်နိုင်ပါသည်။";
            }

            if (IsRejected)
            {
                return "ကျောင်းဘက်မှ ဤကျောင်းအပ်နှံမှု အချက်အလက်အား ပယ်ချထားပါသည်။ ကျေးဇူးပြု၍ အချက်အလက်များကို ပြန်လည်ပြင်ဆင်ပြီး အသစ်ပြန်လည် တင်သွင်းပေးပါ။";
            }

            return "တင်သွင်းထားသော အချက်အလက်များကို ကျောင်းဘက်မှ စိစစ်နေဆဲဖြစ်ပါသည်။ အတည်ပြုပြီးမှသာ ငွေပေးချေနိုင်ပါမည်။";
        }

        private void ApplyRegistrationStatus(int registrationId, int? userId, string? status, bool canProceedToPayment)
        {
            SubmittedRegistrationId = registrationId;
            SubmittedUserId = userId;
            RegistrationReviewStatus = NormalizeRegistrationStatus(status);
            CanProceedToPayment = canProceedToPayment || IsApproved;
            // Note: Personal info page should always show the personal info form, not block with registration panel
            ShowRegistrationStatusPanel = false;

            if (registrationId > 0)
            {
                StudentRegState.SetRegistrationIds(registrationId, userId ?? (requestModel.UserId ?? 0));
            }
        }

        private void ApplyRegistrationResponseData(object? data)
        {
            if (data == null) return;

            var jObj = data as Newtonsoft.Json.Linq.JObject ?? Newtonsoft.Json.Linq.JObject.FromObject(data);
            var registrationId = jObj.Value<int?>("id")
                ?? jObj.Value<int?>("registrationId")
                ?? jObj.Value<int?>("RegistrationId")
                ?? 0;
            var userId = jObj.Value<int?>("userId")
                ?? jObj.Value<int?>("UserId")
                ?? requestModel.UserId;
            var status = jObj.Value<string>("status")
                ?? jObj.Value<string>("Status")
                ?? PendingConfirmationStatus;
            var canProceedToPayment = jObj.Value<bool?>("canProceedToPayment")
                ?? jObj.Value<bool?>("CanProceedToPayment")
                ?? false;

            ApplyRegistrationStatus(registrationId, userId, status, canProceedToPayment);
        }

        private async Task RefreshRegistrationStatus()
        {
            if (SubmittedRegistrationId <= 0) return;

            IsRefreshingStatus = true;
            try
            {
                var regData = await HttpClientService.ExecuteAsync<Newtonsoft.Json.Linq.JObject>(
                    $"StudentRegistrations/{SubmittedRegistrationId}",
                    EnumHttpMethod.Get);

                if (regData != null)
                {
                    var status = regData.Value<string>("status") ?? regData.Value<string>("Status");
                    var canProceedToPayment = regData.Value<bool?>("canProceedToPayment")
                        ?? regData.Value<bool?>("CanProceedToPayment")
                        ?? false;

                    ApplyRegistrationStatus(SubmittedRegistrationId, SubmittedUserId ?? requestModel.UserId, status, canProceedToPayment);
                }
            }
            finally
            {
                IsRefreshingStatus = false;
            }
        }
        private ElementReference studentNameEnInput;

        private bool isUpdate = false;
        public bool IsFormDisabled { get; set; } = false;

        private string CurrentNrcSelectionType = "";
        private bool NrcSelectorVisible = false;

        private async Task ShowNrcSelectDialog(string type)
        {
            CurrentNrcSelectionType = type;
            NrcSelectorVisible = true;
            StateHasChanged();
            await Task.CompletedTask;
        }

        private async Task HandleRegistrationStatusChanged(StudentRegistrationStatusChangedEventArgs args)
        {
            if (args.RegistrationId != SubmittedRegistrationId && args.UserId != SubmittedUserId)
            {
                return;
            }

            await InvokeAsync(() =>
            {
                ApplyRegistrationStatus(args.RegistrationId, args.UserId, args.Status, string.Equals(args.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase));
                StateHasChanged();
            });
        }

        private void ContinueToPayment()
        {
            if (CanProceedToPayment && SubmittedRegistrationId > 0)
            {
                Nav.NavigateTo($"/student/payment?regId={SubmittedRegistrationId}");
            }
        }

        private void StartCorrectedRegistration()
        {
            ShowRegistrationStatusPanel = false;
            RegistrationReviewStatus = "";
            CanProceedToPayment = false;
            SubmittedRegistrationId = 0;
            SubmittedUserId = requestModel.UserId;
            CurrentStep = 1;
            StudentRegState.Clear();
        }

        public void Dispose()
        {
            NotifierService.OnRegistrationStatusChanged -= HandleRegistrationStatusChanged;
        }

        private void LoadDefaultSemesters()
        {
            SemesterList = new List<SemesterModel>
            {
                new SemesterModel { SemesterId = 1, SemesterName = "Semester I",   Sequence = 1 },
                new SemesterModel { SemesterId = 2, SemesterName = "Semester II",  Sequence = 2 },
                new SemesterModel { SemesterId = 3, SemesterName = "Semester III", Sequence = 3 },
                new SemesterModel { SemesterId = 4, SemesterName = "Semester IV",  Sequence = 4 },
                new SemesterModel { SemesterId = 5, SemesterName = "Semester V",   Sequence = 5 },
                new SemesterModel { SemesterId = 6, SemesterName = "Semester VI",  Sequence = 6 },
                new SemesterModel { SemesterId = 7, SemesterName = "Semester VII", Sequence = 7 },
                new SemesterModel { SemesterId = 8, SemesterName = "Semester VIII",Sequence = 8 },
                new SemesterModel { SemesterId = 9, SemesterName = "Semester IX",  Sequence = 9 }
            };
        }

        // ---- Compute the allowed semester number from the student's result history ----
        private void ComputeAllowedSemester()
        {
            string? GetSemName(int seq) =>
                SemesterList.FirstOrDefault(s => s.Sequence == seq)?.SemesterName
                ?? SemesterList.OrderBy(s => s.Sequence).FirstOrDefault()?.SemesterName;

            if (LoggedInStudent == null)
            {
                AllowedSemesterSequence = 1;
                AllowedSemesterName = GetSemName(1);
                requestModel.academic_year_level = AllowedSemesterName;
                return;
            }

            var semResults = new string?[]
            {
                LoggedInStudent.Sem1_Result, LoggedInStudent.Sem2_Result, LoggedInStudent.Sem3_Result,
                LoggedInStudent.Sem4_Result, LoggedInStudent.Sem5_Result, LoggedInStudent.Sem6_Result,
                LoggedInStudent.Sem7_Result, LoggedInStudent.Sem8_Result, LoggedInStudent.Sem9_Result
            };

            int highestPassed = 0;
            int? firstFailedSeq = null;

            for (int i = 0; i < semResults.Length; i++)
            {
                int seq = i + 1;
                if (string.Equals(semResults[i], "Pass", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(semResults[i], "Credit_Transferred", StringComparison.OrdinalIgnoreCase))
                {
                    highestPassed = seq;
                }
                else if (string.Equals(semResults[i], "Fail", StringComparison.OrdinalIgnoreCase))
                {
                    if (firstFailedSeq == null) firstFailedSeq = seq;
                }
            }

            string studentMajor = (LoggedInStudent?.CurrentMajor ?? LoggedInStudent?.FacultyName ?? "").ToLowerInvariant();
            bool isEngineering = studentMajor.Contains("civil") ||
                                 studentMajor.Contains("electronic") ||
                                 studentMajor.Contains("electrical") ||
                                 studentMajor.Contains("mechanical") ||
                                 studentMajor.Contains("engineering");
            TotalRequiredSemesters = isEngineering ? 9 : 8;

            if (highestPassed >= TotalRequiredSemesters && firstFailedSeq == null)
            {
                bool isGradStatus = string.Equals(LoggedInStudent?.Status, "Graduated", StringComparison.OrdinalIgnoreCase);
                if (isGradStatus)
                {
                    IsGraduated = true;
                    AllowedSemesterSequence = TotalRequiredSemesters;
                    AllowedSemesterName = GetSemName(TotalRequiredSemesters);
                    requestModel.academic_year_level = AllowedSemesterName;
                    if (!IsAdminView) return;
                }
                else
                {
                    AllowedSemesterSequence = TotalRequiredSemesters;
                    AllowedSemesterName = GetSemName(TotalRequiredSemesters);
                    requestModel.academic_year_level = AllowedSemesterName;
                    if (!IsAdminView) return;
                }
            }

            AllowedSemesterSequence = firstFailedSeq ?? Math.Min(highestPassed + 1, TotalRequiredSemesters);
            AllowedSemesterName     = GetSemName(AllowedSemesterSequence);
            requestModel.academic_year_level = AllowedSemesterName;

            // Auto-fill "ဖြေဆိုခဲ့သောစာမေးပွဲ" with the last PASSED semester from student directory
            if (highestPassed > 0)
            {
                var lastPassedSemName = SemesterList.FirstOrDefault(s => s.Sequence == highestPassed)?.SemesterName;
                if (!string.IsNullOrEmpty(lastPassedSemName))
                    PastExamSemester = lastPassedSemName;

                // Auto-fill "အောင် / က်" status for that semester
                if (firstFailedSeq.HasValue && firstFailedSeq.Value <= highestPassed)
                    requestModel.past_exam_status = "Fail";
                else
                    requestModel.past_exam_status = "Pass";
            }
            else
            {
                // New student or failed Semester 1 (highestPassed == 0) -> Past exam is high school matriculation exam
                PastExamSemester = "တက္ကသိုလ်ဝင်စာမေးပွဲ";
                requestModel.past_exam_major = "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဇီဝ";
                if (string.IsNullOrEmpty(requestModel.major) || requestModel.major == "-")
                {
                    requestModel.major = "Information Technology";
                }
                requestModel.past_exam_status = "Pass";
            }
        }

        // ---- Auto-fill personal data from previous registration ----
        private async Task AutoFillFromPreviousRegistration(int userId)
        {
            try
            {
                var prev = await HttpClientService.ExecuteAsync<PreviousRegistrationModel>(
                    $"StudentRegistrations/latest/{userId}", EnumHttpMethod.Get);

                if (prev == null) return;

                // Personal info (safe to auto-fill — semester is handled separately)
                requestModel.student_name_mm      = prev.StudentNameMm;
                requestModel.student_name_en      = prev.StudentNameEn;
                requestModel.mother_name          = prev.MotherName;
                requestModel.father_name          = prev.FatherName;
                requestModel.gender_relation      = prev.GenderRelation;
                requestModel.ethnicity            = prev.Ethnicity;
                requestModel.religion             = prev.Religion;
                requestModel.pob                  = prev.Pob;
                requestModel.birth_place_region   = prev.BirthPlaceRegion;
                requestModel.nationality_status   = prev.NationalityStatus;
                requestModel.email                = prev.Email;
                requestModel.blood_type           = prev.BloodType;
                requestModel.current_address      = prev.CurrentAddress;
                requestModel.permanent_address_mm = prev.PermanentAddressMm;
                requestModel.permanent_address_en = prev.PermanentAddressEn;
                requestModel.matric_roll_no       = prev.MatricRollNo;
                requestModel.matric_passed_year   = prev.MatricPassedYear;
                requestModel.exam_center          = prev.ExamCenter;
                requestModel.father_occupation    = prev.FatherOccupation;
                requestModel.mother_occupation    = prev.MotherOccupation;
                requestModel.covid_vaccine_status = prev.CovidVaccineStatus;
                requestModel.guardian_name        = prev.GuardianName;
                requestModel.guardian_relationship= prev.GuardianRelationship;
                requestModel.guardian_occupation  = prev.GuardianOccupation;
                requestModel.guardian_address_phone = prev.GuardianAddressPhone;
                requestModel.app_guardian_name    = prev.AppGuardianName;
                requestModel.app_guardian_nrc     = prev.AppGuardianNrc;
                requestModel.app_guardian_phone   = prev.AppGuardianPhone;
                requestModel.app_guardian_address = prev.AppGuardianAddress;
                requestModel.app_student_name     = prev.AppStudentName;
                requestModel.app_student_phone    = prev.AppStudentPhone;
                requestModel.stipend_requested    = prev.StipendRequested;
                requestModel.university_reg_no    = prev.UniversityRegNo;
                requestModel.AdmissionSerialNo    = prev.AdmissionSerialNo;
                if (prev.AdmissionYear.HasValue)
                    requestModel.admission_year = prev.AdmissionYear.Value;

                // Past exam fields
                if (!string.IsNullOrEmpty(prev.PastExamMajor))
                    requestModel.past_exam_major = prev.PastExamMajor;
                if (!string.IsNullOrEmpty(prev.PastExamRollNo))
                    requestModel.past_exam_roll_no = prev.PastExamRollNo;
                if (prev.PastExamYear.HasValue)
                {
                    requestModel.past_exam_year = prev.PastExamYear.Value;
                    PastExamDate = new DateTime(prev.PastExamYear.Value, 1, 1);
                }
                if (!string.IsNullOrEmpty(prev.PastExamStatus))
                    requestModel.past_exam_status = prev.PastExamStatus;

                // အထူးပြူဘာသာ အမာစာ auto-fill (semester locked separately in ComputeAllowedSemester)
                if (!string.IsNullOrEmpty(prev.Major))
                {
                    requestModel.major = prev.Major;
                    AutoSelectFacultyForMajor(prev.Major);
                }

                if (prev.Dob.HasValue)
                    DobDate = prev.Dob.Value.ToDateTime(TimeOnly.MinValue);

                // COVID vaccine date — parse stored "dd-MM-yyyy" string back into CovidDate
                if (!string.IsNullOrEmpty(prev.CovidVaccineStatus) && prev.CovidVaccineStatus != "-")
                {
                    if (DateTime.TryParseExact(prev.CovidVaccineStatus, "dd-MM-yyyy",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out var covidParsed))
                    {
                        CovidDate = covidParsed;
                    }
                    else if (DateTime.TryParse(prev.CovidVaccineStatus, out var covidParsed2))
                    {
                        CovidDate = covidParsed2;
                    }
                }

                // NRC components (student)
                if (!string.IsNullOrEmpty(prev.NrcState))
                {
                    requestModel.nrc_state    = prev.NrcState;
                    requestModel.nrc_township = prev.NrcTownship;
                    NrcType               = prev.NrcType ?? "(နိုင်)";
                    requestModel.nrc_number   = ToMyanmarDigits(prev.NrcNumber ?? "");

                    if (!string.IsNullOrEmpty(prev.NrcState) && NrcTownshipsByState.ContainsKey(prev.NrcState))
                        CurrentTownshipList = NrcTownshipsByState[prev.NrcState];
                }

                // Guardian & Father NRC — parse stored "state/township(type)number" back into component fields (use app_guardian_nrc for both)
                if (!string.IsNullOrEmpty(prev.AppGuardianNrc) && prev.AppGuardianNrc != "-")
                {
                    var gNrcRaw = prev.AppGuardianNrc;
                    var slashIdx = gNrcRaw.IndexOf('/');
                    if (slashIdx > 0)
                    {
                        var state = gNrcRaw[..slashIdx];
                        var rest = gNrcRaw[(slashIdx + 1)..];
                        string? township = null;
                        string type = "(နိုင်)";
                        string? number = null;

                        var openParen  = rest.IndexOf('(');
                        var closeParen = rest.IndexOf(')');
                        if (openParen >= 0 && closeParen > openParen)
                        {
                            township = rest[..openParen];
                            type     = rest[openParen..(closeParen + 1)];
                            number   = rest[(closeParen + 1)..];
                        }
                        else
                        {
                            township = rest;
                        }

                        GuardianNrcState = state;
                        GuardianNrcTownship = township;
                        GuardianNrcType = type;
                        GuardianNrcNumber = ToMyanmarDigits(number ?? "");

                        FatherNrcState = state;
                        FatherNrcTownship = township;
                        FatherNrcType = type;
                        FatherNrcNumber = ToMyanmarDigits(number ?? "");

                        if (!string.IsNullOrEmpty(state) && NrcTownshipsByState.ContainsKey(state))
                        {
                            var list = NrcTownshipsByState[state];
                            GuardianTownshipList = list;
                            FatherTownshipList = list;
                        }
                    }
                }

                Console.WriteLine($"Auto-filled registration data from previous registration #{prev.RegistrationId}");
            }
            catch (Exception ex)
            {
                // It's OK if the student has no previous registration
                Console.WriteLine($"No previous registration found (or error): {ex.Message}");
            }
        }

        public int GetSemesterNumberFromName(string? name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            var lower = name.ToLower();
            if (lower.Contains("first") || lower.Contains("sem 1") || lower.Contains("semester 1") || lower.Contains("1st") || lower.Contains("one") || lower.Contains("sem i") || lower.Contains("semester i") || lower.EndsWith(" i")) return 1;
            if (lower.Contains("second") || lower.Contains("sem 2") || lower.Contains("semester 2") || lower.Contains("2nd") || lower.Contains("two") || lower.Contains("sem ii") || lower.Contains("semester ii") || lower.EndsWith(" ii")) return 2;
            if (lower.Contains("third") || lower.Contains("sem 3") || lower.Contains("semester 3") || lower.Contains("3rd") || lower.Contains("three") || lower.Contains("sem iii") || lower.Contains("semester iii") || lower.EndsWith(" iii")) return 3;
            if (lower.Contains("fourth") || lower.Contains("sem 4") || lower.Contains("semester 4") || lower.Contains("4th") || lower.Contains("four") || lower.Contains("sem iv") || lower.Contains("semester iv") || lower.EndsWith(" iv")) return 4;
            if (lower.Contains("fifth") || lower.Contains("sem 5") || lower.Contains("semester 5") || lower.Contains("5th") || lower.Contains("five") || lower.Contains("sem v") || lower.Contains("semester v") || lower.EndsWith(" v")) return 5;
            if (lower.Contains("sixth") || lower.Contains("sem 6") || lower.Contains("semester 6") || lower.Contains("6th") || lower.Contains("six") || lower.Contains("sem vi") || lower.Contains("semester vi") || lower.EndsWith(" vi")) return 6;
            if (lower.Contains("seventh") || lower.Contains("sem 7") || lower.Contains("semester 7") || lower.Contains("7th") || lower.Contains("seven") || lower.Contains("sem vii") || lower.Contains("semester vii") || lower.EndsWith(" vii")) return 7;
            if (lower.Contains("eighth") || lower.Contains("sem 8") || lower.Contains("semester 8") || lower.Contains("8th") || lower.Contains("eight") || lower.Contains("sem viii") || lower.Contains("semester viii") || lower.EndsWith(" viii")) return 8;
            if (lower.Contains("ninth") || lower.Contains("sem 9") || lower.Contains("semester 9") || lower.Contains("9th") || lower.Contains("nine") || lower.Contains("sem ix") || lower.Contains("semester ix") || lower.EndsWith(" ix")) return 9;
            return 0;
        }
        public bool IsSemesterAllowed(string? semesterName)
        {
            if (LoggedInStudent == null) return true;

            int targetSemNum = GetSemesterNumberFromName(semesterName);
            if (targetSemNum == 0) return true;

            if (LoggedInStudent.Sem1_Result == "Fail" && targetSemNum > 1) return false;
            if (LoggedInStudent.Sem2_Result == "Fail" && targetSemNum > 2) return false;
            if (LoggedInStudent.Sem3_Result == "Fail" && targetSemNum > 3) return false;
            if (LoggedInStudent.Sem4_Result == "Fail" && targetSemNum > 4) return false;
            if (LoggedInStudent.Sem5_Result == "Fail" && targetSemNum > 5) return false;
            if (LoggedInStudent.Sem6_Result == "Fail" && targetSemNum > 6) return false;
            if (LoggedInStudent.Sem7_Result == "Fail" && targetSemNum > 7) return false;
            if (LoggedInStudent.Sem8_Result == "Fail" && targetSemNum > 8) return false;
            if (LoggedInStudent.Sem9_Result == "Fail" && targetSemNum > 9) return false;

            return true;
        }

        public static string GetEnglishNrcTownship(string mmTownship)
        {
            if (string.IsNullOrEmpty(mmTownship)) return "";
            
            var sb = new System.Text.StringBuilder();
            foreach (char c in mmTownship)
            {
                switch (c)
                {
                    case 'က': sb.Append("Ka"); break;
                    case 'ခ': sb.Append("Kha"); break;
                    case 'ဂ': sb.Append("Ga"); break;
                    case 'ဃ': sb.Append("Gha"); break;
                    case 'င': sb.Append("Nga"); break;
                    case 'စ': sb.Append("Sa"); break;
                    case 'ဆ': sb.Append("Sa"); break;
                    case 'ဇ': sb.Append("Za"); break;
                    case 'ဈ': sb.Append("Zha"); break;
                    case 'ည': sb.Append("Nya"); break;
                    case 'ဋ': sb.Append("Ta"); break;
                    case 'ဌ': sb.Append("Hta"); break;
                    case 'ဍ': sb.Append("Da"); break;
                    case 'ဎ': sb.Append("Dha"); break;
                    case 'ဏ': sb.Append("Na"); break;
                    case 'တ': sb.Append("Ta"); break;
                    case 'ထ': sb.Append("Hta"); break;
                    case 'ဒ': sb.Append("Da"); break;
                    case 'ဓ': sb.Append("Dha"); break;
                    case 'န': sb.Append("Na"); break;
                    case 'ပ': sb.Append("Pa"); break;
                    case 'ဖ': sb.Append("Pha"); break;
                    case 'ဗ': sb.Append("Ba"); break;
                    case 'ဘ': sb.Append("Ba"); break;
                    case 'မ': sb.Append("Ma"); break;
                    case 'ယ': sb.Append("Ya"); break;
                    case 'ရ': sb.Append("Ya"); break;
                    case 'လ': sb.Append("La"); break;
                    case 'ဝ': sb.Append("Wa"); break;
                    case 'သ': sb.Append("Tha"); break;
                    case 'ဟ': sb.Append("Ha"); break;
                    case 'ဠ': sb.Append("La"); break;
                    case 'အ': sb.Append("Ah"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString().ToUpper();
        }

        public static string ToMyanmarDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                if (c >= '0' && c <= '9')
                {
                    sb.Append((char)('၀' + (c - '0')));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string ToEnglishDigits(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                if (c >= '၀' && c <= '၉')
                {
                    sb.Append((char)('0' + (c - '၀')));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public void OnNrcStateChanged(ChangeEventArgs e)
        {
            requestModel.nrc_state = e.Value?.ToString();
            requestModel.nrc_township = "";

            if (!string.IsNullOrEmpty(requestModel.nrc_state) && NrcTownshipsByState.ContainsKey(requestModel.nrc_state))
                CurrentTownshipList = NrcTownshipsByState[requestModel.nrc_state];
            else
                CurrentTownshipList = new List<string>();
        }

        public void OnGuardianNrcStateChanged(ChangeEventArgs e)
        {
            GuardianNrcState = e.Value?.ToString();
            GuardianNrcTownship = "";

            if (!string.IsNullOrEmpty(GuardianNrcState) && NrcTownshipsByState.ContainsKey(GuardianNrcState))
                GuardianTownshipList = NrcTownshipsByState[GuardianNrcState];
            else
                GuardianTownshipList = new List<string>();
        }

        public void OnFatherNrcStateChanged(ChangeEventArgs e)
        {
            FatherNrcState = e.Value?.ToString();
            FatherNrcTownship = "";

            if (!string.IsNullOrEmpty(FatherNrcState) && NrcTownshipsByState.ContainsKey(FatherNrcState))
                FatherTownshipList = NrcTownshipsByState[FatherNrcState];
            else
                FatherTownshipList = new List<string>();
        }

        private async Task OnPhotoSelected(InputFileChangeEventArgs e)
        {
            SelectedPhotoFile = e.File;
            if (SelectedPhotoFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedPhotoFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedPhotoBytes = ms.ToArray();
                PreviewImageUrl = $"data:{SelectedPhotoFile.ContentType};base64,{Convert.ToBase64String(SelectedPhotoBytes)}";
                requestModel.student_image = PreviewImageUrl;
                StateHasChanged();
            }
        }

        private void RemovePhoto()
        {
            SelectedPhotoFile = null;
            SelectedPhotoBytes = null;
            PreviewImageUrl = null;
            requestModel.student_image = null;
        }

        private async Task OnNrcFrontSelected(InputFileChangeEventArgs e)
        {
            SelectedNrcFrontFile = e.File;
            if (SelectedNrcFrontFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedNrcFrontFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedNrcFrontBytes = ms.ToArray();
                PreviewNrcFrontUrl = $"data:{SelectedNrcFrontFile.ContentType};base64,{Convert.ToBase64String(SelectedNrcFrontBytes)}";
                requestModel.nrc_front_image = PreviewNrcFrontUrl;
                StateHasChanged();
            }
        }

        private void RemoveNrcFront()
        {
            SelectedNrcFrontFile = null;
            SelectedNrcFrontBytes = null;
            PreviewNrcFrontUrl = null;
            requestModel.nrc_front_image = null;
        }

        private async Task OnNrcBackSelected(InputFileChangeEventArgs e)
        {
            SelectedNrcBackFile = e.File;
            if (SelectedNrcBackFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedNrcBackFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedNrcBackBytes = ms.ToArray();
                PreviewNrcBackUrl = $"data:{SelectedNrcBackFile.ContentType};base64,{Convert.ToBase64String(SelectedNrcBackBytes)}";
                requestModel.nrc_back_image = PreviewNrcBackUrl;
                StateHasChanged();
            }
        }

        private void RemoveNrcBack()
        {
            SelectedNrcBackFile = null;
            SelectedNrcBackBytes = null;
            PreviewNrcBackUrl = null;
            requestModel.nrc_back_image = null;
        }

        private async Task OnCensusSelected(InputFileChangeEventArgs e)
        {
            SelectedCensusFile = e.File;
            if (SelectedCensusFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedCensusFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedCensusBytes = ms.ToArray();
                PreviewCensusUrl = $"data:{SelectedCensusFile.ContentType};base64,{Convert.ToBase64String(SelectedCensusBytes)}";
                requestModel.census_image = PreviewCensusUrl;
                StateHasChanged();
            }
        }

        private void RemoveCensus()
        {
            SelectedCensusFile = null;
            SelectedCensusBytes = null;
            PreviewCensusUrl = null;
            requestModel.census_image = null;
        }

        // --- Parent NRC Image Handlers ---

        private async Task OnFatherNrcFrontSelected(InputFileChangeEventArgs e)
        {
            SelectedFatherNrcFrontFile = e.File;
            if (SelectedFatherNrcFrontFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedFatherNrcFrontFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedFatherNrcFrontBytes = ms.ToArray();
                PreviewFatherNrcFrontUrl = $"data:{SelectedFatherNrcFrontFile.ContentType};base64,{Convert.ToBase64String(SelectedFatherNrcFrontBytes)}";
                requestModel.father_nrc_front_image = PreviewFatherNrcFrontUrl;
                StateHasChanged();
            }
        }

        private void RemoveFatherNrcFront()
        {
            SelectedFatherNrcFrontFile = null;
            SelectedFatherNrcFrontBytes = null;
            PreviewFatherNrcFrontUrl = null;
            requestModel.father_nrc_front_image = null;
        }

        private async Task OnFatherNrcBackSelected(InputFileChangeEventArgs e)
        {
            SelectedFatherNrcBackFile = e.File;
            if (SelectedFatherNrcBackFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedFatherNrcBackFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedFatherNrcBackBytes = ms.ToArray();
                PreviewFatherNrcBackUrl = $"data:{SelectedFatherNrcBackFile.ContentType};base64,{Convert.ToBase64String(SelectedFatherNrcBackBytes)}";
                requestModel.father_nrc_back_image = PreviewFatherNrcBackUrl;
                StateHasChanged();
            }
        }

        private void RemoveFatherNrcBack()
        {
            SelectedFatherNrcBackFile = null;
            SelectedFatherNrcBackBytes = null;
            PreviewFatherNrcBackUrl = null;
            requestModel.father_nrc_back_image = null;
        }

        private async Task OnMotherNrcFrontSelected(InputFileChangeEventArgs e)
        {
            SelectedMotherNrcFrontFile = e.File;
            if (SelectedMotherNrcFrontFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedMotherNrcFrontFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedMotherNrcFrontBytes = ms.ToArray();
                PreviewMotherNrcFrontUrl = $"data:{SelectedMotherNrcFrontFile.ContentType};base64,{Convert.ToBase64String(SelectedMotherNrcFrontBytes)}";
                requestModel.mother_nrc_front_image = PreviewMotherNrcFrontUrl;
                StateHasChanged();
            }
        }

        private void RemoveMotherNrcFront()
        {
            SelectedMotherNrcFrontFile = null;
            SelectedMotherNrcFrontBytes = null;
            PreviewMotherNrcFrontUrl = null;
            requestModel.mother_nrc_front_image = null;
        }

        private async Task OnMotherNrcBackSelected(InputFileChangeEventArgs e)
        {
            SelectedMotherNrcBackFile = e.File;
            if (SelectedMotherNrcBackFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedMotherNrcBackFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                SelectedMotherNrcBackBytes = ms.ToArray();
                PreviewMotherNrcBackUrl = $"data:{SelectedMotherNrcBackFile.ContentType};base64,{Convert.ToBase64String(SelectedMotherNrcBackBytes)}";
                requestModel.mother_nrc_back_image = PreviewMotherNrcBackUrl;
                StateHasChanged();
            }
        }

        private void RemoveMotherNrcBack()
        {
            SelectedMotherNrcBackFile = null;
            SelectedMotherNrcBackBytes = null;
            PreviewMotherNrcBackUrl = null;
            requestModel.mother_nrc_back_image = null;
        }

        public string GetImageUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return path;
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return path;

            var baseUrl = Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5077";
            return $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        }

        private void NextStep()
        {
            if (CurrentStep == 1)
            {
                // 💡 User ID ကို Token မှ ဆွဲမရခဲ့ပါက API Error မတက်ခင် ဤနေရာတွင် တားပေးမည်
                if ((requestModel.UserId == null || requestModel.UserId <= 0) && (requestModel.NewStudentAccId == null || requestModel.NewStudentAccId <= 0))
                {
                    ShowError("စနစ်အတွင်း User ID သို့မဟုတ် New Student ID အား ရှာမတွေ့ပါ။ ကျေးဇူးပြု၍ Logout ထွက်ပြီး Login အသစ်ပြန်ဝင်ပေးပါ။");
                    return;
                }

                if (string.IsNullOrWhiteSpace(requestModel.student_name_mm) ||
                    string.IsNullOrWhiteSpace(requestModel.app_student_phone) ||
                    string.IsNullOrWhiteSpace(requestModel.nrc_state) ||
                    string.IsNullOrWhiteSpace(requestModel.nrc_township) ||
                    string.IsNullOrWhiteSpace(requestModel.nrc_number) ||
                    !DobDate.HasValue)
                {
                    ShowError("ကျေးဇူးပြု၍ မရှိမဖြစ်လိုအပ်သော အချက်အလက်များ (*) ကို အပြည့်အစုံ ဖြည့်စွက်ပါ။");
                    return;
                }

                // 🎂 Minimum Age Requirement: At least 16 years old
                if (DobDate.HasValue)
                {
                    var today = DateTime.Today;
                    var age = today.Year - DobDate.Value.Year;
                    if (DobDate.Value.Date > today.AddYears(-age)) age--;

                    if (age < 16)
                    {
                        ShowError("ကျောင်းအပ်နှံရန် အနည်းဆုံး အသက် (၁၆) နှစ် ပြည့်ရပါမည်။ (Minimum age requirement is 16 years old)");
                        return;
                    }
                }
            }
            else if (CurrentStep == 2)
            {
                if (string.IsNullOrWhiteSpace(requestModel.permanent_address_mm) ||
                    string.IsNullOrWhiteSpace(requestModel.academic_year_range) ||
                    string.IsNullOrWhiteSpace(requestModel.academic_year_level))
                {
                    ShowError("ကျေးဇူးပြု၍ မရှိမဖြစ်လိုအပ်သော အချက်အလက်များ (*) ကို အပြည့်အစုံ ဖြည့်စွက်ပါ။");
                    return;
                }

                // Auto-fill Step 3 matriculation table fields from Step 2
                requestModel.past_exam_major = requestModel.major;
                requestModel.past_exam_roll_no = requestModel.roll_no;
                if (!string.IsNullOrEmpty(requestModel.academic_year_range))
                {
                    var parts = requestModel.academic_year_range.Split('-');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int yr))
                    {
                        requestModel.past_exam_year = yr;
                        PastExamDate = new DateTime(yr, 1, 1);
                    }
                }
            }
            else if (CurrentStep == 3)
            {
                if (string.IsNullOrWhiteSpace(requestModel.father_name) ||
                    string.IsNullOrWhiteSpace(requestModel.guardian_name) ||
                    string.IsNullOrWhiteSpace(requestModel.guardian_address_phone))
                {
                    ShowError("ကျေးဇူးပြု၍ မရှိမဖြစ်လိုအပ်သော အချက်အလက်များ (*) ကို အပြည့်အစုံ ဖြည့်စွက်ပါ။");
                    return;
                }
            }

            if (CurrentStep < TotalSteps)
            {
                if (CurrentStep == 3)
                {
                    requestModel.app_student_name = requestModel.student_name_mm;
                    requestModel.app_guardian_name = requestModel.guardian_name;
                    requestModel.app_guardian_phone = requestModel.guardian_address_phone;
                    requestModel.current_address = requestModel.permanent_address_mm;

                    // Automatically copy Father's NRC to Guardian's NRC variables if they are empty
                    if (string.IsNullOrEmpty(GuardianNrcState))
                    {
                        GuardianNrcState = FatherNrcState;
                        GuardianNrcTownship = FatherNrcTownship;
                        GuardianNrcType = FatherNrcType;
                        GuardianNrcNumber = FatherNrcNumber;
                        if (!string.IsNullOrEmpty(GuardianNrcState) && NrcTownshipsByState.ContainsKey(GuardianNrcState))
                        {
                            GuardianTownshipList = NrcTownshipsByState[GuardianNrcState];
                        }
                    }

                    if (!string.IsNullOrEmpty(GuardianNrcState) && !string.IsNullOrEmpty(GuardianNrcTownship) && !string.IsNullOrEmpty(GuardianNrcNumber))
                    {
                        requestModel.app_guardian_nrc = $"{GuardianNrcState}/{GuardianNrcTownship}{GuardianNrcType}{ToEnglishDigits(GuardianNrcNumber)}";
                    }
                }
                CurrentStep++;
            }
        }

        private void PrevStep() { if (CurrentStep > 1) CurrentStep--; }

        private void ShowError(string message)
        {
            IsSuccessModal = false;
            ModalMessage = message;
            ShowModal = true;
        }


        public void CloseModal()
        {
            ShowModal = false;
            StateHasChanged();
        }

        public void EnableEditing()
        {
            IsFormDisabled = false;
            StateHasChanged();
        }

        public void CancelEditing()
        {
            IsFormDisabled = true;
            StateHasChanged();
        }

        [Inject] public StudentRegistrationState StudentRegState { get; set; } = null!;
        
        private async Task SavePersonalInfoForm()
        {
            // Only allow submission if not in admin view
            if (IsAdminView) return;

            IsSubmitting = true;

            requestModel.FacultyId = SelectedFacultyId;
            requestModel.dob = DobDate ?? DateTime.Now;
            requestModel.covid_vaccine_status = CovidDate?.ToString("dd-MM-yyyy") ?? "-";

            if (PastExamDate.HasValue)
            {
                requestModel.past_exam_year = PastExamDate.Value.Year;
            }
            requestModel.previous_year_roll_no = PastExamSemester;

            // Automatically copy Father's NRC to Guardian's NRC variables if they are empty
            if (string.IsNullOrEmpty(GuardianNrcState))
            {
                GuardianNrcState = FatherNrcState;
                GuardianNrcTownship = FatherNrcTownship;
                GuardianNrcType = FatherNrcType;
                GuardianNrcNumber = FatherNrcNumber;
            }

            string studentNrcNumberEng = ToEnglishDigits(requestModel.nrc_number ?? "");
            string guardianNrcNumberEng = ToEnglishDigits(GuardianNrcNumber ?? "");

            if (!string.IsNullOrEmpty(requestModel.nrc_state) && !string.IsNullOrEmpty(requestModel.nrc_township) && !string.IsNullOrEmpty(requestModel.nrc_number))
            {
                requestModel.nrc_type = NrcType;
                requestModel.student_nrc_no = $"{requestModel.nrc_state}/{requestModel.nrc_township}{NrcType}{studentNrcNumberEng}";
            }
            else
                requestModel.student_nrc_no = "-";

            if (!string.IsNullOrEmpty(GuardianNrcState) && !string.IsNullOrEmpty(GuardianNrcTownship) && !string.IsNullOrEmpty(GuardianNrcNumber))
                requestModel.app_guardian_nrc = $"{GuardianNrcState}/{GuardianNrcTownship}{GuardianNrcType}{guardianNrcNumberEng}";
            else if (string.IsNullOrEmpty(requestModel.app_guardian_nrc))
                requestModel.app_guardian_nrc = "-";

            requestModel.student_name_mm ??= "-";
            requestModel.student_name_en ??= "-";
            requestModel.permanent_address_mm ??= "-";
            requestModel.permanent_address_en ??= "-";
            requestModel.father_name ??= "-";
            requestModel.mother_name ??= "-";
            requestModel.academic_year_range ??= "-";
            requestModel.academic_year_level ??= "-";
            requestModel.major ??= "-";
            requestModel.matric_roll_no ??= "-";
            requestModel.exam_center ??= "-";
            requestModel.pob ??= "-";
            requestModel.birth_place_region ??= "-";
            requestModel.ethnicity ??= "-";
            requestModel.religion ??= "-";

            try
            {
                ActionResponseModel? response;
                if (isUpdate)
                {
                    if (requestModel.NewStudentAccId.HasValue && requestModel.NewStudentAccId > 0)
                    {
                        response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"studentpersonalinfo/newstudent/{requestModel.NewStudentAccId}", EnumHttpMethod.Put, requestModel);
                    }
                    else
                    {
                        response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"studentpersonalinfo/{requestModel.UserId}", EnumHttpMethod.Put, requestModel);
                    }
                }
                else
                {
                    if (requestModel.NewStudentAccId.HasValue && requestModel.NewStudentAccId > 0)
                    {
                        response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"studentpersonalinfo/newstudent/{requestModel.NewStudentAccId}", EnumHttpMethod.Post, requestModel);
                    }
                    else
                    {
                        response = await HttpClientService.ExecuteAsync<ActionResponseModel>($"studentpersonalinfo/{requestModel.UserId}", EnumHttpMethod.Post, requestModel);
                    }
                }

                if (response?.IsSuccess == true)
                {
                    isUpdate = true;
                    IsFormDisabled = true;  // lock form back to read-only after save
                    if (requestModel.UserId.HasValue && requestModel.UserId > 0)
                    {
                        await LoadStudentPersonalInfo(requestModel.UserId.Value);
                    }
                    else if (requestModel.NewStudentAccId.HasValue && requestModel.NewStudentAccId > 0)
                    {
                        await LoadStudentPersonalInfoForNewStudent(requestModel.NewStudentAccId.Value);
                    }
                    IsSuccessModal = true;
                    ModalMessage = "ကိုယ်ရေးအချက်အလက်များ အောင်မြင်စွာ သိမ်းဆည်းပြီးပါပြီ။";
                    ShowModal = true;
                    StateHasChanged();
                }
                else
                {
                    ShowError(response?.Message ?? "အချက်အလက် သိမ်းဆည်းရာတွင် အမှားဖြစ်ပေါ်နေပါသည်။");
                }
            }
            catch (Exception ex)
            {
                ShowError($"စနစ်ပိုင်းဆိုင်ရာ ချို့ယွင်းချက် ဖြစ်ပေါ်နေပါသည်: {ex.Message}");
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
