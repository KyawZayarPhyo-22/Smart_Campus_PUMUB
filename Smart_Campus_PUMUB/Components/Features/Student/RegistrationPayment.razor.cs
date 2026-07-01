using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Security.Claims;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;

namespace Smart_Campus_PUMUB.Components.Features.Student
{
    public partial class RegistrationPayment : ComponentBase
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
        [Inject] public NavigationManager Nav { get; set; } = null!;
        [Inject] public StudentRegistrationState StudentRegState { get; set; } = null!; // 💡 Register မှ Data ကို လက်ခံမည့် State
        [Inject] public Smart_Campus_PUMUB.BlazorServer.Frontend.Services.StudentRegistrationNotifierService NotifierService { get; set; } = null!;

        private const string ApprovedStatus = "Approved";

        public RegistrationPaymentCreateRequestModel PaymentModel { get; set; } = new()
        {
            RegistrationId = 0,
            AmountPaid = 0,
            PaymentMethod = "KBZPay"
        };

        public List<PaymentFeeModel> PaymentFees { get; set; } = new();

        public string InputStudentName { get; set; } = "";
        public string InputRollNo { get; set; } = "";
        public string InputAcademicYear { get; set; } = "";

        public List<SemesterModel> SemesterList { get; set; } = new();

        public bool IsLoading { get; set; } = true;
        public bool IsSavingPayment { get; set; } = false;
        public string RegistrationStatus { get; set; } = "";
        public bool CanProceedToPayment { get; set; } = false;

        public string? PreviewReceiptUrl { get; set; }
        public IBrowserFile? SelectedReceiptFile { get; set; }

        public bool ShowModal { get; set; } = false;
        public string ModalMessage { get; set; } = "";
        public bool IsSuccessModal { get; set; } = false;

