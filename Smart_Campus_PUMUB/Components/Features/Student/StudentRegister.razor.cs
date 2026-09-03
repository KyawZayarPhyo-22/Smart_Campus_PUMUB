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
    public partial class StudentRegister : ComponentBase, IDisposable
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public NavigationManager Nav { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
        [Inject] public StudentRegistrationNotifierService NotifierService { get; set; } = null!;
        [Inject] public IConfiguration Configuration { get; set; } = null!;

        private const string PendingConfirmationStatus = "Pending Confirmation";
        private const string LegacyPendingStatus = "Pending";
        private const string ApprovedStatus = "Approved";
        private const string RejectedStatus = "Rejected";

        public StudentRegistrationCreateRequestModel RegModel { get; set; } = new()
        {
            nationality_status = "တိုင်းရင်းသား",
            stipend_requested = false,
            gender_relation = "Male",
            blood_type = "O",
            past_exam_major = "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဇီဝ",
            past_exam_status = "Pass",
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
        public StudentRetakeStatusModel? RetakeStatus { get; set; }

        private void OnMatricRollNoInput(ChangeEventArgs e)
        {
            var raw = e.Value?.ToString() ?? "";
            RegModel.matric_roll_no = System.Text.RegularExpressions.Regex.Replace(raw, @"[^\u1000-\u1049\u104E\u103F\-\/\s]", "");
        }

        //public void CloseModal()
        //{
        //    ShowModal = false;
        //    if (IsSuccessModal) Nav.NavigateTo("/student/payment");
        //}

        public int CurrentStep { get; set; } = 1;
        private const int TotalSteps = 5;
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

        public List<FacultyModel> FacultyList { get; set; } = new();
        public List<MajorModel> MajorList { get; set; } = new();
        public int? SelectedFacultyId { get; set; }

        // --- Subject Grade (Step 3) ---
        public List<SubjectModel> UpcomingSubjects { get; set; } = new();
        public List<SubjectGradeBindingModel> PreviousSubjects { get; set; } = new();
        public List<GradeModel> AllGrades { get; set; } = new();
        public string? PreviousSemesterDisplayName { get; set; }

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
                if (!string.IsNullOrEmpty(RegModel.major) && !FilteredMajors.Contains(RegModel.major, StringComparer.OrdinalIgnoreCase))
                {
                    RegModel.major = null;
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
        public string PastExamSemester { get; set; } = "တက္ကသိုလ်ဝင်စာမေးပွဲ";
        public DateTime? PastExamDate { get; set; }

        public bool IsMatriculationPastExam =>
            string.IsNullOrEmpty(PastExamSemester) ||
            PastExamSemester == "တက္ကသိုလ်ဝင်စာမေးပွဲ";

        private void OnPastExamSemesterChanged()
        {
            if (IsMatriculationPastExam)
            {
                if (string.IsNullOrEmpty(RegModel.past_exam_major) ||
                    (RegModel.past_exam_major != "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဇီဝ" &&
                     RegModel.past_exam_major != "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဘောဂ" &&
                     RegModel.past_exam_major != "မြန်မာ/အင်္ဂလိပ်/ပထဝီ/သမိုင်း/ဘောဂ"))
                {
                    RegModel.past_exam_major = "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဇီဝ";
                }
                RegModel.past_exam_status = "Pass";
                if (string.IsNullOrEmpty(RegModel.past_exam_roll_no) && !string.IsNullOrEmpty(RegModel.matric_roll_no))
                {
                    RegModel.past_exam_roll_no = RegModel.matric_roll_no;
                }
            }
            else
            {
                // University Semester selected (e.g. Semester I, Semester II)
                // Default past_exam_major to the student's selected/current major (e.g. "CST")
                if (!string.IsNullOrEmpty(RegModel.major) && 
                    (string.IsNullOrEmpty(RegModel.past_exam_major) || 
                     RegModel.past_exam_major.Contains("မြန်မာ") || 
                     RegModel.past_exam_major.Contains("အင်္ဂလိပ်")))
                {
                    RegModel.past_exam_major = RegModel.major;
                }

                // If repeating/retaking the same semester, default status to "Fail"
                if (string.Equals(PastExamSemester, RegModel.academic_year_level, StringComparison.OrdinalIgnoreCase))
                {
                    RegModel.past_exam_status = "Fail";
                }
                else
                {
                    RegModel.past_exam_status = "Pass";
                }

                if (string.IsNullOrEmpty(RegModel.past_exam_roll_no) && !string.IsNullOrEmpty(RegModel.roll_no))
                {
                    RegModel.past_exam_roll_no = RegModel.roll_no;
                }
            }
        }

        public string NrcType { get; set; } = "(နိုင်)";
        public List<string> CurrentTownshipList { get; set; } = new();

        // --- Roll No Auto-Fill ---
        private string _rollNoInput = "";
        public string RollNoInput
        {
            get => _rollNoInput;
            set
            {
                _rollNoInput = value;
                RegModel.roll_no = value;
            }
        }
        public bool IsAutoFilling { get; set; } = false;
        public string AutoFillStatus { get; set; } = "";
        public bool AutoFillSuccess { get; set; } = false;

        public async Task HandleRollNoKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await FetchDataByRollNo();
            }
        }

        public async Task OnRollNoChanged(ChangeEventArgs e)
        {
            var val = e.Value?.ToString() ?? "";
            _rollNoInput = val;
            RegModel.roll_no = val;
            await FetchDataByRollNo(val);
        }

        public async Task FetchDataByRollNo(string? rollNo = null)
        {
            var val = rollNo ?? _rollNoInput;
            if (string.IsNullOrWhiteSpace(val))
            {
                AutoFillSuccess = false;
                AutoFillStatus = "⚠ ခုံအမှတ် (Roll No) ရိုက်ထည့်ပါ";
                return;
            }

            _rollNoInput = val;
            RegModel.roll_no = val;

            IsAutoFilling = true;
            AutoFillStatus = "";
            StateHasChanged();

            try
            {
                var info = await HttpClientService.ExecuteAsync<StudentPersonalInfoResponse>($"studentpersonalinfo/by-roll/{Uri.EscapeDataString(val)}", EnumHttpMethod.Get);
                if (info != null)
                {
                    // Ensure RegModel.UserId / NewStudentAccId is populated from current auth state if not yet set
                    if ((!RegModel.UserId.HasValue || RegModel.UserId <= 0) && (!RegModel.NewStudentAccId.HasValue || RegModel.NewStudentAccId <= 0))
                    {
                        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                        var user = authState.User;
                        if (user.Identity?.IsAuthenticated == true)
                        {
                            var userIdStr = user.FindFirst("UserId")?.Value
                                         ?? user.FindFirst("User_Id")?.Value
                                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                         ?? user.FindFirst("id")?.Value
                                         ?? user.FindFirst("uid")?.Value
                                         ?? user.FindFirst("sub")?.Value;
                            if (int.TryParse(userIdStr, out int uId))
                            {
                                RegModel.UserId = uId;
                            }

                            var newAccStr = user.FindFirst("NewStudentAccId")?.Value;
                            if (int.TryParse(newAccStr, out int nId))
                            {
                                RegModel.NewStudentAccId = nId;
                            }
                        }
                    }

                    // ====================================================================
                    // SECURITY CHECK: Prevent using another user's Roll No for Auto-Fill
                    // ====================================================================
                    bool isSameUser = false;
                    if (RegModel.NewStudentAccId.HasValue && RegModel.NewStudentAccId > 0 && info.NewStudentAccId == RegModel.NewStudentAccId)
                    {
                        isSameUser = true;
                    }
                    else if (RegModel.UserId.HasValue && RegModel.UserId > 0 && info.UserId == RegModel.UserId)
                    {
                        isSameUser = true;
                    }

                    if (!isSameUser)
                    {
                        AutoFillSuccess = false;
                        AutoFillStatus = "အခြားကျောင်းသား၏ ခုံအမှတ်ကို အသုံးပြု၍မရပါ။ မိမိ၏ ခုံအမှတ်ကိုသာ အသုံးပြုပါ။";
                        IsAutoFilling = false;
                        
                        ShowModal = true;
                        IsSuccessModal = false;
                        ModalMessage = "အခြားကျောင်းသား၏ ခုံအမှတ်ကို အသုံးပြု၍မရပါ။ မိမိ၏ ခုံအမှတ်ကိုသာ အသုံးပြုပါ။";
                        StateHasChanged();
                        return;
                    }
                    // ====================================================================
                    RegModel.student_name_mm      = info.student_name_mm;
                    RegModel.student_name_en      = info.student_name_en;
                    RegModel.father_name          = info.father_name;
                    RegModel.mother_name          = info.mother_name;
                    RegModel.gender_relation      = info.gender_relation ?? RegModel.gender_relation;
                    RegModel.ethnicity            = info.ethnicity;
                    RegModel.religion             = info.religion;
                    RegModel.pob                  = info.pob;
                    RegModel.birth_place_region   = info.birth_place_region;
                    RegModel.student_nrc_no       = info.student_nrc_no;
                    RegModel.nationality_status   = info.nationality_status ?? RegModel.nationality_status;
                    if (info.dob.HasValue) { RegModel.dob = info.dob.Value; DobDate = info.dob.Value.Date; }
                    RegModel.email                = info.email;
                    RegModel.blood_type           = info.blood_type ?? RegModel.blood_type;
                    RegModel.current_address      = info.current_address;
                    RegModel.permanent_address_mm = info.permanent_address_mm;
                    RegModel.permanent_address_en = info.permanent_address_en;
                    RegModel.matric_roll_no       = info.matric_roll_no;
                    RegModel.matric_passed_year   = info.matric_passed_year;
                    RegModel.exam_center          = info.exam_center;
                    RegModel.father_occupation    = info.father_occupation;
                    RegModel.mother_occupation    = info.mother_occupation;
                    RegModel.guardian_name        = info.guardian_name;
                    RegModel.guardian_relationship= info.guardian_relationship;
                    RegModel.guardian_occupation  = info.guardian_occupation;
                    RegModel.guardian_address_phone = info.guardian_address_phone;
                    RegModel.app_guardian_name    = info.app_guardian_name;
                    RegModel.app_guardian_nrc     = info.app_guardian_nrc;
                    RegModel.app_guardian_phone   = info.app_guardian_phone;
                    RegModel.app_guardian_address = info.app_guardian_address;
                    RegModel.app_student_name     = info.app_student_name;
                    RegModel.app_student_phone    = info.app_student_phone;
                    RegModel.university_reg_no    = info.university_reg_no;
                    if (info.admission_year.HasValue) RegModel.admission_year = info.admission_year;

                    // ── အထူးပြုဘာသာ + Faculty auto-fill (Personal Info မှ) ──
                    if (!string.IsNullOrEmpty(info.major))
                        RegModel.major = info.major;

                    // Faculty dropdown — Personal Info မှ FacultyId ကို ဦးစွာ သုံး၊
                    // မရရှိသေးပါက major name မှ auto-detect
                    if (info.FacultyId.HasValue && info.FacultyId.Value > 0)
                    {
                        SelectedFacultyId = info.FacultyId.Value;
                        UpdateFilteredMajors();
                    }
                    else if (!string.IsNullOrEmpty(info.major))
                    {
                        AutoSelectFacultyForMajor(info.major);
                    }

                    // ── ကိုဗစ်ကာကွယ်ဆေး ပြီးစီးသည့်ရက် auto-fill ──
                    if (!string.IsNullOrEmpty(info.covid_vaccine_status) && info.covid_vaccine_status != "-")
                    {
                        if (DateTime.TryParseExact(info.covid_vaccine_status, "dd-MM-yyyy",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out var covidParsed))
                            CovidDate = covidParsed;
                        else if (DateTime.TryParse(info.covid_vaccine_status, out var covidParsed2))
                            CovidDate = covidParsed2;
                    }

                    // ── Student NRC component fields ──
                    RegModel.nrc_state    = info.nrc_state;
                    RegModel.nrc_township = info.nrc_township;
                    RegModel.nrc_type     = info.nrc_type;
                    RegModel.nrc_number   = info.nrc_number;
                    if (!string.IsNullOrEmpty(info.nrc_type)) NrcType = info.nrc_type;

                    // ── NRC & Census document images auto-fill ──
                    if (!string.IsNullOrEmpty(info.nrc_front_image))
                    {
                        RegModel.nrc_front_image = info.nrc_front_image;
                        PreviewNrcFrontUrl = GetImageUrl(info.nrc_front_image);
                    }
                    if (!string.IsNullOrEmpty(info.nrc_back_image))
                    {
                        RegModel.nrc_back_image = info.nrc_back_image;
                        PreviewNrcBackUrl = GetImageUrl(info.nrc_back_image);
                    }
                    if (!string.IsNullOrEmpty(info.census_image))
                    {
                        RegModel.census_image = info.census_image;
                        PreviewCensusUrl = GetImageUrl(info.census_image);
                    }

                    // ── Passport photo auto-fill ──
                    if (!string.IsNullOrEmpty(info.student_image))
                    {
                        RegModel.student_image = info.student_image;
                        PreviewImageUrl = GetImageUrl(info.student_image);
                    }

                    // ── Parent NRC images auto-fill ──
                    if (!string.IsNullOrEmpty(info.father_nrc_front_image))
                    {
                        RegModel.father_nrc_front_image = info.father_nrc_front_image;
                        PreviewFatherNrcFrontUrl = GetImageUrl(info.father_nrc_front_image);
                    }
                    if (!string.IsNullOrEmpty(info.father_nrc_back_image))
                    {
                        RegModel.father_nrc_back_image = info.father_nrc_back_image;
                        PreviewFatherNrcBackUrl = GetImageUrl(info.father_nrc_back_image);
                    }
                    if (!string.IsNullOrEmpty(info.mother_nrc_front_image))
                    {
                        RegModel.mother_nrc_front_image = info.mother_nrc_front_image;
                        PreviewMotherNrcFrontUrl = GetImageUrl(info.mother_nrc_front_image);
                    }
                    if (!string.IsNullOrEmpty(info.mother_nrc_back_image))
                    {
                        RegModel.mother_nrc_back_image = info.mother_nrc_back_image;
                        PreviewMotherNrcBackUrl = GetImageUrl(info.mother_nrc_back_image);
                    }
                    if (!string.IsNullOrEmpty(info.nrc_state) && NrcTownshipsByState.TryGetValue(info.nrc_state, out var towns))
                        CurrentTownshipList = towns;

                    // ── အဘ / Guardian NRC — parse "state/township(type)number" string ──
                    if (!string.IsNullOrEmpty(info.app_guardian_nrc) && info.app_guardian_nrc != "-")
                    {
                        var nrcRaw   = info.app_guardian_nrc;
                        var slashIdx = nrcRaw.IndexOf('/');
                        if (slashIdx > 0)
                        {
                            var state  = nrcRaw[..slashIdx];
                            var rest   = nrcRaw[(slashIdx + 1)..];
                            string? township = null;
                            string  nrcType  = "(နိုင်)";
                            string? number   = null;

                            var openParen  = rest.IndexOf('(');
                            var closeParen = rest.IndexOf(')');
                            if (openParen >= 0 && closeParen > openParen)
                            {
                                township = rest[..openParen];
                                nrcType  = rest[openParen..(closeParen + 1)];
                                number   = rest[(closeParen + 1)..];
                            }
                            else { township = rest; }

                            // Guardian NRC fields
                            GuardianNrcState    = state;
                            GuardianNrcTownship = township;
                            GuardianNrcType     = nrcType;
                            GuardianNrcNumber   = ToMyanmarDigits(number ?? "");

                            // Father NRC fields
                            FatherNrcState    = state;
                            FatherNrcTownship = township;
                            FatherNrcType     = nrcType;
                            FatherNrcNumber   = ToMyanmarDigits(number ?? "");

                            if (!string.IsNullOrEmpty(state) && NrcTownshipsByState.ContainsKey(state))
                            {
                                var nrcList = NrcTownshipsByState[state];
                                GuardianTownshipList = nrcList;
                                FatherTownshipList   = nrcList;
                            }
                        }
                    }

                    AutoFillSuccess = true;
                    AutoFillStatus = "✔ ကျောင်းသားအချက်အလက် အလိုအလျောက် ဖြည့်ပြီးပါပြီ";

                    // ── Pass/Fail မှ Semester auto-compute ──
                    if (info.StudentData != null)
                    {
                        LoggedInStudent = info.StudentData;
                    }
                    else if (info.UserId.HasValue && info.UserId > 0)
                    {
                        try
                        {
                            LoggedInStudent = await HttpClientService.ExecuteAsync<StudentModel>(
                                $"Student/user/{info.UserId}", EnumHttpMethod.Get);
                        }
                        catch { }
                    }
                    else if (RegModel.UserId.HasValue && RegModel.UserId > 0)
                    {
                        try
                        {
                            LoggedInStudent = await HttpClientService.ExecuteAsync<StudentModel>(
                                $"Student/user/{RegModel.UserId}", EnumHttpMethod.Get);
                        }
                        catch { }
                    }

                    // ── Retake Status ──
                    if (info.RetakeStatus != null)
                    {
                        RetakeStatus = info.RetakeStatus;
                    }
                    else
                    {
                        int? targetUid = info.UserId ?? RegModel.UserId;
                        if (targetUid.HasValue && targetUid.Value > 0)
                        {
                            try
                            {
                                RetakeStatus = await HttpClientService.ExecuteAsync<StudentRetakeStatusModel>(
                                    $"Student/retake-status/{targetUid.Value}", EnumHttpMethod.Get);
                            }
                            catch { }
                        }
                    }

                    ComputeAllowedSemester();
                }
                else
                {
                    RetakeStatus = null;
                    AutoFillSuccess = false;
                    AutoFillStatus = "⚠ ဤ Roll No. နှင့် Databank ထဲတွင် Data မတွေ့ပါ";
                }
            }
            catch
            {
                RetakeStatus = null;
                AutoFillSuccess = false;
                AutoFillStatus = "⚠ ဤ Roll No. နှင့် Databank ထဲတွင် Data မတွေ့ပါ";
            }

            IsAutoFilling = false;
            StateHasChanged();
        }


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

                if (user.IsInRole("NewStudent") && int.TryParse(newStudentAccIdString, out int parsedNewStudentAccId))
                {
                    RegModel.NewStudentAccId = parsedNewStudentAccId;
                    RegModel.UserId = 0;
                    Console.WriteLine($"Logged in as NewStudent with AccId: {parsedNewStudentAccId}");
                }
                else if (int.TryParse(userIdString, out int parsedUserId))
                {
                    RegModel.UserId = parsedUserId;
                    Console.WriteLine($"Auto-filled UserId: {parsedUserId}");

                    try
                    {
                        var studentData = await HttpClientService.ExecuteAsync<StudentModel>($"Student/user/{parsedUserId}", EnumHttpMethod.Get);
                        if (studentData != null)
                        {
                            LoggedInStudent = studentData;
                            // Roll No intentionally NOT auto-filled here — the student must type
                            // their Roll No to trigger the personal-data auto-fill
                            Console.WriteLine($"Loaded student session for user: {parsedUserId}. Roll No left blank for manual entry.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading student details: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to parse UserId or NewStudentAccId from claims");
                }
            }
            else
            {
                Console.WriteLine("User is not authenticated");
            }

            var semTask = HttpClientService.ExecuteAsync<List<SemesterModel>>("Semester", EnumHttpMethod.Get);
            var gradeTask = HttpClientService.ExecuteAsync<GradeListResponseModel>("grade", EnumHttpMethod.Get);
            var facTask = HttpClientService.ExecuteAsync<List<FacultyModel>>("Faculty", EnumHttpMethod.Get);
            var majorTask = HttpClientService.ExecuteAsync<List<MajorModel>>("Major", EnumHttpMethod.Get);

            try
            {
                await Task.WhenAll(semTask, gradeTask, facTask, majorTask);

                var semRes = semTask.Result;
                if (semRes != null && semRes.Any())
                    SemesterList = semRes;
                else
                    LoadDefaultSemesters();

                var gradeResp = gradeTask.Result;
                if (gradeResp != null && gradeResp.IsSuccess && gradeResp.Data != null)
                    AllGrades = gradeResp.Data.ToList();

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

            // --- Compute allowed semester based on student's result history ---
            ComputeAllowedSemester();
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
            ShowRegistrationStatusPanel = registrationId > 0;

            if (registrationId > 0)
            {
                StudentRegState.SetRegistrationIds(registrationId, userId ?? (RegModel.UserId ?? 0));
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
                ?? RegModel.UserId;
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

                    ApplyRegistrationStatus(SubmittedRegistrationId, SubmittedUserId ?? RegModel.UserId, status, canProceedToPayment);
                }
            }
            finally
            {
                IsRefreshingStatus = false;
            }
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
            SubmittedUserId = RegModel.UserId;
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
                new SemesterModel { SemesterId = 1, SemesterName = "Semester I",    Sequence = 1 },
                new SemesterModel { SemesterId = 2, SemesterName = "Semester II",   Sequence = 2 },
                new SemesterModel { SemesterId = 3, SemesterName = "Semester III",  Sequence = 3 },
                new SemesterModel { SemesterId = 4, SemesterName = "Semester IV",   Sequence = 4 },
                new SemesterModel { SemesterId = 5, SemesterName = "Semester V",    Sequence = 5 },
                new SemesterModel { SemesterId = 6, SemesterName = "Semester VI",   Sequence = 6 },
                new SemesterModel { SemesterId = 7, SemesterName = "Semester VII",  Sequence = 7 },
                new SemesterModel { SemesterId = 8, SemesterName = "Semester VIII", Sequence = 8 },
                new SemesterModel { SemesterId = 9, SemesterName = "Semester IX",   Sequence = 9 }
            };
        }

        // ---- Compute the allowed semester number from the student's result history ----
        private void ComputeAllowedSemester()
        {
            // Helper: SemesterList မှ Sequence နဲ့ match ဆွဲ၊ မရရင် first semester ပြ
            string? GetSemName(int seq) =>
                SemesterList.FirstOrDefault(s => s.Sequence == seq)?.SemesterName
                ?? SemesterList.OrderBy(s => s.Sequence).FirstOrDefault()?.SemesterName;

            if (LoggedInStudent == null)
            {
                // New student / student directory မတွေ့ → Semester 1 ကနေ စမည်
                AllowedSemesterSequence = 1;
                AllowedSemesterName = GetSemName(1);
                RegModel.academic_year_level = AllowedSemesterName;
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
                bool hasPendingFailed = (RetakeStatus != null && RetakeStatus.FailedSubjectsCount > 0);
                bool isGradStatus = string.Equals(LoggedInStudent?.Status, "Graduated", StringComparison.OrdinalIgnoreCase);

                if (!hasPendingFailed && isGradStatus)
                {
                    IsGraduated = true;
                    AllowedSemesterSequence = TotalRequiredSemesters;
                    AllowedSemesterName = GetSemName(TotalRequiredSemesters);
                    RegModel.academic_year_level = AllowedSemesterName;
                    return;
                }
                else
                {
                    // Student completed final semester curriculum but has remaining retakes -> stay on final semester to register retakes
                    AllowedSemesterSequence = TotalRequiredSemesters;
                    AllowedSemesterName = GetSemName(TotalRequiredSemesters);
                    RegModel.academic_year_level = AllowedSemesterName;
                    return;
                }
            }

            AllowedSemesterSequence = firstFailedSeq ?? Math.Min(highestPassed + 1, TotalRequiredSemesters);
            AllowedSemesterName     = GetSemName(AllowedSemesterSequence);
            RegModel.academic_year_level = AllowedSemesterName;

            // Auto-fill "ဖြေဆိုခဲ့သောစာမေးပွဲ" with the latest attempted semester
            if (firstFailedSeq.HasValue)
            {
                var failedSemName = SemesterList.FirstOrDefault(s => s.Sequence == firstFailedSeq.Value)?.SemesterName;
                if (!string.IsNullOrEmpty(failedSemName))
                    PastExamSemester = failedSemName;
                RegModel.past_exam_status = "Fail";
                if (!string.IsNullOrEmpty(RegModel.major))
                    RegModel.past_exam_major = RegModel.major;
                if (!string.IsNullOrEmpty(RegModel.roll_no))
                    RegModel.past_exam_roll_no = RegModel.roll_no;
            }
            else if (highestPassed > 0)
            {
                var lastPassedSemName = SemesterList.FirstOrDefault(s => s.Sequence == highestPassed)?.SemesterName;
                if (!string.IsNullOrEmpty(lastPassedSemName))
                    PastExamSemester = lastPassedSemName;
                RegModel.past_exam_status = "Pass";
                if (!string.IsNullOrEmpty(RegModel.major))
                    RegModel.past_exam_major = RegModel.major;
                if (!string.IsNullOrEmpty(RegModel.roll_no))
                    RegModel.past_exam_roll_no = RegModel.roll_no;
            }
            else
            {
                // ── New student / Semester 1 မအောင်သေးသူ (highestPassed == 0) ──
                // Pass/Fail history တွင် ဘာမှမအောင်သေး → Past exam မှာ တက္ကသိုလ်ဝင် ဖြေဆိုခဲ့တာပဲ
                PastExamSemester = "တက္ကသိုလ်ဝင်စာမေးပွဲ";

                // ခုံအမှတ် → matric_roll_no (တက္ကသိုလ်ဝင် ခုံအမှတ်) ကို auto fill
                if (!string.IsNullOrEmpty(RegModel.matric_roll_no))
                    RegModel.past_exam_roll_no = RegModel.matric_roll_no;

                // အောင်/ကျ → Pass (တက္ကသိုလ်ဝင်ကြောင်း တကျောင်းဝင်ပြီးသားဆို Pass ပဲ)
                RegModel.past_exam_status = "Pass";

                // အဓိကဘာသာ — ဖြေဆိုသည့်ဘာသာ မရွေးရသေးပါက ပထမတွဲ auto select
                if (string.IsNullOrEmpty(RegModel.past_exam_major) ||
                    (RegModel.past_exam_major != "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဇီဝ" &&
                     RegModel.past_exam_major != "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဘောဂ" &&
                     RegModel.past_exam_major != "မြန်မာ/အင်္ဂလိပ်/ပထဝီ/သမိုင်း/ဘောဂ"))
                {
                    RegModel.past_exam_major = "မြန်မာ/အင်္ဂလိပ်/သင်္ချာ/ရူပ/ဓါတု/ဇီဝ";
                }

                // Enrollment major — blank မှသာ IT set
                if (string.IsNullOrEmpty(RegModel.major) || RegModel.major == "-")
                    RegModel.major = "Information Technology";
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
                RegModel.student_name_mm      = prev.StudentNameMm;
                RegModel.student_name_en      = prev.StudentNameEn;
                RegModel.mother_name          = prev.MotherName;
                RegModel.father_name          = prev.FatherName;
                RegModel.gender_relation      = prev.GenderRelation;
                RegModel.ethnicity            = prev.Ethnicity;
                RegModel.religion             = prev.Religion;
                RegModel.pob                  = prev.Pob;
                RegModel.birth_place_region   = prev.BirthPlaceRegion;
                RegModel.nationality_status   = prev.NationalityStatus;
                RegModel.email                = prev.Email;
                RegModel.blood_type           = prev.BloodType;
                RegModel.current_address      = prev.CurrentAddress;
                RegModel.permanent_address_mm = prev.PermanentAddressMm;
                RegModel.permanent_address_en = prev.PermanentAddressEn;
                RegModel.matric_roll_no       = prev.MatricRollNo;
                RegModel.matric_passed_year   = prev.MatricPassedYear;
                RegModel.exam_center          = prev.ExamCenter;
                RegModel.father_occupation    = prev.FatherOccupation;
                RegModel.mother_occupation    = prev.MotherOccupation;
                RegModel.covid_vaccine_status = prev.CovidVaccineStatus;
                RegModel.guardian_name        = prev.GuardianName;
                RegModel.guardian_relationship= prev.GuardianRelationship;
                RegModel.guardian_occupation  = prev.GuardianOccupation;
                RegModel.guardian_address_phone = prev.GuardianAddressPhone;
                RegModel.app_guardian_name    = prev.AppGuardianName;
                RegModel.app_guardian_nrc     = prev.AppGuardianNrc;
                RegModel.app_guardian_phone   = prev.AppGuardianPhone;
                RegModel.app_guardian_address = prev.AppGuardianAddress;
                RegModel.app_student_name     = prev.AppStudentName;
                RegModel.app_student_phone    = prev.AppStudentPhone;
                RegModel.stipend_requested    = prev.StipendRequested;
                RegModel.university_reg_no    = prev.UniversityRegNo;
                RegModel.AdmissionSerialNo    = prev.AdmissionSerialNo;
                if (prev.AdmissionYear.HasValue)
                    RegModel.admission_year = prev.AdmissionYear.Value;

                // Past exam fields
                if (!string.IsNullOrEmpty(prev.PastExamMajor))
                    RegModel.past_exam_major = prev.PastExamMajor;
                if (!string.IsNullOrEmpty(prev.PastExamRollNo))
                    RegModel.past_exam_roll_no = prev.PastExamRollNo;
                if (prev.PastExamYear.HasValue)
                {
                    RegModel.past_exam_year = prev.PastExamYear.Value;
                    PastExamDate = new DateTime(prev.PastExamYear.Value, 1, 1);
                }
                if (!string.IsNullOrEmpty(prev.PastExamStatus))
                    RegModel.past_exam_status = prev.PastExamStatus;

                // အထူးပြူဘာသာ အမာစာ auto-fill (semester locked separately in ComputeAllowedSemester)
                if (!string.IsNullOrEmpty(prev.Major))
                {
                    RegModel.major = prev.Major;
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
                    RegModel.nrc_state    = prev.NrcState;
                    RegModel.nrc_township = prev.NrcTownship;
                    NrcType               = prev.NrcType ?? "(နိုင်)";
                    RegModel.nrc_number   = ToMyanmarDigits(prev.NrcNumber ?? "");

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
            RegModel.nrc_state = e.Value?.ToString();
            RegModel.nrc_township = "";

            if (!string.IsNullOrEmpty(RegModel.nrc_state) && NrcTownshipsByState.ContainsKey(RegModel.nrc_state))
                CurrentTownshipList = NrcTownshipsByState[RegModel.nrc_state];
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
                StateHasChanged();
            }
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
                StateHasChanged();
            }
        }

        private void RemoveNrcFront()
        {
            SelectedNrcFrontFile = null;
            SelectedNrcFrontBytes = null;
            PreviewNrcFrontUrl = null;
            RegModel.nrc_front_image = null;
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
                StateHasChanged();
            }
        }

        private void RemoveNrcBack()
        {
            SelectedNrcBackFile = null;
            SelectedNrcBackBytes = null;
            PreviewNrcBackUrl = null;
            RegModel.nrc_back_image = null;
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
                StateHasChanged();
            }
        }

        private void RemoveCensus()
        {
            SelectedCensusFile = null;
            SelectedCensusBytes = null;
            PreviewCensusUrl = null;
            RegModel.census_image = null;
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
                RegModel.father_nrc_front_image = PreviewFatherNrcFrontUrl;
                StateHasChanged();
            }
        }

        private void RemoveFatherNrcFront()
        {
            SelectedFatherNrcFrontFile = null;
            SelectedFatherNrcFrontBytes = null;
            PreviewFatherNrcFrontUrl = null;
            RegModel.father_nrc_front_image = null;
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
                RegModel.father_nrc_back_image = PreviewFatherNrcBackUrl;
                StateHasChanged();
            }
        }

        private void RemoveFatherNrcBack()
        {
            SelectedFatherNrcBackFile = null;
            SelectedFatherNrcBackBytes = null;
            PreviewFatherNrcBackUrl = null;
            RegModel.father_nrc_back_image = null;
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
                RegModel.mother_nrc_front_image = PreviewMotherNrcFrontUrl;
                StateHasChanged();
            }
        }

        private void RemoveMotherNrcFront()
        {
            SelectedMotherNrcFrontFile = null;
            SelectedMotherNrcFrontBytes = null;
            PreviewMotherNrcFrontUrl = null;
            RegModel.mother_nrc_front_image = null;
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
                RegModel.mother_nrc_back_image = PreviewMotherNrcBackUrl;
                StateHasChanged();
            }
        }

        private void RemoveMotherNrcBack()
        {
            SelectedMotherNrcBackFile = null;
            SelectedMotherNrcBackBytes = null;
            PreviewMotherNrcBackUrl = null;
            RegModel.mother_nrc_back_image = null;
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

        private async Task NextStep()
        {
            if (RetakeStatus?.IsDisqualified == true)
            {
                ShowError($"သတ်မှတ်ထားသော အများဆုံး Retake အကြိမ်အရေအတွက် ({RetakeStatus.MaxRetakeLimit} ကြိမ်) ပြည့်သွားပြီဖြစ်ပါသဖြင့် ကျောင်းအပ်နှံခွင့်/စာရင်းသွင်းခွင့် မရှိတော့ပါ။ ကျောင်းတက်ရောက်ခွင့် အရည်အချင်း မပြည့်မီတော့ပါ။");
                return;
            }

            if (CurrentStep == 1)
            {
                // 💡 User ID ကို Token မှ ဆွဲမရခဲ့ပါက API Error မတက်ခင် ဤနေရာတွင် တားပေးမည်
                if ((RegModel.UserId == null || RegModel.UserId <= 0) && (RegModel.NewStudentAccId == null || RegModel.NewStudentAccId <= 0))
                {
                    ShowError("စနစ်အတွင်း User ID သို့မဟုတ် New Student ID အား ရှာမတွေ့ပါ။ ကျေးဇူးပြု၍ Logout ထွက်ပြီး Login အသစ်ပြန်ဝင်ပေးပါ။");
                    return;
                }

                // 📷 Passport Photo Required Validation
                if (SelectedPhotoBytes == null && string.IsNullOrEmpty(PreviewImageUrl))
                {
                    ShowError("ကျေးဇူးပြု၍ ပတ်စပို့စ်အရွယ် ဓာတ်ပုံ ထည့်သွင်းပေးပါ။ (Passport Photo is required)");
                    return;
                }

                if (string.IsNullOrWhiteSpace(RegModel.student_name_mm) ||
                    string.IsNullOrWhiteSpace(RegModel.app_student_phone) ||
                    string.IsNullOrWhiteSpace(RegModel.nrc_state) ||
                    string.IsNullOrWhiteSpace(RegModel.nrc_township) ||
                    string.IsNullOrWhiteSpace(RegModel.nrc_number) ||
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
                if (string.IsNullOrWhiteSpace(RegModel.permanent_address_mm) ||
                    string.IsNullOrWhiteSpace(RegModel.academic_year_range) ||
                    string.IsNullOrWhiteSpace(RegModel.academic_year_level))
                {
                    ShowError("ကျေးဇူးပြု၍ မရှိမဖြစ်လိုအပ်သော အချက်အလက်များ (*) ကို အပြည့်အစုံ ဖြည့်စွက်ပါ။");
                    return;
                }

                // Auto-fill Step 4 matriculation table fields from Step 2
                RegModel.past_exam_major = RegModel.major;
                RegModel.past_exam_roll_no = RegModel.roll_no;
                if (!string.IsNullOrEmpty(RegModel.academic_year_range))
                {
                    var parts = RegModel.academic_year_range.Split('-');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int yr))
                    {
                        RegModel.past_exam_year = yr;
                        PastExamDate = new DateTime(yr, 1, 1);
                    }
                }

                // Fetch Subjects for Step 3
                var currentSem = SemesterList.FirstOrDefault(s => s.SemesterName == RegModel.academic_year_level);
                if (currentSem != null)
                {
                    // 1. Fetch upcoming subjects filtered by student's major for current semester
                    var upcomingUrl = $"Enrollment/subjects-by-major?semesterId={currentSem.SemesterId}&major={Uri.EscapeDataString(RegModel.major ?? "")}";
                    if (RegModel.UserId.HasValue && RegModel.UserId.Value > 0)
                        upcomingUrl += $"&userId={RegModel.UserId.Value}";
                    if (RegModel.NewStudentAccId.HasValue && RegModel.NewStudentAccId.Value > 0)
                        upcomingUrl += $"&newStudentAccId={RegModel.NewStudentAccId.Value}";
                    if (!string.IsNullOrWhiteSpace(RegModel.roll_no))
                        upcomingUrl += $"&rollNo={Uri.EscapeDataString(RegModel.roll_no.Trim())}";

                    var upcomingResp = await HttpClientService.ExecuteAsync<List<SubjectModel>>(upcomingUrl, EnumHttpMethod.Get);
                    if (upcomingResp != null)
                    {
                        foreach (var sub in upcomingResp)
                        {
                            sub.IsSelected = sub.IsRetake && !sub.IsSubjectDisqualified;
                        }
                        UpcomingSubjects = upcomingResp
                            .OrderByDescending(s => s.IsRetake)
                            .ThenByDescending(s => s.IsCarriedOver)
                            .ThenBy(s => s.SubjectType == Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective)
                            .ThenBy(s => s.SemesterId)
                            .ThenBy(s => s.SubjectCode)
                            .ToList();
                    }

                    // Load Target Credits for this faculty and semester
                    int facId = SelectedFacultyId.HasValue && SelectedFacultyId.Value > 0 
                        ? SelectedFacultyId.Value 
                        : (FacultyList.FirstOrDefault(f => !string.IsNullOrEmpty(LoggedInStudent?.FacultyName) && f.FacultyName == LoggedInStudent.FacultyName)?.FacultyId ?? 1);
                    try
                    {
                        var credResp = await HttpClientService.ExecuteAsync<FacultySemesterCreditModel>($"student/settings/semester-credit/{facId}/{currentSem.SemesterId}", EnumHttpMethod.Get);
                        if (credResp != null)
                        {
                            MinSemesterCredits = credResp.MinCredits.HasValue && credResp.MinCredits.Value > 0 ? credResp.MinCredits.Value : 18;
                            MaxSemesterCredits = credResp.MaxCredits.HasValue && credResp.MaxCredits.Value > 0 ? credResp.MaxCredits.Value : (credResp.RequiredCredits > 0 ? credResp.RequiredCredits : 24);
                            TargetSemesterCredits = MaxSemesterCredits;
                        }
                        else
                        {
                            MinSemesterCredits = 18;
                            MaxSemesterCredits = 24;
                            TargetSemesterCredits = 24;
                        }
                    }
                    catch
                    {
                        MinSemesterCredits = 18;
                        MaxSemesterCredits = 24;
                        TargetSemesterCredits = 24;
                    }
                    
                    // 2. Determine previous semester:
                    // Check if student has failed this current semester (i.e. is repeating current semester)
                    int currentSeq = currentSem.Sequence ?? 1;
                    var currentSemResult = currentSeq switch
                    {
                        1 => LoggedInStudent?.Sem1_Result,
                        2 => LoggedInStudent?.Sem2_Result,
                        3 => LoggedInStudent?.Sem3_Result,
                        4 => LoggedInStudent?.Sem4_Result,
                        5 => LoggedInStudent?.Sem5_Result,
                        6 => LoggedInStudent?.Sem6_Result,
                        7 => LoggedInStudent?.Sem7_Result,
                        8 => LoggedInStudent?.Sem8_Result,
                        9 => LoggedInStudent?.Sem9_Result,
                        _ => null
                    };

                    bool isRepeatingCurrentSemester = string.Equals(currentSemResult, "Fail", StringComparison.OrdinalIgnoreCase);

                    SemesterModel? previousSem = null;
                    if (isRepeatingCurrentSemester)
                    {
                        // Student failed current semester -> Previous attended semester is the previous attempt of current semester!
                        previousSem = currentSem;
                        PreviousSemesterDisplayName = $"{currentSem.SemesterName} (ယခင်ဖြေဆိုခဲ့သော ရမှတ်များ)";
                    }
                    else if (currentSeq > 1)
                    {
                        // First time in current semester -> Previous semester is the passed preceding semester
                        previousSem = SemesterList.FirstOrDefault(s => s.Sequence == (currentSeq - 1));
                        PreviousSemesterDisplayName = $"{previousSem?.SemesterName} (ယခင်အောင်မြင်ခဲ့သော ရမှတ်များ)";
                    }
                    else
                    {
                        // First time in Semester 1 -> No previous semester
                        previousSem = null;
                        PreviousSemesterDisplayName = null;
                    }

                    if (previousSem != null)
                    {
                        var prevUrl = $"Enrollment/previous-grades?semesterId={previousSem.SemesterId}&major={Uri.EscapeDataString(RegModel.major ?? "")}";
                        if (RegModel.UserId.HasValue && RegModel.UserId.Value > 0)
                            prevUrl += $"&userId={RegModel.UserId.Value}";
                        if (RegModel.NewStudentAccId.HasValue && RegModel.NewStudentAccId.Value > 0)
                            prevUrl += $"&newStudentAccId={RegModel.NewStudentAccId.Value}";
                        if (!string.IsNullOrWhiteSpace(RegModel.roll_no))
                            prevUrl += $"&rollNo={Uri.EscapeDataString(RegModel.roll_no.Trim())}";

                        var previousResp = await HttpClientService.ExecuteAsync<List<StudentSubjectGradeItemModel>>(prevUrl, EnumHttpMethod.Get);
                        if (previousResp != null)
                        {
                            PreviousSubjects = previousResp.Select(p => new SubjectGradeBindingModel
                            {
                                SubjectId = p.SubjectId,
                                SubjectName = p.SubjectName,
                                SubjectCode = p.SubjectCode,
                                SemesterName = p.SemesterName,
                                Grade = !string.IsNullOrWhiteSpace(p.ReexamGrade) ? p.ReexamGrade.Trim() : (p.Grade ?? ""),
                                ReexamGrade = p.ReexamGrade,
                                ReexamIsPass = p.ReexamIsPass,
                                IsRetake = p.IsRetake,
                                IsCarriedOver = p.IsCarriedOver,
                                IsReexam = p.IsReexam || !string.IsNullOrWhiteSpace(p.ReexamGrade)
                            }).ToList();
                        }
                        else
                        {
                            PreviousSubjects.Clear();
                        }
                    }
                    else
                    {
                        PreviousSubjects.Clear();
                    }
                }
            }
            else if (CurrentStep == 3)
            {
                int totalSelected = GetTotalSelectedCredits();

                // 1. Semester Required Credit Points Range Validation (18 ~ 24 Credits)
                if (totalSelected < MinSemesterCredits)
                {
                    int remainingToMin = MinSemesterCredits - totalSelected;
                    ShowError($"ဤ Semester တွင် အနည်းဆုံး {MinSemesterCredits} Credits ရွေးချယ်ပေးရန် လိုအပ်ပါသည် (လက်ရှိရွေးချယ်ပြီး: {totalSelected} / သတ်မှတ်ချက်: {MinSemesterCredits} မှ {MaxSemesterCredits} Credits)။ ကျေးဇူးပြု၍ နောက်ထပ် {remainingToMin} Credits ရွေးချယ်ပေးပါ။");
                    return;
                }
                else if (totalSelected > MaxSemesterCredits)
                {
                    int overCredits = totalSelected - MaxSemesterCredits;
                    ShowError($"ဤ Semester တွင် အများဆုံး {MaxSemesterCredits} Credits သာ ရွေးချယ်ခွင့်ရှိပါသည် (လက်ရှိရွေးချယ်ပြီး: {totalSelected} Credits)။ သတ်မှတ်ချက်ထက် {overCredits} Credits ပိုမိုနေပါသဖြင့် {MaxSemesterCredits} Credits ထက် မကျော်လွန်အောင် ပြန်လည်ရွေးချယ်ပေးပါ။");
                    return;
                }

                // 2. Check if any selected subject has unsatisfied prerequisite
                var selectedSubjects = UpcomingSubjects.Where(s => s.IsSelected).ToList();
                var invalidSelected = selectedSubjects.Where(s => !s.IsRetake && !s.IsPrerequisiteSatisfied).ToList();
                if (invalidSelected.Any())
                {
                    var invalidNames = string.Join(", ", invalidSelected.Select(s => s.SubjectName));
                    ShowError($"ရွေးချယ်ထားသော ဘာသာရပ် ({invalidNames}) ၏ Pre-Requisite မအောင်မြင်သေးသဖြင့် ရွေးချယ်၍ မရပါ။");
                    return;
                }

                // 3. Validate Electives per semester does not exceed max allowed
                var electiveGroups = UpcomingSubjects
                    .Where(s => s.SubjectType == Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective && !s.IsRetake)
                    .GroupBy(s => s.SemesterId);

                foreach (var grp in electiveGroups)
                {
                    int maxAllowed = GetMaxElectiveForSemester(grp.Key);
                    int selectedInGrp = grp.Count(s => s.IsSelected);
                    if (selectedInGrp > maxAllowed)
                    {
                        string semName = grp.FirstOrDefault()?.SemesterName ?? $"Semester {grp.Key}";
                        ShowError($"{semName} အတွက် Elective ဘာသာရပ်ကို အများဆုံး {maxAllowed} ခုသာ ရွေးချယ်ခွင့်ရှိပါသည် (လက်ရှိရွေးချယ်ထားမှု: {selectedInGrp} ဘာသာ)။ ကျေးဇူးပြု၍ {maxAllowed} ဘာသာအထိ လျှော့ချပေးပါ။");
                        return;
                    }
                }
            }
            else if (CurrentStep == 4)
            {
                if (string.IsNullOrWhiteSpace(RegModel.father_name) ||
                    string.IsNullOrWhiteSpace(RegModel.guardian_name) ||
                    string.IsNullOrWhiteSpace(RegModel.guardian_address_phone))
                {
                    ShowError("ကျေးဇူးပြု၍ မရှိမဖြစ်လိုအပ်သော အချက်အလက်များ (*) ကို အပြည့်အစုံ ဖြည့်စွက်ပါ။");
                    return;
                }
            }

            if (CurrentStep < TotalSteps)
            {
                if (CurrentStep == 4)
                {
                    RegModel.app_student_name = RegModel.student_name_mm;
                    RegModel.app_guardian_name = RegModel.guardian_name;
                    RegModel.app_guardian_phone = RegModel.guardian_address_phone;
                    RegModel.current_address = RegModel.permanent_address_mm;

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
                        RegModel.app_guardian_nrc = $"{GuardianNrcState}/{GuardianNrcTownship}{GuardianNrcType}{ToEnglishDigits(GuardianNrcNumber)}";
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
        }

        [Inject] public StudentRegistrationState StudentRegState { get; set; } = null!;
        
        private async Task SubmitRegistrationForm()
        {
            IsSubmitting = true;

            RegModel.dob = DobDate ?? DateTime.Now;
            RegModel.covid_vaccine_status = CovidDate?.ToString("dd-MM-yyyy") ?? "-";

            if (PastExamDate.HasValue)
            {
                RegModel.past_exam_year = PastExamDate.Value.Year;
            }
            RegModel.previous_year_roll_no = PastExamSemester;

            string studentNrcNumberEng = ToEnglishDigits(RegModel.nrc_number ?? "");
            string guardianNrcNumberEng = ToEnglishDigits(GuardianNrcNumber ?? "");

            if (!string.IsNullOrEmpty(RegModel.nrc_state) && !string.IsNullOrEmpty(RegModel.nrc_township) && !string.IsNullOrEmpty(RegModel.nrc_number))
            {
                RegModel.nrc_type = NrcType;
                RegModel.student_nrc_no = $"{RegModel.nrc_state}/{RegModel.nrc_township}{NrcType}{studentNrcNumberEng}";
            }
            else
                RegModel.student_nrc_no = "-";

            if (!string.IsNullOrEmpty(GuardianNrcState) && !string.IsNullOrEmpty(GuardianNrcTownship) && !string.IsNullOrEmpty(GuardianNrcNumber))
                RegModel.app_guardian_nrc = $"{GuardianNrcState}/{GuardianNrcTownship}{GuardianNrcType}{guardianNrcNumberEng}";
            else if (string.IsNullOrEmpty(RegModel.app_guardian_nrc))
                RegModel.app_guardian_nrc = "-";

            RegModel.student_name_mm ??= "-";
            RegModel.student_name_en ??= "-";
            RegModel.permanent_address_mm ??= "-";
            RegModel.permanent_address_en ??= "-";
            RegModel.father_name ??= "-";
            RegModel.mother_name ??= "-";
            RegModel.academic_year_range ??= "-";
            RegModel.academic_year_level ??= "-";
            RegModel.major ??= "-";
            RegModel.matric_roll_no ??= "-";
            RegModel.exam_center ??= "-";
            RegModel.pob ??= "-";
            RegModel.birth_place_region ??= "-";
            RegModel.ethnicity ??= "-";
            RegModel.religion ??= "-";

            using var content = new MultipartFormDataContent();

            if (RegModel.UserId.HasValue)
                content.Add(new StringContent(RegModel.UserId.Value.ToString()), "UserId");
            if (RegModel.NewStudentAccId.HasValue && RegModel.NewStudentAccId.Value > 0)
                content.Add(new StringContent(RegModel.NewStudentAccId.Value.ToString()), "NewStudentAccId");
            if (!string.IsNullOrEmpty(RegModel.AdmissionSerialNo))
                content.Add(new StringContent(RegModel.AdmissionSerialNo), "AdmissionSerialNo");

            content.Add(new StringContent(RegModel.academic_year_range ?? "-"), "academic_year_range");
            content.Add(new StringContent(RegModel.academic_year_level ?? "-"), "academic_year_level");
            content.Add(new StringContent(RegModel.major ?? "-"), "major");
            content.Add(new StringContent(RegModel.roll_no ?? "-"), "roll_no");
            content.Add(new StringContent(RegModel.university_reg_no ?? "-"), "university_reg_no");

            if (RegModel.admission_year.HasValue)
                content.Add(new StringContent(RegModel.admission_year.Value.ToString()), "admission_year");

            content.Add(new StringContent(RegModel.student_name_mm ?? "-"), "student_name_mm");
            content.Add(new StringContent(RegModel.student_name_en ?? "-"), "student_name_en");
            content.Add(new StringContent(RegModel.mother_name ?? "-"), "mother_name");
            content.Add(new StringContent(RegModel.father_name ?? "-"), "father_name");
            content.Add(new StringContent(RegModel.gender_relation ?? "-"), "gender_relation");
            content.Add(new StringContent(RegModel.ethnicity ?? "-"), "ethnicity");
            content.Add(new StringContent(RegModel.religion ?? "-"), "religion");
            content.Add(new StringContent(RegModel.pob ?? "-"), "pob");
            content.Add(new StringContent(RegModel.birth_place_region ?? "-"), "birth_place_region");
            content.Add(new StringContent(RegModel.student_nrc_no ?? "-"), "student_nrc_no");
            content.Add(new StringContent(RegModel.nationality_status ?? "-"), "nationality_status");
            content.Add(new StringContent(RegModel.dob.ToString("yyyy-MM-dd")), "dob");
            content.Add(new StringContent(RegModel.email ?? ""), "email");
            content.Add(new StringContent(RegModel.blood_type ?? "-"), "blood_type");
            content.Add(new StringContent(RegModel.covid_vaccine_status ?? "-"), "covid_vaccine_status");
            content.Add(new StringContent(RegModel.current_address ?? ""), "current_address");
            content.Add(new StringContent(RegModel.permanent_address_mm ?? "-"), "permanent_address_mm");
            content.Add(new StringContent(RegModel.permanent_address_en ?? "-"), "permanent_address_en");
            content.Add(new StringContent(RegModel.matric_roll_no ?? "-"), "matric_roll_no");
            content.Add(new StringContent(RegModel.matric_passed_year?.ToString() ?? "0"), "matric_passed_year");
            content.Add(new StringContent(RegModel.exam_center ?? "-"), "exam_center");
            content.Add(new StringContent(RegModel.father_occupation ?? ""), "father_occupation");
            content.Add(new StringContent(RegModel.mother_occupation ?? ""), "mother_occupation");
            content.Add(new StringContent(RegModel.past_exam_major ?? ""), "past_exam_major");
            content.Add(new StringContent(RegModel.past_exam_roll_no ?? ""), "past_exam_roll_no");

            if (RegModel.past_exam_year.HasValue)
                content.Add(new StringContent(RegModel.past_exam_year.Value.ToString()), "past_exam_year");

            content.Add(new StringContent(RegModel.past_exam_status ?? ""), "past_exam_status");
            content.Add(new StringContent(RegModel.previous_year_roll_no ?? ""), "previous_year_roll_no");
            content.Add(new StringContent(RegModel.guardian_name ?? ""), "guardian_name");
            content.Add(new StringContent(RegModel.guardian_relationship ?? ""), "guardian_relationship");
            content.Add(new StringContent(RegModel.guardian_occupation ?? ""), "guardian_occupation");
            content.Add(new StringContent(RegModel.guardian_address_phone ?? ""), "guardian_address_phone");
            content.Add(new StringContent(RegModel.app_guardian_name ?? ""), "app_guardian_name");
            content.Add(new StringContent(RegModel.app_guardian_nrc ?? ""), "app_guardian_nrc");
            content.Add(new StringContent(RegModel.app_guardian_phone ?? ""), "app_guardian_phone");
            content.Add(new StringContent(RegModel.app_guardian_address ?? ""), "app_guardian_address");
            content.Add(new StringContent(RegModel.app_student_name ?? ""), "app_student_name");
            content.Add(new StringContent(RegModel.app_student_phone ?? ""), "app_student_phone");

            if (RegModel.stipend_requested.HasValue)
                content.Add(new StringContent(RegModel.stipend_requested.Value.ToString().ToLower()), "stipend_requested");

            content.Add(new StringContent(RegModel.created_by ?? ""), "created_by");
            content.Add(new StringContent(RegModel.nrc_state ?? ""), "nrc_state");
            content.Add(new StringContent(RegModel.nrc_township ?? ""), "nrc_township");
            content.Add(new StringContent(NrcType), "nrc_type");
            content.Add(new StringContent(studentNrcNumberEng), "nrc_number");

            var selectedSubIds = UpcomingSubjects.Where(s => s.IsSelected).Select(s => s.SubjectId).ToList();
            if (selectedSubIds.Any())
            {
                content.Add(new StringContent(string.Join(",", selectedSubIds)), "selected_subject_ids");
            }

            if (SelectedPhotoBytes != null && SelectedPhotoFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedPhotoBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedPhotoFile.ContentType);
                content.Add(fileContent, "StudentImageFile", SelectedPhotoFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.student_image))
            {
                content.Add(new StringContent(RegModel.student_image), "student_image");
            }

            if (SelectedNrcFrontBytes != null && SelectedNrcFrontFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedNrcFrontBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedNrcFrontFile.ContentType);
                content.Add(fileContent, "NrcFrontImageFile", SelectedNrcFrontFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.nrc_front_image))
            {
                content.Add(new StringContent(RegModel.nrc_front_image), "nrc_front_image");
            }

            if (SelectedNrcBackBytes != null && SelectedNrcBackFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedNrcBackBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedNrcBackFile.ContentType);
                content.Add(fileContent, "NrcBackImageFile", SelectedNrcBackFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.nrc_back_image))
            {
                content.Add(new StringContent(RegModel.nrc_back_image), "nrc_back_image");
            }

            if (SelectedCensusBytes != null && SelectedCensusFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedCensusBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedCensusFile.ContentType);
                content.Add(fileContent, "CensusImageFile", SelectedCensusFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.census_image))
            {
                content.Add(new StringContent(RegModel.census_image), "census_image");
            }

            if (SelectedFatherNrcFrontBytes != null && SelectedFatherNrcFrontFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedFatherNrcFrontBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedFatherNrcFrontFile.ContentType);
                content.Add(fileContent, "FatherNrcFrontImageFile", SelectedFatherNrcFrontFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.father_nrc_front_image))
            {
                content.Add(new StringContent(RegModel.father_nrc_front_image), "father_nrc_front_image");
            }

            if (SelectedFatherNrcBackBytes != null && SelectedFatherNrcBackFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedFatherNrcBackBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedFatherNrcBackFile.ContentType);
                content.Add(fileContent, "FatherNrcBackImageFile", SelectedFatherNrcBackFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.father_nrc_back_image))
            {
                content.Add(new StringContent(RegModel.father_nrc_back_image), "father_nrc_back_image");
            }

            if (SelectedMotherNrcFrontBytes != null && SelectedMotherNrcFrontFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedMotherNrcFrontBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedMotherNrcFrontFile.ContentType);
                content.Add(fileContent, "MotherNrcFrontImageFile", SelectedMotherNrcFrontFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.mother_nrc_front_image))
            {
                content.Add(new StringContent(RegModel.mother_nrc_front_image), "mother_nrc_front_image");
            }

            if (SelectedMotherNrcBackBytes != null && SelectedMotherNrcBackFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedMotherNrcBackBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedMotherNrcBackFile.ContentType);
                content.Add(fileContent, "MotherNrcBackImageFile", SelectedMotherNrcBackFile.Name);
            }
            else if (!string.IsNullOrEmpty(RegModel.mother_nrc_back_image))
            {
                content.Add(new StringContent(RegModel.mother_nrc_back_image), "mother_nrc_back_image");
            }

            try
            {
                var response = await HttpClientService.ExecuteMultipartAsync<StudentRegistrationResponseModel>("StudentRegistrations", content);

                if (response?.IsSuccess == true)
                {
                    IsSuccessModal = true;
                    ModalMessage = "ကျောင်းအပ်နှံခြင်း အချက်အလက်များ အောင်မြင်စွာ တင်သွင်းပြီးပါပြီ။ ကျောင်းဘက်မှ အတည်ပြုစိစစ်ခြင်းကို စောင့်ဆိုင်းပေးပါ။";
                    ShowModal = true;
                    
                    // Store registration data in state service for payment page
                    StudentRegState.SetFromRegistrationModel(RegModel);

                    try
                    {
                        ApplyRegistrationResponseData(response.Data);
                        await NotifierService.NotifyRegistrationSubmitted();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing registration status: {ex.Message}");
                    }
                    
                    // If response has Registration ID, store it
                    if (response.Data != null)
                    {
                        try
                        {
                            // response.Data is a JObject from Newtonsoft.Json
                            var jObj = response.Data as Newtonsoft.Json.Linq.JObject;
                            if (jObj != null)
                            {
                                int registrationId = jObj.Value<int>("id");
                                if (registrationId == 0)
                                {
                                    registrationId = jObj.Value<int>("registrationId");
                                }

                                int userId = jObj.Value<int>("userId");
                                if (userId == 0)
                                {
                                    userId = jObj.Value<int>("UserId");
                                }

                                if (registrationId > 0)
                                {
                                    StudentRegState.SetRegistrationIds(registrationId, userId > 0 ? userId : (RegModel.UserId ?? 0));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error parsing registration ID: {ex.Message}");
                        }
                    }
                }
                else
                {
                    string errMessage = !string.IsNullOrWhiteSpace(response?.Message)
                        ? response.Message
                        : "Data အချက်အလက်များ မပြည့်စုံပါ သို့မဟုတ် မမှန်ကန်ပါ။ ကျေးဇူးပြု၍ ပြန်လည် စစ်ဆေးပေးပါ။";
                    ShowError(errMessage);
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

        public int GetMaxElectiveForSemester(int? semesterId)
        {
            if (!semesterId.HasValue) return 1;
            var sem = SemesterList.FirstOrDefault(s => s.SemesterId == semesterId.Value);
            if (sem == null) return 1;

            var majorName = (RegModel.major ?? "").Trim();
            bool isCS = majorName.Contains("Computer Science", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CS", StringComparison.OrdinalIgnoreCase);
            bool isCT = majorName.Contains("Computer Technology", StringComparison.OrdinalIgnoreCase) || majorName.Equals("CT", StringComparison.OrdinalIgnoreCase);

            int max = isCS ? (sem.MaxElectiveCS ?? 0) : (isCT ? (sem.MaxElectiveCT ?? 0) : (sem.MaxElective ?? 0));
            return max > 0 ? max : 1;
        }

        public int GetTotalRequiredElectives()
        {
            var electives = UpcomingSubjects.Where(s => s.SubjectType == Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective && !s.IsRetake).ToList();
            var distinctSemIds = electives.Select(e => e.SemesterId).Distinct().ToList();
            int total = 0;
            foreach (var semId in distinctSemIds)
            {
                int eligibleInSem = electives.Count(e => e.SemesterId == semId && e.IsPrerequisiteSatisfied);
                int maxInSem = GetMaxElectiveForSemester(semId);
                total += Math.Min(maxInSem, eligibleInSem);
            }
            return total;
        }

        public int GetMaxElectiveForCurrentSemester()
        {
            var currentSem = SemesterList.FirstOrDefault(s => s.SemesterName == RegModel.academic_year_level);
            return GetMaxElectiveForSemester(currentSem?.SemesterId);
        }

        public void OnElectiveRowClicked(SubjectModel ele)
        {
            if (!ele.IsPrerequisiteSatisfied)
            {
                ShowError($"'{ele.SubjectName}' သည် Pre-Requisite မအောင်မြင်သေးသဖြင့် ရွေးချယ်ခွင့် မရှိပါ။\n{ele.PrerequisiteStatusMessage}");
                return;
            }

            int maxAllowed = GetMaxElectiveForSemester(ele.SemesterId);
            var electivesInSem = UpcomingSubjects.Where(s => s.SubjectType == Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective && !s.IsRetake && s.SemesterId == ele.SemesterId).ToList();
            int currentSelectedCount = electivesInSem.Count(s => s.IsSelected);

            if (!ele.IsSelected && currentSelectedCount >= maxAllowed && maxAllowed > 1)
            {
                string semName = ele.SemesterName ?? "ဤ Semester";
                ShowError($"{semName} အတွက် Elective ဘာသာရပ်ကို အများဆုံး {maxAllowed} ခုသာ ရွေးချယ်နိုင်ပါသည် (လက်ရှိရွေးချယ်ပြီး: {currentSelectedCount} ခု)။\nအသစ်ထပ်မံရွေးချယ်လိုပါက ရွေးချယ်ထားပြီးသော ဘာသာရပ်တစ်ခုကို အမှန်ခြစ် ဖြုတ်ပေးပါ။");
                return;
            }

            ToggleElectiveSelection(ele);
        }

        public void ToggleElectiveSelection(SubjectModel sub)
        {
            if (!sub.IsPrerequisiteSatisfied)
            {
                ShowError($"'{sub.SubjectName}' သည် Pre-Requisite မအောင်မြင်သေးသဖြင့် ရွေးချယ်ခွင့် မရှိပါ။\n{sub.PrerequisiteStatusMessage}");
                return;
            }

            int maxAllowed = GetMaxElectiveForSemester(sub.SemesterId);
            var electivesInSem = UpcomingSubjects.Where(s => s.SubjectType == Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective && !s.IsRetake && s.SemesterId == sub.SemesterId).ToList();

            if (sub.IsSelected)
            {
                // Unselecting is always allowed
                sub.IsSelected = false;
            }
            else
            {
                // Trying to select this elective
                int currentSelectedCount = electivesInSem.Count(s => s.IsSelected);
                if (currentSelectedCount >= maxAllowed)
                {
                    if (maxAllowed == 1)
                    {
                        // If max is 1, switch selection to this new one
                        foreach (var s in electivesInSem)
                        {
                            s.IsSelected = false;
                        }
                        sub.IsSelected = true;
                    }
                    else
                    {
                        var semName = sub.SemesterName ?? "ဤ Semester";
                        ShowError($"{semName} အတွက် Elective ဘာသာရပ်ကို အများဆုံး {maxAllowed} ခုသာ ရွေးချယ်နိုင်ပါသည် (လက်ရှိရွေးချယ်ပြီး: {currentSelectedCount} ခု)။\nအသစ်ထပ်မံရွေးချယ်လိုပါက ရွေးချယ်ပြီးသား ဘာသာရပ်တစ်ခုကို အမှန်ခြစ် ဖြုတ်ပေးပါ။");
                        StateHasChanged();
                        return;
                    }
                }
                else
                {
                    sub.IsSelected = true;
                }
            }
            StateHasChanged();
        }

        // ==========================================
        // Live Credit Points Helpers & Subject Selection
        // ==========================================
        public int MinSemesterCredits { get; set; } = 18;
        public int MaxSemesterCredits { get; set; } = 24;
        public int TargetSemesterCredits { get; set; } = 24;

        public bool IsCreditRequirementSatisfied()
        {
            int total = GetTotalSelectedCredits();
            return total >= MinSemesterCredits && total <= MaxSemesterCredits;
        }

        public int GetRemainingToMinCredits()
        {
            int total = GetTotalSelectedCredits();
            return Math.Max(0, MinSemesterCredits - total);
        }

        public int GetSelectedCoreCredits()
        {
            var coreSubs = UpcomingSubjects?.Where(s => 
                s.SubjectType != Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective && 
                !s.IsRetake && 
                !s.IsSubjectDisqualified && 
                s.IsSelected && 
                s.IsPrerequisiteSatisfied
            ).ToList() ?? new();

            return coreSubs.Sum(s => s.Credit > 0 ? s.Credit : 3);
        }

        public int GetSelectedRetakeCredits()
        {
            var retakeSubs = UpcomingSubjects?.Where(s => 
                s.IsRetake && 
                !s.IsSubjectDisqualified && 
                s.IsSelected
            ).ToList() ?? new();

            return retakeSubs.Sum(s => s.Credit > 0 ? s.Credit : 3);
        }

        public int GetSelectedElectiveCredits()
        {
            var electives = UpcomingSubjects?.Where(s => 
                s.SubjectType == Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective && 
                !s.IsRetake && 
                !s.IsSubjectDisqualified && 
                s.IsSelected &&
                s.IsPrerequisiteSatisfied
            ).ToList() ?? new();

            return electives.Sum(s => s.Credit > 0 ? s.Credit : 3);
        }

        public int GetTotalSelectedCredits()
        {
            return GetSelectedCoreCredits() + GetSelectedRetakeCredits() + GetSelectedElectiveCredits();
        }

        public int GetRemainingCredits()
        {
            int total = GetTotalSelectedCredits();
            return Math.Max(0, MaxSemesterCredits - total);
        }

        public double GetCreditProgressPercentage()
        {
            if (MaxSemesterCredits <= 0) return 100.0;
            return Math.Min(100.0, (double)GetTotalSelectedCredits() / MaxSemesterCredits * 100.0);
        }

        public void ToggleSubjectSelection(SubjectModel sub)
        {
            if (sub.IsSubjectDisqualified)
            {
                ShowError($"'{sub.SubjectName}' သည် ၂ ကြိမ်မြောက် Re-exam ကျရှုံးခဲ့သဖြင့် အပြီးတိုင် Retake ယူခွင့် ပိတ်သိမ်းခံထားရသော ဘာသာရပ်ဖြစ်ပါသည်။ ရွေးချယ်ခွင့် မရှိပါ။");
                return;
            }

            if (sub.IsRetake)
            {
                ShowError($"'{sub.SubjectName}' သည် Retake (ပြန်လည်သင်ယူရန် မဖြစ်မနေလိုအပ်သော) ဘာသာရပ်ဖြစ်သဖြင့် အမှန်ခြစ် ဖြုတ်၍ မရပါ။");
                return;
            }

            if (!sub.IsPrerequisiteSatisfied)
            {
                ShowError($"'{sub.SubjectName}' သည် Pre-Requisite မအောင်မြင်သေးသဖြင့် ရွေးချယ်ခွင့် မရှိပါ။\n{sub.PrerequisiteStatusMessage}");
                return;
            }

            if (sub.SubjectType == Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective)
            {
                ToggleElectiveSelection(sub);
                return;
            }

            sub.IsSelected = !sub.IsSelected;
            StateHasChanged();
        }

        public void SelectAllEligibleCoreSubjects()
        {
            if (UpcomingSubjects == null) return;
            foreach (var sub in UpcomingSubjects.Where(s => s.SubjectType != Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.Elective && !s.IsRetake && !s.IsSubjectDisqualified && s.IsPrerequisiteSatisfied))
            {
                sub.IsSelected = true;
            }
            StateHasChanged();
        }

        public void DeselectAllSubjects()
        {
            if (UpcomingSubjects == null) return;
            foreach (var sub in UpcomingSubjects)
            {
                if (!sub.IsRetake)
                {
                    sub.IsSelected = false;
                }
            }
            StateHasChanged();
        }
    }
}
