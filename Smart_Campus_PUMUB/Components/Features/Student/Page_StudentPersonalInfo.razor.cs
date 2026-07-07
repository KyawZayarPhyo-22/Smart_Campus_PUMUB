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
            academic_year_range = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",
            admission_year = DateTime.Now.Year
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
        public DateTime? CovidDate { get; set; }
        public DateTime? SignDate { get; set; } = DateTime.Today;

        public string? PreviewImageUrl { get; set; }
        public IBrowserFile? SelectedPhotoFile { get; set; }
        public byte[]? SelectedPhotoBytes { get; set; }
        public List<SemesterModel> SemesterList { get; set; } = new();
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

        private readonly Dictionary<string, List<string>> NrcTownshipsByState = new()
        {
            { "1", new List<string> { "ကမတ", "ခဖန", "ဆလမ", "တဆလ", "နမတ", "ဖကန", "မခဘ", "မစန", "မညန", "မမန", "မကတ", "ရကန", "လဗန", "ဝမန", "သတန", "ဟပန" } },
            { "2", new List<string> { "ဒမဆ", "ဖရဆ", "ဘလခ", "မဆန", "ရတန", "လကန" } },
            { "3", new List<string> { "ကကရ", "ကဆက", "ကဒန", "ကမမ", "ကရန", "ကလတ", "ခအဇ", "ဘအန", "မဝတ", "ပတန", "ဖအန", "လဘန", "သတင" } },
            { "4", new List<string> { "ကပလ", "ကဆန", "ကတလ", "ခတလ", "စခင", "တတန", "တဇန", "ထတလ", "ပလဝ", "ဖလန", "မတပ", "မကန", "ရကခ", "ဟခန" } },
            { "5", new List<string> { "ကလဝ", "ကလတ", "ကနန", "ခဥတ", "ခတန", "စကင", "စလက", "ဒပယ", "တမန", "ထခင", "နယပ", "ပလဘ", "ဖလန", "ဘမန", "မလန", "မကန", "မမန", "ရဘန", "လဟန", "ဝလတ", "ဟမလ" } },
            { "6", new List<string> { "ကသန", "ခမက", "ထဝယ", "ပလန", "မမန", "ရဖန", "လလန" } },
            { "7", new List<string> { "ကပက", "ကဝန", "ညလပ", "တငင", "ထရန", "ဒဥက", "ပခန", "ပတန", "ဖမန", "မလန", "ရတရ", "လပတ", "ဝမန", "သဝတ" } },
            { "8", new List<string> { "ကမန", "ခမန", "ဂဂဝ", "ဆမန", "တတက", "နမဖ", "ပခက", "ပမန", "မကန", "မဘန", "မလန", "ရစက", "လဟန", "သယန" } },
            { "9", new List<string> { "ကဆန", "ကပတ", "ခအဇ", "စကတ", "တတဥ", "ပဘန", "ပမန", "မကန", "မတလ", "မဟမ", "ရမသ", "လဝန", "ဝတန", "သစန" } },
            { "10", new List<string> { "ကမရ", "ခဆန", "စမန", "တတန", "ထမန", "ပမန", "မလမ", "မဒန", "ရမန", "လမန", "သထန" } },
            { "11", new List<string> { "ကတန", "ခအဇ", "စတပ", "တကန", "ပဏတ", "ပတန", "မအန", "မပန", "ရသတ" } },
            { "12", new List<string> { "ကမရ", "ကမတ", "ခရန", "စခင", "တမဝ", "ဒဂမ", "ဒဂရ", "ဒပန", "ပဘတ", "မဂဒ", "ရကန", "လမတ", "သဃက" } },
            { "13", new List<string> { "ကထန", "ခလန", "ညရန", "တခလ", "နစန", "ပလန", "မဆန", "မငန", "ရစန", "လခတ" } },
            { "14", new List<string> { "ကလန", "ခရန", "ညတန", "တကန", "ပသန", "ဖပန", "မအပ", "မမင", "ရကန", "လမန", "ဟသတ" } }
        };

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

            try
            {
                var response = await HttpClientService.ExecuteAsync<List<SemesterModel>>("Semester", EnumHttpMethod.Get);
                if (response != null && response.Any())
                {
                    SemesterList = response;
                }
                else
                {
                    LoadDefaultSemesters();
                }
            }
            catch { LoadDefaultSemesters(); }

            // --- Compute allowed semester based on academic history ---
            ComputeAllowedSemester();

            // --- Load existing personal info or auto-fill from previous registration ---
            if (requestModel.NewStudentAccId.HasValue && requestModel.NewStudentAccId > 0)
            {
                await LoadStudentPersonalInfoForNewStudent(requestModel.NewStudentAccId.Value);
            }
            else if (requestModel.UserId.HasValue && requestModel.UserId > 0)
            {
                await LoadStudentPersonalInfo(requestModel.UserId.Value);
            }
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

                if (info != null)
                {
                    isUpdate = true;
                    IsFormDisabled = true;
                    
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
                    requestModel.nrc_number = info.nrc_number;

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

                    // Guardian NRC
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
                return "Admin has approved your registration information. You can continue to payment.";
            }

            if (IsRejected)
            {
                return "Admin rejected this registration information. Please correct the form and submit a new registration.";
            }

            return "Your submitted information is under admin review. Payment will be available after approval.";
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
                new SemesterModel { SemesterId = 1, SemesterName = "First Year",  Sequence = 1 },
                new SemesterModel { SemesterId = 2, SemesterName = "Second Year", Sequence = 2 },
                new SemesterModel { SemesterId = 3, SemesterName = "Third Year",  Sequence = 3 },
                new SemesterModel { SemesterId = 4, SemesterName = "Fourth Year", Sequence = 4 },
                new SemesterModel { SemesterId = 5, SemesterName = "Fifth Year",  Sequence = 5 }
            };
        }

        // ---- Compute the allowed semester number from the student's result history ----
        private void ComputeAllowedSemester()
        {
            if (LoggedInStudent == null)
            {
                AllowedSemesterSequence = 1;
                AllowedSemesterName = SemesterList.FirstOrDefault(s => s.Sequence == 1)?.SemesterName;
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

            if (highestPassed >= 9 && firstFailedSeq == null)
            {
                IsGraduated = true;
                AllowedSemesterSequence = 9;
                AllowedSemesterName = SemesterList.FirstOrDefault(s => s.Sequence == 9)?.SemesterName;
                return;
            }

            AllowedSemesterSequence = firstFailedSeq ?? (highestPassed + 1);
            AllowedSemesterName = SemesterList.FirstOrDefault(s => s.Sequence == AllowedSemesterSequence)?.SemesterName
                ?? SemesterList.FirstOrDefault()?.SemesterName;

            // Pre-select the allowed semester in the form model
            requestModel.academic_year_level = AllowedSemesterName;

            // Auto-fill "ဖြေဆိုခဲ့သောစာမေးပွဲ" with the last PASSED semester from student directory
            if (highestPassed > 0)
            {
                var lastPassedSemName = SemesterList.FirstOrDefault(s => s.Sequence == highestPassed)?.SemesterName;
                if (!string.IsNullOrEmpty(lastPassedSemName))
                    PastExamSemester = lastPassedSemName;

                // Auto-fill "အောင် / က်" status for that semester
                // The last-passed semester is always "Pass"; if there's a failed sem, use that result
                if (firstFailedSeq.HasValue && firstFailedSeq.Value <= highestPassed)
                    requestModel.past_exam_status = "Fail";
                else
                    requestModel.past_exam_status = "Pass";
            }
            else if (firstFailedSeq.HasValue)
            {
                // No passed semester at all, but there is a failed one
                var failedSemName = SemesterList.FirstOrDefault(s => s.Sequence == firstFailedSeq.Value)?.SemesterName;
                if (!string.IsNullOrEmpty(failedSemName))
                    PastExamSemester = failedSemName;
                requestModel.past_exam_status = "Fail";
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

                // အထူးပြူဘာသာ အမာစာ auto-fill (semester locked separately in ComputeAllowedSemester)
                if (!string.IsNullOrEmpty(prev.Major))
                    requestModel.major = prev.Major;

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
            if (lower.Contains("first") || lower.Contains("sem 1") || lower.Contains("semester 1") || lower.Contains("1st") || lower.Contains("one")) return 1;
            if (lower.Contains("second") || lower.Contains("sem 2") || lower.Contains("semester 2") || lower.Contains("2nd") || lower.Contains("two")) return 2;
            if (lower.Contains("third") || lower.Contains("sem 3") || lower.Contains("semester 3") || lower.Contains("3rd") || lower.Contains("three")) return 3;
            if (lower.Contains("fourth") || lower.Contains("sem 4") || lower.Contains("semester 4") || lower.Contains("4th") || lower.Contains("four")) return 4;
            if (lower.Contains("fifth") || lower.Contains("sem 5") || lower.Contains("semester 5") || lower.Contains("5th") || lower.Contains("five")) return 5;
            if (lower.Contains("sixth") || lower.Contains("sem 6") || lower.Contains("semester 6") || lower.Contains("6th") || lower.Contains("six")) return 6;
            if (lower.Contains("seventh") || lower.Contains("sem 7") || lower.Contains("semester 7") || lower.Contains("7th") || lower.Contains("seven")) return 7;
            if (lower.Contains("eighth") || lower.Contains("sem 8") || lower.Contains("semester 8") || lower.Contains("8th") || lower.Contains("eight")) return 8;
            if (lower.Contains("ninth") || lower.Contains("sem 9") || lower.Contains("semester 9") || lower.Contains("9th") || lower.Contains("nine")) return 9;
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
                StateHasChanged();
            }
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
            if (IsSuccessModal)
            {
                Nav.NavigateTo("/profile");
            }
        }

        public void EnableEditing()
        {
            IsFormDisabled = false;
            StateHasChanged();
        }

        [Inject] public StudentRegistrationState StudentRegState { get; set; } = null!;
        
        private async Task SavePersonalInfoForm()
        {
            // Only allow submission if not in admin view
            if (IsAdminView) return;

            IsSubmitting = true;

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
                    IsSuccessModal = true;
                    ModalMessage = "Personal info saved successfully.";
                    ShowModal = true;
                }
                else
                {
                    ShowError(response?.Message ?? "An error occurred while saving.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"System Error: {ex.Message}");
            }
            finally
            {
                IsSubmitting = false;
            }
        }
    }
}
