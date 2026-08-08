using System.Collections.Generic;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class StudentRegistrationDataModel
{
    public int RegistrationId { get; set; }
    public string StudentNameMm { get; set; } = null!;
    public string Major { get; set; } = null!;
    public string? FacultyName { get; set; }
    public string RollNo { get; set; } = null!;
    public string AcademicYearLevel { get; set; } = null!;
    public DateTime CreatedDatetime { get; set; }
    public string Status { get; set; } = null!;

    // 💡 API က Include နဲ့ တွဲပို့လိုက်တဲ့ Payment ဒေတာတွေကို ဖမ်းယူမည့် List
    public List<RegistrationPaymentModel> RegistrationPayments { get; set; } = new();
}