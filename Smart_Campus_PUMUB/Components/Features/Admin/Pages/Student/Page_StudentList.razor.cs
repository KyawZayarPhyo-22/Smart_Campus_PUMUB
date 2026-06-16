using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Student;

public partial class Page_StudentList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<StudentRegistrationDataModel> StudentList { get; set; } = new();

    private string SearchTerm { get; set; } = "";
    private string SelectedLevel { get; set; } = "All";
    private DateTime SelectedDate { get; set; } = DateTime.Today;

    private bool ShowDetailModal { get; set; } = false;
    private StudentRegistrationFullModel? SelectedDetail { get; set; }
    private bool IsLoading { get; set; } = true;

    // Confirm Modal Variables
    private bool ShowConfirmModal { get; set; } = false;
    private string ConfirmAction { get; set; } = "";
    private string ConfirmMessage { get; set; } = "";

    private IEnumerable<StudentRegistrationDataModel> FilteredStudents
    {
        get
        {
            var data = StudentList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
                data = data.Where(s => (s.RollNo ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       (s.StudentNameMm ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

            if (SelectedLevel != "All")
                data = data.Where(s => string.Equals(s.AcademicYearLevel, SelectedLevel, StringComparison.OrdinalIgnoreCase));

            data = data.Where(s => s.CreatedDatetime.Date == SelectedDate.Date);

            return data.OrderBy(s => s.AcademicYearLevel).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadStudents();
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

    private async Task OpenViewModal(int id)
    {
        SelectedDetail = await HttpClientService.ExecuteAsync<StudentRegistrationFullModel>($"StudentRegistrations/{id}", EnumHttpMethod.Get);
        ShowDetailModal = true;
    }

    private void CloseViewModal()
    {
        ShowDetailModal = false;
        SelectedDetail = null;
    }

    private string GetStatusClass(string? status) => status switch
    {
        "Approved" => "bg-success",
        "Pending" => "bg-warning text-dark",
        "Rejected" => "bg-danger",
        _ => "bg-secondary"
    };

    // 💡 Confirm Box ခေါ်မည့် Method
    private void PromptConfirm(string action)
    {
        ConfirmAction = action;
        ConfirmMessage = action == "Approved"
            ? "Are you sure you want to approve this registration?"
            : "Are you sure you want to reject this registration?";
        ShowConfirmModal = true;
    }

    // 💡 Confirm Box ပိတ်မည့် Method
    private void CancelConfirm()
    {
        ShowConfirmModal = false;
        ConfirmAction = "";
    }

    // 💡 သေချာပြီဆိုမှ Update လုပ်မည့် Method
    private async Task ExecuteConfirm()
    {
        ShowConfirmModal = false;
        await UpdateRegistrationStatus(ConfirmAction);
    }

    private async Task UpdateRegistrationStatus(string newStatus)
    {
        if (SelectedDetail == null) return;

        try
        {
            var regPayload = new { Status = newStatus, modified_by = "Admin" };
            await HttpClientService.ExecuteAsync<object>($"StudentRegistrations/{SelectedDetail.RegistrationId}/status", EnumHttpMethod.Patch, regPayload);

            if (SelectedDetail.RegistrationPayments != null && SelectedDetail.RegistrationPayments.Any())
            {
                var paymentId = SelectedDetail.RegistrationPayments.First().PaymentId;
                var payPayload = new { Status = newStatus, VerifyBy = 1 };
                await HttpClientService.ExecuteAsync<object>($"RegistrationPayment/{paymentId}/verify", EnumHttpMethod.Patch, payPayload);
            }

            SelectedDetail.Status = newStatus;
            if (SelectedDetail.RegistrationPayments?.Any() == true)
            {
                SelectedDetail.RegistrationPayments.First().Status = newStatus;
            }

            var listItem = StudentList.FirstOrDefault(x => x.RegistrationId == SelectedDetail.RegistrationId);
            if (listItem != null)
            {
                listItem.Status = newStatus;
                if (listItem.RegistrationPayments?.Any() == true)
                {
                    listItem.RegistrationPayments.First().Status = newStatus;
                }
            }
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
}