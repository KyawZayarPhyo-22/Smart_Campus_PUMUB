using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Features.Admin.Pages.Registrationreport;

public partial class Page_RegistrationReport : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<StudentRegistrationDataModel> RawStudentList { get; set; } = new();
    private bool IsLoading { get; set; } = true;

    private int ActiveTab { get; set; } = 1;

    // Filters
    private DateTime? AdmFromDate { get; set; }
    private DateTime? AdmToDate { get; set; } = DateTime.Today;
    private string AdmSelectedMajor { get; set; } = "All";
    private string AdmSelectedYear { get; set; } = "All";
    private string AdmSelectedStatus { get; set; } = "All"; // 💡 Registration Status Filter

    // Active Filters (actually applied on button click)
    private DateTime? ActiveAdmFromDate { get; set; }
    private DateTime? ActiveAdmToDate { get; set; } = DateTime.Today;
    private string ActiveAdmSelectedMajor { get; set; } = "All";
    private string ActiveAdmSelectedYear { get; set; } = "All";
    private string ActiveAdmSelectedStatus { get; set; } = "All";

    private void ApplyAdmissionFilter()
    {
        ActiveAdmFromDate = AdmFromDate;
        ActiveAdmToDate = AdmToDate;
        ActiveAdmSelectedMajor = AdmSelectedMajor;
        ActiveAdmSelectedYear = AdmSelectedYear;
        ActiveAdmSelectedStatus = AdmSelectedStatus;
        CurrentPageAdm = 1;
        StateHasChanged();
    }

    private void ResetAdmissionFilter()
    {
        AdmFromDate = null;
        AdmToDate = DateTime.Today;
        AdmSelectedMajor = "All";
        AdmSelectedYear = "All";
        AdmSelectedStatus = "All";

        ActiveAdmFromDate = null;
        ActiveAdmToDate = DateTime.Today;
        ActiveAdmSelectedMajor = "All";
        ActiveAdmSelectedYear = "All";
        ActiveAdmSelectedStatus = "All";
        CurrentPageAdm = 1;
        StateHasChanged();
    }

    private DateTime? PayFromDate { get; set; }
    private DateTime? PayToDate { get; set; } = DateTime.Today;
    private string PaySelectedYear { get; set; } = "All";
    private string PaySelectedMethod { get; set; } = "All";
    private string PaySelectedStatus { get; set; } = "All"; // 💡 Payment Status Filter

    // Active Filters (actually applied on button click)
    private DateTime? ActivePayFromDate { get; set; }
    private DateTime? ActivePayToDate { get; set; } = DateTime.Today;
    private string ActivePaySelectedYear { get; set; } = "All";
    private string ActivePaySelectedMethod { get; set; } = "All";
    private string ActivePaySelectedStatus { get; set; } = "All";

    private void ApplyPaymentFilter()
    {
        ActivePayFromDate = PayFromDate;
        ActivePayToDate = PayToDate;
        ActivePaySelectedYear = PaySelectedYear;
        ActivePaySelectedMethod = PaySelectedMethod;
        ActivePaySelectedStatus = PaySelectedStatus;
        CurrentPagePay = 1;
        StateHasChanged();
    }

    private void ResetPaymentFilter()
    {
        PayFromDate = null;
        PayToDate = DateTime.Today;
        PaySelectedYear = "All";
        PaySelectedMethod = "All";
        PaySelectedStatus = "All";

        ActivePayFromDate = null;
        ActivePayToDate = DateTime.Today;
        ActivePaySelectedYear = "All";
        ActivePaySelectedMethod = "All";
        ActivePaySelectedStatus = "All";
        CurrentPagePay = 1;
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        ApplyAdmissionFilter();
        ApplyPaymentFilter();
    }

    private async Task LoadData()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<StudentRegistrationDataModel>>("StudentRegistrations", EnumHttpMethod.Get);
            if (response != null) RawStudentList = response;
        }
        finally { IsLoading = false; }
    }

    // =================================================================
    // 💡 1. Base Filtered Data
    // =================================================================
    private IEnumerable<StudentRegistrationDataModel> BaseAdmissionFilteredData
    {
        get
        {
            var data = RawStudentList.AsEnumerable();

            if (ActiveAdmFromDate.HasValue && ActiveAdmToDate.HasValue)
                data = data.Where(s => s.CreatedDatetime.Date >= ActiveAdmFromDate.Value.Date && s.CreatedDatetime.Date <= ActiveAdmToDate.Value.Date);
            else if (!ActiveAdmFromDate.HasValue && ActiveAdmToDate.HasValue)
                data = data.Where(s => s.CreatedDatetime.Date == ActiveAdmToDate.Value.Date);

            if (ActiveAdmSelectedMajor != "All")
                data = data.Where(s => s.Major == ActiveAdmSelectedMajor);

            if (ActiveAdmSelectedYear != "All")
                data = data.Where(s => s.AcademicYearLevel == ActiveAdmSelectedYear);

            // 💡 Registration Status Logic
            if (ActiveAdmSelectedStatus != "All")
                data = data.Where(s => (s.Status ?? "Pending") == ActiveAdmSelectedStatus);

            return data;
        }
    }

    private IEnumerable<PaymentReportItem> BasePaymentFilteredData
    {
        get
        {
            var payments = new List<PaymentReportItem>();

            foreach (var student in RawStudentList)
            {
                foreach (var pay in student.RegistrationPayments)
                {
                    payments.Add(new PaymentReportItem
                    {
                        StudentName = student.StudentNameMm,
                        RollNo = student.RollNo ?? "-",
                        Major = student.Major,
                        AcademicYear = student.AcademicYearLevel,
                        AmountPaid = pay.AmountPaid,
                        PaymentMethod = pay.PaymentMethod ?? "-",
                        Status = pay.Status ?? "Pending",
                        PaymentDate = student.CreatedDatetime
                    });
                }
            }

            var data = payments.AsEnumerable();

            if (ActivePayFromDate.HasValue && ActivePayToDate.HasValue)
                data = data.Where(p => p.PaymentDate.Date >= ActivePayFromDate.Value.Date && p.PaymentDate.Date <= ActivePayToDate.Value.Date);
            else if (!ActivePayFromDate.HasValue && ActivePayToDate.HasValue)
                data = data.Where(p => p.PaymentDate.Date == ActivePayToDate.Value.Date);

            if (ActivePaySelectedYear != "All")
                data = data.Where(p => p.AcademicYear == ActivePaySelectedYear);

            if (ActivePaySelectedMethod != "All")
                data = data.Where(p => p.PaymentMethod == ActivePaySelectedMethod);

            // 💡 Payment Status Logic
            if (ActivePaySelectedStatus != "All")
                data = data.Where(p => p.Status == ActivePaySelectedStatus);

            return data;
        }
    }

    // =================================================================
    // 💡 2. Totals Calculation 
    // =================================================================
    private int TotalRegistrations => BaseAdmissionFilteredData.Count();
    private int TotalApprovedReg => BaseAdmissionFilteredData.Count(s => (s.Status ?? "Pending") == "Approved");
    private int TotalRejectedReg => BaseAdmissionFilteredData.Count(s => (s.Status ?? "Pending") == "Rejected");

    private int TotalPayments => BasePaymentFilteredData.Count();
    private int TotalApprovedPay => BasePaymentFilteredData.Count(p => p.Status == "Approved");
    private int TotalRejectedPay => BasePaymentFilteredData.Count(p => p.Status == "Rejected");

    // Pagination Variables
    private int PageSize { get; set; } = 10;
    private int CurrentPageAdm { get; set; } = 1;
    private int TotalPagesAdm { get; set; } = 1;
    private int CurrentPagePay { get; set; } = 1;
    private int TotalPagesPay { get; set; } = 1;

    private void OnPageChangedAdm(int newPage)
    {
        CurrentPageAdm = newPage;
        StateHasChanged();
    }

    private void OnPageChangedPay(int newPage)
    {
        CurrentPagePay = newPage;
        StateHasChanged();
    }

    // =================================================================
    // 💡 3. UI တွင် ပြသမည့် Table Data
    // =================================================================
    private IEnumerable<StudentRegistrationDataModel> AdmissionReportData
    {
        get
        {
            var allFiltered = BaseAdmissionFilteredData.OrderBy(s => s.RollNo?.Length ?? 0).ThenBy(s => s.RollNo).ToList();
            int count = allFiltered.Count;
            int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
            TotalPagesAdm = calcPages < 1 ? 1 : calcPages;
            if (CurrentPageAdm > TotalPagesAdm) CurrentPageAdm = TotalPagesAdm;
            if (CurrentPageAdm < 1) CurrentPageAdm = 1;
            return allFiltered.Skip((CurrentPageAdm - 1) * PageSize).Take(PageSize).ToList();
        }
    }

    private IEnumerable<PaymentReportItem> PaymentReportData
    {
        get
        {
            var allFiltered = BasePaymentFilteredData.OrderBy(p => p.RollNo?.Length ?? 0).ThenBy(p => p.RollNo).ToList();
            int count = allFiltered.Count;
            int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
            TotalPagesPay = calcPages < 1 ? 1 : calcPages;
            if (CurrentPagePay > TotalPagesPay) CurrentPagePay = TotalPagesPay;
            if (CurrentPagePay < 1) CurrentPagePay = 1;
            return allFiltered.Skip((CurrentPagePay - 1) * PageSize).Take(PageSize).ToList();
        }
    }


    private async Task ExportAdmissionToExcel()
    {
        var builder = new StringBuilder();
        builder.AppendLine("No,Admission Date,Roll No,Name,Major,Academic Year,Status");

        var allData = BaseAdmissionFilteredData.OrderBy(s => s.RollNo?.Length ?? 0).ThenBy(s => s.RollNo).ToList();
        int count = 1;
        foreach (var item in allData)
        {
            builder.AppendLine($"{count},{item.CreatedDatetime:dd-MM-yyyy},{item.RollNo},{item.StudentNameMm},{item.Major},{item.AcademicYearLevel},{item.Status ?? "Pending"}");
            count++;
        }

        await DownloadFile("Admission_Report.csv", builder.ToString());
    }

    private async Task ExportPaymentToExcel()
    {
        var builder = new StringBuilder();
        builder.AppendLine("No,Date,Roll No,Name,Major,Academic Year,Payment Method,Amount Paid,Status");

        var allData = BasePaymentFilteredData.OrderBy(p => p.RollNo?.Length ?? 0).ThenBy(p => p.RollNo).ToList();
        int count = 1;
        foreach (var item in allData)
        {
            builder.AppendLine($"{count},{item.PaymentDate:dd-MM-yyyy},{item.RollNo},{item.StudentName},{item.Major},{item.AcademicYear},{item.PaymentMethod},{item.AmountPaid},{item.Status}");
            count++;
        }

        await DownloadFile("Payment_Report.csv", builder.ToString());
    }

    private async Task DownloadFile(string fileName, string csvData)
    {
        var bom = new byte[] { 0xEF, 0xBB, 0xBF };
        var bytes = Encoding.UTF8.GetBytes(csvData);
        var fullBytes = bom.Concat(bytes).ToArray();

        var base64 = Convert.ToBase64String(fullBytes);
        await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", fileName, "text/csv;charset=utf-8", base64);
    }

    private async Task PrintReport()
    {
        await JSRuntime.InvokeVoidAsync("printReportWindow");
    }
}

// Models
public class PaymentReportItem
{
    public string StudentName { get; set; } = null!;
    public string RollNo { get; set; } = null!;
    public string Major { get; set; } = null!;
    public string? AcademicYear { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
}

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
    public decimal AmountPaid { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
}