        private string? GetClaimValue(ClaimsPrincipal user, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                var claim = user.Claims.FirstOrDefault(c => c.Type.Equals(key, StringComparison.OrdinalIgnoreCase) || c.Type.EndsWith(key, StringComparison.OrdinalIgnoreCase));
                if (claim != null) return claim.Value;
            }
            return null;
        }

        protected override async Task OnInitializedAsync()
        {
            // ၁။ Auth စစ်ဆေးခြင်း (Payment Record တွင် CreatedBy ထည့်ရန်)
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                var userIdString = GetClaimValue(user, "User_Id", "UserId", ClaimTypes.NameIdentifier, "id", "uid");
                if (int.TryParse(userIdString, out int parsedUserId))
                {
                    PaymentModel.CreatedBy = parsedUserId.ToString();
                }
            }

            // ၂။ Register မှ လွှဲပြောင်းပေးသော Registration ID ကို ရယူခြင်း
            var uri = new Uri(Nav.Uri);
            var queryParameters = QueryHelpers.ParseQuery(uri.Query);

            if (queryParameters.TryGetValue("regId", out var regIdValue) && int.TryParse(regIdValue, out int regIdFromQuery))
            {
                // URL Query (eg. ?regId=123) မှ ရလျှင် ၎င်းကို သုံးမည်
                PaymentModel.RegistrationId = regIdFromQuery;
            }
            else if (StudentRegState != null && StudentRegState.RegistrationId > 0)
            {
                // Query မှ မရပါက Register Form ဖြည့်စဉ်က သိမ်းခဲ့သော State ထဲမှ ယူမည်
                PaymentModel.RegistrationId = StudentRegState.RegistrationId;
            }

            // ၃။ Registration ID မရရှိပါက Error ပြပြီး ကျောင်းအပ်ဖောင်သို့ ပြန်လွှတ်မည်
            if (PaymentModel.RegistrationId <= 0)
            {
                ShowError("ကျောင်းအပ်နှံမှု မှတ်တမ်းအမှတ် (Registration ID) မတွေ့ရှိပါ။ ကျေးဇူးပြု၍ ကျောင်းအပ်ဖောင်ကို အရင်ဖြည့်ပါ။");
                IsLoading = false;
                return;
            }

            // ၃.၅။ Registration ID ရှိပါက ကျောင်းသားအချက်အလက်ကို API မှ ဆွဲယူ၍ Auto-fill ဖြည့်ပေးမည်
            try
            {
                var regData = await HttpClientService.ExecuteAsync<Newtonsoft.Json.Linq.JObject>(
                    $"StudentRegistrations/{PaymentModel.RegistrationId}", 
                    EnumHttpMethod.Get
                );

                if (regData != null)
                {
                    InputStudentName = regData.Value<string>("studentNameMm") ?? regData.Value<string>("StudentNameMm") ?? "";
                    InputRollNo = regData.Value<string>("rollNo") ?? regData.Value<string>("RollNo") ?? "";
                    InputAcademicYear = regData.Value<string>("academicYearLevel") ?? regData.Value<string>("AcademicYearLevel") ?? "";
                    RegistrationStatus = regData.Value<string>("status") ?? regData.Value<string>("Status") ?? "";
                    CanProceedToPayment = regData.Value<bool?>("canProceedToPayment")
                        ?? regData.Value<bool?>("CanProceedToPayment")
                        ?? string.Equals(RegistrationStatus, ApprovedStatus, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching registration details: {ex.Message}");
                
                // Failover: API မှ မရရှိခဲ့ပါက State Service ထဲမှ ဆွဲယူမည်
                if (StudentRegState != null)
                {
                    InputStudentName = StudentRegState.StudentName;
                    InputRollNo = StudentRegState.RollNo;
                    InputAcademicYear = StudentRegState.AcademicYear;
                }
            }

            // ၃.၇။ Fetch dynamically configured payment fees for this Semester/ClassYear
            if (!string.IsNullOrEmpty(InputAcademicYear))
            {
                await LoadPaymentFees(InputAcademicYear);
            }

            // ၄။ Semester (အတန်း) Data ကို API မှ လှမ်းယူခြင်း
            try
            {
                var semesterResponse = await HttpClientService.ExecuteAsync<List<SemesterModel>>("Semester", EnumHttpMethod.Get);
                if (semesterResponse != null && semesterResponse.Any())
                {
                    SemesterList = semesterResponse;
                }
                else
                {
                    LoadDefaultSemesters();
                }
            }
            catch
            {
                LoadDefaultSemesters();
            }

            IsLoading = false;
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

        private async Task OnReceiptSelected(InputFileChangeEventArgs e)
        {
            SelectedReceiptFile = e.File;
            if (SelectedReceiptFile != null)
            {
                using var ms = new MemoryStream();
                await SelectedReceiptFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(ms);
                PreviewReceiptUrl = $"data:{SelectedReceiptFile.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
                StateHasChanged();
            }
        }

        private async Task SubmitPaymentData()
        {
            if (!CanProceedToPayment)
            {
                ShowError("Registration information is still under admin review. Payment can be submitted only after admin approval.");
                return;
            }

            if (PaymentModel.RegistrationId <= 0)
            {
                ShowError("ကျောင်းအပ်နှံမှု မှတ်တမ်းအမှတ် မတွေ့ရှိပါ။ ကျေးဇူးပြု၍ ကျောင်းအပ်ဖောင်ကို အရင်ဖြည့်ပါ။");
                return;
            }

            if (string.IsNullOrWhiteSpace(InputStudentName))
            {
                ShowError("ကျေးဇူးပြု၍ ကျောင်းသားအမည် ထည့်သွင်းပေးပါ။");
                return;
            }

            if (string.IsNullOrWhiteSpace(InputAcademicYear))
            {
                ShowError("ကျေးဇူးပြု၍ အတန်း (Semester) ရွေးချယ်ပေးပါ။");
                return;
            }

            if (SelectedReceiptFile == null)
            {
                ShowError("ကျေးဇူးပြု၍ ငွေသွင်းစလစ်ပုံ တင်ပေးပါ။");
                return;
            }

            IsSavingPayment = true;

            try
            {
                using var content = new MultipartFormDataContent();

                content.Add(new StringContent(PaymentModel.RegistrationId.ToString()), "RegistrationId");
                content.Add(new StringContent(PaymentModel.AmountPaid.ToString()), "AmountPaid");
                content.Add(new StringContent(PaymentModel.PaymentMethod), "PaymentMethod");
                content.Add(new StringContent(PaymentModel.CreatedBy ?? ""), "CreatedBy");

                var fileContent = new StreamContent(SelectedReceiptFile.OpenReadStream(5 * 1024 * 1024));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(SelectedReceiptFile.ContentType);
                content.Add(fileContent, "ReceiptImage", SelectedReceiptFile.Name);

                var response = await HttpClientService.ExecuteMultipartAsync<RegistrationPaymentResponseModel>("RegistrationPayment", content);

                if (response?.IsSuccess == true)
                {
                    IsSuccessModal = true;
                    ModalMessage = "ငွေသွင်းအချက်အလက်များ အောင်မြင်စွာ တင်သွင်းပြီးပါပြီ။ ကျောင်းမှ အတည်ပြုချိန်အား စောင့်ဆိုင်းပေးပါ။";
                    ShowModal = true;
                    
                    // Notify any listening components (like Page_StudentList.razor)
                    try
                    {
                        await NotifierService.NotifyRegistrationSubmitted();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error triggering registration notifier: {ex.Message}");
                    }
                }

                else
                {
                    ShowError(response?.Message ?? "ငွေသွင်းစနစ် ချို့ယွင်းနေပါသည်။");
                }
            }
            catch (Exception ex)
            {
                ShowError($"System Error: {ex.Message}");
            }
            finally
            {
                IsSavingPayment = false;
            }
        }

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
                // အောင်မြင်ပါက State များကို ရှင်းထုတ်ပြီး Home သို့ ပြန်ပို့မည်
                if (StudentRegState != null) StudentRegState.Clear();
                Nav.NavigateTo("/");
            }
            else if (PaymentModel.RegistrationId <= 0)
            {
                // ID မရှိ၍ Error ပြပါက ကျောင်းအပ်ဖောင်သို့ ပြန်လွှတ်မည်
                Nav.NavigateTo("/Register");
            }
        }

        private async Task LoadPaymentFees(string classYear)
        {
            try
            {
                var fees = await HttpClientService.ExecuteAsync<List<PaymentFeeModel>>(
                    $"payment-fees?classYear={Uri.EscapeDataString(classYear)}", 
                    EnumHttpMethod.Get
                );

                if (fees != null && fees.Any())
                {
                    PaymentFees = fees;
                    PaymentModel.AmountPaid = PaymentFees.Sum(x => x.MontlyAmount);
                }
                else
                {
                    LoadFallbackFees(classYear);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading payment fees: {ex.Message}");
                LoadFallbackFees(classYear);
            }
        }

        private void LoadFallbackFees(string classYear)
        {
            PaymentFees = new List<PaymentFeeModel>
            {
                new PaymentFeeModel { FeeName = "မှတ်ပုံတင်ကြေး", MontlyAmount = 2000 },
                new PaymentFeeModel { FeeName = "ကျောင်းဝင်ကြေး", MontlyAmount = 2000 },
                new PaymentFeeModel { FeeName = "အားကစားကြေး", MontlyAmount = 2000 },
                new PaymentFeeModel { FeeName = "ဓာတ်ခွဲခန်းကြေး", MontlyAmount = 6000 },
                new PaymentFeeModel { FeeName = "စာမေးပွဲဝင်ကြေး", MontlyAmount = 5000 },
                new PaymentFeeModel { FeeName = "စာကြည့်တိုက်ကြေး", MontlyAmount = 5000 },
                new PaymentFeeModel { FeeName = $"ကျောင်းလခ ({classYear})", MontlyAmount = 30000 }
            };
            PaymentModel.AmountPaid = PaymentFees.Sum(x => x.MontlyAmount);
        }
    }
}
