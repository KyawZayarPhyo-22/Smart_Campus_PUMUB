using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System.Security.Claims;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;

namespace Smart_Campus_PUMUB.Components.Features.Student
{
    public partial class RegistrationPayment : ComponentBase, IDisposable
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
        [Inject] public NavigationManager Nav { get; set; } = null!;
        [Inject] public StudentRegistrationState StudentRegState { get; set; } = null!; // 💡 Register မှ Data ကို လက်ခံမည့် State
        [Inject] public Smart_Campus_PUMUB.BlazorServer.Frontend.Services.StudentRegistrationNotifierService NotifierService { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

        private const string ApprovedStatus = "Approved";
        private int currentUserId = 0;

        public string SelectedPaymentTab { get; set; } = "KBZPAY_QR"; // "KBZPAY_QR" or "MANUAL_SLIP"
        public bool ShowQrModal { get; set; } = false;
        public bool IsGeneratingQr { get; set; } = false;
        public bool IsPaymentConfirmed { get; set; } = false;
        public KpayPrecreateResponseModel? KpayQrData { get; set; }
        public int QrRemainingSeconds { get; set; } = 300;
        private System.Threading.CancellationTokenSource? _pollingCts;

        public bool ShowReceiptSlipModal { get; set; } = false;
        public string SlipTxnId { get; set; } = "";
        public string SlipOrderId { get; set; } = "";
        public DateTime SlipPaymentDate { get; set; } = DateTime.Now;
        public decimal SlipAmountPaid { get; set; } = 0;

        public RegistrationPaymentCreateRequestModel PaymentModel { get; set; } = new()
        {
            RegistrationId = 0,
            AmountPaid = 0,
            PaymentMethod = "KBZPay (MMQR)"
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
                    currentUserId = parsedUserId;
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
                    var regUserId = regData.Value<int?>("userId") ?? regData.Value<int?>("UserId");
                    if (currentUserId <= 0 && regUserId.HasValue && regUserId.Value > 0)
                    {
                        currentUserId = regUserId.Value;
                    }

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
                new SemesterModel { SemesterName = "Semester I" },
                new SemesterModel { SemesterName = "Semester II" },
                new SemesterModel { SemesterName = "Semester III" },
                new SemesterModel { SemesterName = "Semester IV" },
                new SemesterModel { SemesterName = "Semester V" },
                new SemesterModel { SemesterName = "Semester VI" },
                new SemesterModel { SemesterName = "Semester VII" },
                new SemesterModel { SemesterName = "Semester VIII" },
                new SemesterModel { SemesterName = "Semester IX" }
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
                ShowError($"စနစ်ပိုင်းဆိုင်ရာ ချို့ယွင်းချက် ဖြစ်ပေါ်နေပါသည်: {ex.Message}");
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

        #region KPay MMQR Methods

        public async Task InitiateKpayPayment()
        {
            if (!CanProceedToPayment || PaymentModel.RegistrationId <= 0)
            {
                ShowError("ကျောင်းအပ်နှံမှု အချက်အလက်များကို Admin မှ အတည်ပြုပြီးမှသာ ငွေပေးချေနိုင်ပါမည်။");
                return;
            }

            if (PaymentModel.AmountPaid <= 0)
            {
                ShowError("ပေးချေရမည့် ကျောင်းကြေး ပမာဏ မရှိပါ။");
                return;
            }

            IsGeneratingQr = true;
            try
            {
                var request = new KpayPrecreateRequestModel
                {
                    RegistrationId = PaymentModel.RegistrationId,
                    Amount = PaymentModel.AmountPaid,
                    Title = $"Student Fee - {InputStudentName} ({InputAcademicYear})",
                    CreatedBy = PaymentModel.CreatedBy
                };

                var response = await HttpClientService.ExecuteAsync<KpayPrecreateResponseModel>(
                    "RegistrationPayment/initiate-kpay",
                    EnumHttpMethod.Post,
                    request
                );

                if (response != null && response.IsSuccess)
                {
                    KpayQrData = response;
                    ShowQrModal = true;
                    IsPaymentConfirmed = false;
                    QrRemainingSeconds = 300;
                    StartStatusPolling(response.OrderId);
                }
                else
                {
                    ShowError(response?.Message ?? "KBZPay MMQR Code ထုတ်ယူခြင်း မအောင်မြင်ပါ။");
                }
            }
            catch (Exception ex)
            {
                ShowError($"System Error: {ex.Message}");
            }
            finally
            {
                IsGeneratingQr = false;
            }
        }

        private void StartStatusPolling(string orderId)
        {
            _pollingCts?.Cancel();
            _pollingCts = new System.Threading.CancellationTokenSource();
            var token = _pollingCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && !IsPaymentConfirmed)
                {
                    await Task.Delay(2000, token);
                    if (token.IsCancellationRequested) break;

                    if (QrRemainingSeconds > 0)
                    {
                        QrRemainingSeconds = Math.Max(0, QrRemainingSeconds - 2);
                    }

                    try
                    {
                        var statusRes = await HttpClientService.ExecuteAsync<PaymentStatusCheckResponseModel>(
                            $"RegistrationPayment/check-status/{orderId}",
                            EnumHttpMethod.Get
                        );

                        if (statusRes != null && statusRes.IsPaid)
                        {
                            await InvokeAsync(async () =>
                            {
                                await HandlePaymentSuccess(statusRes);
                            });
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Polling status check error: {ex.Message}");
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }, token);
        }

        private async Task HandlePaymentSuccess(PaymentStatusCheckResponseModel statusRes)
        {
            IsPaymentConfirmed = true;
            StateHasChanged();

            var targetUserId = currentUserId > 0 ? currentUserId : (StudentRegState?.UserId ?? 0);

            try
            {
                await NotifierService.NotifyPaymentStatusChanged(statusRes.PaymentId, targetUserId > 0 ? targetUserId : null, "Approved");
                await NotifierService.NotifyRegistrationStatusChanged(statusRes.RegistrationId, targetUserId > 0 ? targetUserId : null, "Approved");
                await NotifierService.NotifyRegistrationSubmitted();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notifier error: {ex.Message}");
            }

            // Populate Receipt Slip Details
            SlipTxnId = !string.IsNullOrEmpty(KpayQrData?.TxnId) ? KpayQrData.TxnId : $"kp{DateTime.Now:yyyyMMddHHmmss}";
            SlipOrderId = !string.IsNullOrEmpty(KpayQrData?.OrderId) ? KpayQrData.OrderId : $"REG{statusRes.RegistrationId}_{DateTime.Now:yyyyMMddHHmmss}";
            SlipPaymentDate = DateTime.Now;
            SlipAmountPaid = PaymentModel.AmountPaid;

            // Close QR modal and show the Official KBZPay E-Receipt Slip
            await Task.Delay(1000);
            ShowQrModal = false;
            _pollingCts?.Cancel();

            ShowReceiptSlipModal = true;
            StateHasChanged();
        }

        public void CloseReceiptSlip()
        {
            ShowReceiptSlipModal = false;
            Nav.NavigateTo("/");
        }

        public async Task PrintReceiptSlip()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("window.print");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Print error: {ex.Message}");
            }
        }

        public async Task MockPayNow()
        {
            if (KpayQrData == null || string.IsNullOrEmpty(KpayQrData.OrderId)) return;

            try
            {
                await HttpClientService.ExecuteAsync<Newtonsoft.Json.Linq.JObject>(
                    $"RegistrationPayment/mock-complete-kpay/{KpayQrData.OrderId}",
                    EnumHttpMethod.Post
                );

                var statusRes = await HttpClientService.ExecuteAsync<PaymentStatusCheckResponseModel>(
                    $"RegistrationPayment/check-status/{KpayQrData.OrderId}",
                    EnumHttpMethod.Get
                );

                if (statusRes != null && statusRes.IsPaid)
                {
                    await HandlePaymentSuccess(statusRes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mock Pay Error: {ex.Message}");
            }
        }

        public void CloseQrModal()
        {
            _pollingCts?.Cancel();
            ShowQrModal = false;
        }

        public void Dispose()
        {
            _pollingCts?.Cancel();
            _pollingCts?.Dispose();
        }

        #endregion
    }
}
