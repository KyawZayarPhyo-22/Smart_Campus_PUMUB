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

                Console.WriteLine("User Claims:");
                foreach (var claim in user.Claims)
                {
                    Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
                }

                if (int.TryParse(userIdString, out int parsedUserId))
                {
                    RegModel.UserId = parsedUserId;
                    Console.WriteLine($"Auto-filled UserId: {parsedUserId}");

                    try
                    {
                        var studentData = await HttpClientService.ExecuteAsync<StudentModel>($"Student/user/{parsedUserId}", EnumHttpMethod.Get);
                        if (studentData != null)
                        {
                            LoggedInStudent = studentData;
                            RegModel.roll_no = LoggedInStudent.CurrentRollNo;
                            Console.WriteLine($"Loaded student details for user: {parsedUserId}. Roll No auto-filled: {RegModel.roll_no}");
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
                new SemesterModel { SemesterName = "First Year" },
                new SemesterModel { SemesterName = "Second Year" },
                new SemesterModel { SemesterName = "Third Year" },
                new SemesterModel { SemesterName = "Fourth Year" },
                new SemesterModel { SemesterName = "Fifth Year" }
            };
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

        private void NextStep()
        {
            if (CurrentStep == 1)
            {
                // 💡 User ID ကို Token မှ ဆွဲမရခဲ့ပါက API Error မတက်ခင် ဤနေရာတွင် တားပေးမည်
                if (RegModel.UserId == null || RegModel.UserId <= 0)
                {
                    ShowError("စနစ်အတွင်း User ID အား ရှာမတွေ့ပါ။ ကျေးဇူးပြု၍ Logout ထွက်ပြီး Login အသစ်ပြန်ဝင်ပေးပါ။");
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

                // Auto-fill Step 3 matriculation table fields from Step 2
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
            }
            else if (CurrentStep == 3)
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
                if (CurrentStep == 3)
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
                        RegModel.app_guardian_nrc = $"{GuardianNrcState}/{GuardianNrcTownship}{GuardianNrcType}{GuardianNrcNumber}";
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

            if (!string.IsNullOrEmpty(RegModel.nrc_state) && !string.IsNullOrEmpty(RegModel.nrc_township) && !string.IsNullOrEmpty(RegModel.nrc_number))
            {
                RegModel.nrc_type = NrcType;
                RegModel.student_nrc_no = $"{RegModel.nrc_state}/{RegModel.nrc_township}{NrcType}{RegModel.nrc_number}";
            }
            else
                RegModel.student_nrc_no = "-";

            if (!string.IsNullOrEmpty(GuardianNrcState) && !string.IsNullOrEmpty(GuardianNrcTownship) && !string.IsNullOrEmpty(GuardianNrcNumber))
                RegModel.app_guardian_nrc = $"{GuardianNrcState}/{GuardianNrcTownship}{GuardianNrcType}{GuardianNrcNumber}";
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
            content.Add(new StringContent(RegModel.matric_passed_year.ToString()), "matric_passed_year");
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
            content.Add(new StringContent(RegModel.nrc_number ?? ""), "nrc_number");

            if (SelectedPhotoBytes != null && SelectedPhotoFile != null)
            {
                var fileContent = new ByteArrayContent(SelectedPhotoBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedPhotoFile.ContentType);
                content.Add(fileContent, "StudentImageFile", SelectedPhotoFile.Name);
            }

            try
            {
                var response = await HttpClientService.ExecuteMultipartAsync<StudentRegistrationResponseModel>("StudentRegistrations", content);

                if (response?.IsSuccess == true)
                {
                    IsSuccessModal = true;
                    ModalMessage = "Registration submitted successfully. Your information is now pending admin confirmation.";
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
                    ShowError(response?.Message ?? "Data အချက်အလက်များ မပြည့်စုံပါ");
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
