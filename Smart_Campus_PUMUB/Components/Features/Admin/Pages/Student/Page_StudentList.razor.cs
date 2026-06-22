using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
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

    private void ApplyFilter()
    {
        SearchTerm = SearchInput;
        SelectedLevel = SelectedLevelInput;
        FromDate = FromDateInput;
        ToDate = ToDateInput;
        CurrentPage = 1;
        StateHasChanged();
    }

    private void ResetFilter()
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
        StateHasChanged();
    }

    private void HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            ApplyFilter();
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
    private int PageSize { get; set; } = 10; // ၁ မျက်နှာလျှင် ပြသလိုသော size (ဥပမာ - ၁၀ ယောက်စီ ပြသမည်)
    private int TotalPages { get; set; } = 1;

    private IEnumerable<StudentRegistrationDataModel> GetFilteredStudents()
    {
        var data = StudentList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            data = data.Where(s => (s.RollNo ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   (s.StudentNameMm ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

        if (SelectedLevel != "All")
            data = data.Where(s => string.Equals(s.AcademicYearLevel, SelectedLevel, StringComparison.OrdinalIgnoreCase));

        // 💡 1. From ရော To ရော ရွေးထားလျှင် (Range Filter)
        if (FromDate.HasValue && ToDate.HasValue)
        {
            data = data.Where(s => s.CreatedDatetime.Date >= FromDate.Value.Date && s.CreatedDatetime.Date <= ToDate.Value.Date);
        }
        // 💡 2. From တစ်ခုတည်း ရွေးထားလျှင် (From နောက်ပိုင်း အကုန်)
        else if (FromDate.HasValue && !ToDate.HasValue)
        {
            data = data.Where(s => s.CreatedDatetime.Date >= FromDate.Value.Date);
        }
        // 💡 3. From မရွေးဘဲ To တစ်ခုတည်း ရွေးထားလျှင် (ToDate ထဲက ရက်စွဲ တစ်ရက်တည်းစာ ကွက်တိရှာမည်)
        else if (!FromDate.HasValue && ToDate.HasValue)
        {
            data = data.Where(s => s.CreatedDatetime.Date == ToDate.Value.Date);
        }

        return data.OrderByDescending(s => s.RegistrationId).ToList();
    }

    private IEnumerable<StudentRegistrationDataModel> FilteredStudents
    {
        get
        {
            var allFiltered = GetFilteredStudents();

            // Total Pages ကို တွက်ချက်ခြင်း
            int count = allFiltered.Count();
            int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
            TotalPages = calcPages < 1 ? 1 : calcPages;

            // Filter ပြောင်းသွားလို့ လက်ရှိ Page က စာမျက်နှာစုစုပေါင်းထက် ကြီးသွားရင် ချိန်ညှိပေးခြင်း
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }

            // Skip နှင့် Take ကို အသုံးပြုပြီး သတ်မှတ်ထားသော စာမျက်နှာအတွက်သာ data ကို ဖြတ်ယူခြင်း
            return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    private void OnPageChanged(int newPage)
    {
        CurrentPage = newPage;
        StateHasChanged();
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
            var response = await HttpClientService.ExecuteAsync<List<StudentRegistrationDataModel>>("StudentRegistrations", EnumHttpMethod.Get);
            if (response != null) StudentList = response;
        }
        finally { IsLoading = false; }
    }

    private async Task OpenRegModal(int id)
    {
        IsPaymentViewMode = false;
        SelectedDetail = await HttpClientService.ExecuteAsync<StudentRegistrationFullModel>($"StudentRegistrations/{id}", EnumHttpMethod.Get);
        ModalCurrentStep = 1;
        ShowDetailModal = true;
    }

    private async Task OpenPaymentModal(int id)
    {
        IsPaymentViewMode = true;
        SelectedDetail = await HttpClientService.ExecuteAsync<StudentRegistrationFullModel>($"StudentRegistrations/{id}", EnumHttpMethod.Get);
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
