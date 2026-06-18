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
    public partial class StudentRegister : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public NavigationManager Nav { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;

        public StudentRegistrationCreateRequestModel RegModel { get; set; } = new()
        {
            nationality_status = "တိုင်းရင်းသား",
            stipend_requested = false,
            gender_relation = "Male",
            blood_type = "O",
            academic_year_range = "2023-2024",
            admission_year = DateTime.Now.Year
        };

        public bool ShowModal { get; set; } = false;
        public string ModalMessage { get; set; } = "";
        public bool IsSuccessModal { get; set; } = false;

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
        public List<SemesterModel> SemesterList { get; set; } = new();

        public string NrcType { get; set; } = "(နိုင်)";
        public List<string> CurrentTownshipList { get; set; } = new();

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

        protected override async Task OnInitializedAsync()
        {
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

        public void OnNrcStateChanged(ChangeEventArgs e)
        {
            RegModel.nrc_state = e.Value?.ToString();
            RegModel.nrc_township = "";

            if (!string.IsNullOrEmpty(RegModel.nrc_state) && NrcTownshipsByState.ContainsKey(RegModel.nrc_state))
                CurrentTownshipList = NrcTownshipsByState[RegModel.nrc_state];
            else
                CurrentTownshipList = new List<string>();
        }

        private async Task OnPhotoSelected(InputFileChangeEventArgs e)
        {
            SelectedPhotoFile = e.File;
            if (SelectedPhotoFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedPhotoFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                PreviewImageUrl = $"data:{SelectedPhotoFile.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
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
                // 💡 METHOD 1: AppState Service သုံးခြင်း (အကောင်းဆုံး)
                StudentRegState.SetFromRegistrationModel(RegModel);

                var targetUrl = "/student/payment";
                if (StudentRegState.RegistrationId > 0)
                {
                    targetUrl += $"?regId={StudentRegState.RegistrationId}";
                }

                Nav.NavigateTo(targetUrl);
            }
        }

        [Inject] public StudentRegistrationState StudentRegState { get; set; } = null!;
        
        private async Task SubmitRegistrationForm()
        {
            IsSubmitting = true;

            RegModel.dob = DobDate ?? DateTime.Now;
            RegModel.covid_vaccine_status = CovidDate?.ToString("dd-MM-yyyy") ?? "-";

            if (!string.IsNullOrEmpty(RegModel.nrc_state) && !string.IsNullOrEmpty(RegModel.nrc_township) && !string.IsNullOrEmpty(RegModel.nrc_number))
                RegModel.student_nrc_no = $"{RegModel.nrc_state}/{RegModel.nrc_township}{NrcType}{RegModel.nrc_number}";
            else
                RegModel.student_nrc_no = "-";

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
            content.Add(new StringContent(RegModel.nrc_number ?? ""), "nrc_number");

            if (SelectedPhotoFile != null)
            {
                var fileContent = new StreamContent(SelectedPhotoFile.OpenReadStream(5 * 1024 * 1024));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(SelectedPhotoFile.ContentType);
                content.Add(fileContent, "StudentImageFile", SelectedPhotoFile.Name);
            }

            try
            {
                var response = await HttpClientService.ExecuteMultipartAsync<StudentRegistrationResponseModel>("StudentRegistrations", content);

                if (response?.IsSuccess == true)
                {
                    IsSuccessModal = true;
                    ModalMessage = "မှတ်ပုံတင်ခြင်း အောင်မြင်ပါသည်။ ငွေပေးချေမှု ဆက်လက်လုပ်ဆောင်ပါ။";
                    ShowModal = true;
                    
                    // Store registration data in state service for payment page
                    StudentRegState.SetFromRegistrationModel(RegModel);
                    
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