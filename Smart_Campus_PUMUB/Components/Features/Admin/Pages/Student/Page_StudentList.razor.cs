using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentList : ComponentBase, IDisposable
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = null!;
    [Inject] public Smart_Campus_PUMUB.BlazorServer.Frontend.Services.StudentRegistrationNotifierService NotifierService { get; set; } = null!;

    private List<StudentRegistrationDataModel> StudentList { get; set; } = new();


    // Filter Variables
    private string SearchTerm { get; set; } = "";
    private string SelectedLevel { get; set; } = "All";

    // 💡 စဝင်ဝင်ချင်း ToDate ကို ဒီနေ့ရက်စွဲ အဖြစ်ထားပေးမည်။
    private DateTime? FromDate { get; set; }
    private DateTime? ToDate { get; set; } = DateTime.Today;

    // Filter Inputs (bound to UI)
    private string SearchInput { get; set; } = "";
    private string SelectedLevelInput { get; set; } = "All";
    private DateTime? FromDateInput { get; set; }
    private DateTime? ToDateInput { get; set; } = DateTime.Today;

    private async Task ApplyFilter()
    {
        SearchTerm = SearchInput;
        SelectedLevel = SelectedLevelInput;
        FromDate = FromDateInput;
        ToDate = ToDateInput;
        CurrentPage = 1;
        await LoadStudents();
    }

    private async Task ResetFilter()
    {
        SearchInput = "";
        SelectedLevelInput = "All";
        FromDateInput = null;
        ToDateInput = DateTime.Today;

        SearchTerm = "";
        SelectedLevel = "All";
        FromDate = null;
        ToDate = DateTime.Today;
        CurrentPage = 1;
        await LoadStudents();
    }

    private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await ApplyFilter();
        }
    }

    private bool ShowDetailModal { get; set; } = false;
    private StudentRegistrationFullModel? SelectedDetail { get; set; }
    private int ModalCurrentStep { get; set; } = 1;
    private bool IsLoading { get; set; } = true;

    private bool ShowConfirmModal { get; set; } = false;
    private string ConfirmAction { get; set; } = "";
    private string ConfirmMessage { get; set; } = "";
    private bool IsPaymentViewMode { get; set; } = false;

    // Pagination Variables
    private int CurrentPage { get; set; } = 1;
    private int PageSize { get; set; } = 10;
    private int TotalPages { get; set; } = 1;
    private int TotalCount { get; set; } = 0;

    private IEnumerable<StudentRegistrationDataModel> FilteredStudents => StudentList;

    private async Task OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        await LoadStudents();
    }

    protected override async Task OnInitializedAsync()
    {
        NotifierService.OnRegistrationSubmitted += HandleRegistrationSubmitted;
        await LoadStudents();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("initDatePicker", "fromDateInput");
            await JSRuntime.InvokeVoidAsync("initDatePicker", "toDateInput");
        }
    }

    private async Task HandleRegistrationSubmitted()
    {
        await LoadStudents();
        await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        NotifierService.OnRegistrationSubmitted -= HandleRegistrationSubmitted;
    }


    private async Task LoadStudents()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<PagedResult<StudentRegistrationDataModel>>(
                $"StudentRegistrations/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}&level={Uri.EscapeDataString(SelectedLevel)}",
                EnumHttpMethod.Get
            );

            if (response != null)
            {
                StudentList = response.Items;
                TotalCount = response.TotalCount;
                TotalPages = response.TotalPages;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading student registrations: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    private List<PaymentFeeModel> PaymentFees { get; set; } = new();

    private async Task LoadPaymentFeesForDetail(string classYear, RegistrationPaymentModel? payment)
    {
        try
        {
            string url = payment != null
                ? $"payment-fees?classYear={Uri.EscapeDataString(classYear)}&status=All"
                : $"payment-fees?classYear={Uri.EscapeDataString(classYear)}";

            var fees = await HttpClientService.ExecuteAsync<List<PaymentFeeModel>>(url, EnumHttpMethod.Get);

            if (fees != null && fees.Any())
            {
                if (payment != null)
                {
                    PaymentFees = FilterFeesForPayment(fees, payment.AmountPaid, payment.CreatedDateTime);
                }
                else
                {
                    PaymentFees = fees;
                }
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

    private List<PaymentFeeModel> FilterFeesForPayment(List<PaymentFeeModel> fees, decimal amountPaid, DateTime? paymentCreatedTime)
    {
        if (paymentCreatedTime == null)
        {
            return fees.Where(f => f.Status == "Active").ToList();
        }

        var candidateFees = fees.Where(f => f.CreatedDateTime == null || f.CreatedDateTime <= paymentCreatedTime).ToList();

        decimal currentSum = candidateFees.Sum(f => f.MontlyAmount);
        if (currentSum == amountPaid)
        {
            return candidateFees;
        }

        if (currentSum > amountPaid)
        {
            var modifiedPostPayment = candidateFees.Where(f => f.ModifiedDateTime != null && f.ModifiedDateTime > paymentCreatedTime).ToList();
            int n = modifiedPostPayment.Count;
            
            for (int i = 1; i < (1 << n); i++)
            {
                var subsetToRemove = new List<PaymentFeeModel>();
                for (int j = 0; j < n; j++)
                {
                    if ((i & (1 << j)) != 0)
                    {
                        subsetToRemove.Add(modifiedPostPayment[j]);
                    }
                }

                var tempFees = candidateFees.Except(subsetToRemove).ToList();
                if (tempFees.Sum(f => f.MontlyAmount) == amountPaid)
                {
                    return tempFees;
                }
            }
        }

        return candidateFees.Where(f => f.Status == "Active").ToList();
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
    }

    private async Task OpenRegModal(int id)
    {
        IsPaymentViewMode = false;
        SelectedDetail = await HttpClientService.ExecuteAsync<StudentRegistrationFullModel>($"StudentRegistrations/{id}", EnumHttpMethod.Get);
        if (SelectedDetail != null && !string.IsNullOrEmpty(SelectedDetail.AcademicYearLevel))
        {
            var payment = SelectedDetail.RegistrationPayments?.FirstOrDefault();
            await LoadPaymentFeesForDetail(SelectedDetail.AcademicYearLevel, payment);
        }
        ModalCurrentStep = 1;
        ShowDetailModal = true;
    }

    private async Task OpenPaymentModal(int id)
    {
        IsPaymentViewMode = true;
        SelectedDetail = await HttpClientService.ExecuteAsync<StudentRegistrationFullModel>($"StudentRegistrations/{id}", EnumHttpMethod.Get);
        if (SelectedDetail != null && !string.IsNullOrEmpty(SelectedDetail.AcademicYearLevel))
        {
            var payment = SelectedDetail.RegistrationPayments?.FirstOrDefault();
            await LoadPaymentFeesForDetail(SelectedDetail.AcademicYearLevel, payment);
        }
        ModalCurrentStep = 4;
        ShowDetailModal = true;
    }

    private async Task OpenViewModal(int id)
    {
        await OpenRegModal(id);
    }

    private void CloseViewModal()
    {
        ShowDetailModal = false;
        SelectedDetail = null;
    }

    private string GetStatusClass(string? status) => status switch
    {
        "Approved" => "bg-success",
        "Pending Confirmation" => "bg-warning text-dark",
        "Pending" => "bg-warning text-dark",
        "Rejected" => "bg-danger",
        _ => "bg-secondary"
    };

    private bool CanReviewRegistration(string? status)
    {
        return string.Equals(status, "Pending Confirmation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase);
    }

    private bool CanReviewPayment(string? status)
    {
        return string.Equals(status, "Pending Confirmation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(status);
    }

    private void PromptConfirm(string action)
    {
        ConfirmAction = action;
        if (IsPaymentViewMode)
        {
            ConfirmMessage = action == "Approved"
                ? "Are you sure you want to approve this payment?"
                : "Are you sure you want to reject this payment?";
        }
        else
        {
            ConfirmMessage = action == "Approved"
                ? "Are you sure you want to approve this registration?"
                : "Are you sure you want to reject this registration?";
        }
        ShowConfirmModal = true;
    }

    private void CancelConfirm()
    {
        ShowConfirmModal = false;
        ConfirmAction = "";
    }

    private async Task ExecuteConfirm()
    {
        ShowConfirmModal = false;
        if (IsPaymentViewMode)
        {
            await UpdatePaymentStatus(ConfirmAction);
        }
        else
        {
            await UpdateRegistrationStatus(ConfirmAction);
        }
    }

    private async Task UpdatePaymentStatus(string newStatus)
    {
        if (SelectedDetail == null) return;
        var payment = SelectedDetail.RegistrationPayments?.FirstOrDefault();
        if (payment == null) return;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userIdClaim = authState.User.FindFirst("UserId")?.Value;
            int verifyBy = int.TryParse(userIdClaim, out var parsedId) ? parsedId : 1;

            var paymentPayload = new { Status = newStatus, VerifyBy = verifyBy };
            await HttpClientService.ExecuteAsync<object>($"RegistrationPayment/{payment.PaymentId}/verify", EnumHttpMethod.Patch, paymentPayload);

            payment.Status = newStatus;

            var listItem = StudentList.FirstOrDefault(x => x.RegistrationId == SelectedDetail.RegistrationId);
            if (listItem != null && listItem.RegistrationPayments.Any())
            {
                listItem.RegistrationPayments.First().Status = newStatus;
            }

            await NotifierService.NotifyPaymentStatusChanged(payment.PaymentId, SelectedDetail.UserId, newStatus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating payment status: {ex.Message}");
        }
        finally
        {
            CloseViewModal();
            StateHasChanged();
        }
    }

    private async Task UpdateRegistrationStatus(string newStatus)
    {
        if (SelectedDetail == null) return;

        try
        {
            var regPayload = new { Status = newStatus, modified_by = "Admin" };
            await HttpClientService.ExecuteAsync<object>($"StudentRegistrations/{SelectedDetail.RegistrationId}/status", EnumHttpMethod.Patch, regPayload);

            SelectedDetail.Status = newStatus;

            var listItem = StudentList.FirstOrDefault(x => x.RegistrationId == SelectedDetail.RegistrationId);
            if (listItem != null)
            {
                listItem.Status = newStatus;
            }

            await NotifierService.NotifyRegistrationStatusChanged(SelectedDetail.RegistrationId, SelectedDetail.UserId, newStatus);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating status: {ex.Message}");
        }
        finally
        {
            CloseViewModal();
            StateHasChanged();
        }
    }
}

// Models
public class StudentRegistrationDataModel
{
    public int RegistrationId { get; set; }
    public string StudentNameMm { get; set; } = null!;
    public string Major { get; set; } = null!;
    public string? RollNo { get; set; }
    public string? AcademicYearLevel { get; set; }
    public DateTime CreatedDatetime { get; set; }
    public string? Status { get; set; }
    public List<RegistrationPaymentModel> RegistrationPayments { get; set; } = new();
}

public class RegistrationPaymentModel
{
    public int PaymentId { get; set; }
    public int RegistrationId { get; set; }
    public decimal AmountPaid { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ReceiptImage { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedDateTime { get; set; }
}

public class StudentRegistrationFullModel : StudentRegistrationDataModel
{
    public string? StudentNameEn { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? StudentNrcNo { get; set; }
    public string? PermanentAddressMm { get; set; }
    public string? StudentImage { get; set; }
    public string? SignatureImage { get; set; }
    public string? AppGuardianName { get; set; }
    public string? AppGuardianNrc { get; set; }
    public string? AppGuardianPhone { get; set; }
    public string? AppGuardianAddress { get; set; }

    // Additional fields for complete 4-step registration view alignment
    public int? UserId { get; set; }
    public string? AdmissionSerialNo { get; set; }
    public string? AcademicYearRange { get; set; }
    public string? UniversityRegNo { get; set; }
    public int? AdmissionYear { get; set; }
    public string? GenderRelation { get; set; }
    public string? Ethnicity { get; set; }
    public string? Religion { get; set; }
    public string? Pob { get; set; }
    public string? BirthPlaceRegion { get; set; }
    public string? NationalityStatus { get; set; }
    public DateTime? Dob { get; set; }
    public string? Email { get; set; }
    public string? BloodType { get; set; }
    public string? CovidVaccineStatus { get; set; }
    public string? CurrentAddress { get; set; }
    public string? PermanentAddressEn { get; set; }
    public string? MatricRollNo { get; set; }
    public int? MatricPassedYear { get; set; }
    public string? ExamCenter { get; set; }
    public string? FatherOccupation { get; set; }
    public string? MotherOccupation { get; set; }
    public string? PastExamMajor { get; set; }
    public string? PastExamRollNo { get; set; }
    public int? PastExamYear { get; set; }
    public string? PastExamStatus { get; set; }
    public string? PreviousYearRollNo { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? GuardianAddressPhone { get; set; }
    public string? AppStudentName { get; set; }
    public string? AppStudentPhone { get; set; }
    public bool? StipendRequested { get; set; }
}
