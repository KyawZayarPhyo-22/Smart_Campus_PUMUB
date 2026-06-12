using Microsoft.AspNetCore.Http;
using System;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class RegistrationPaymentCreateRequestModel
{
    public int RegistrationId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public IFormFile ReceiptImage { get; set; } = null!; // 💡 File Upload ရန်
    public string? CreatedBy { get; set; } // ဘယ်ကျောင်းသား တင်လိုက်လဲဆိုတဲ့ UserId
}

public class RegistrationPaymentUpdateRequestModel
{
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public IFormFile? ReceiptImage { get; set; } // ပြင်ချင်မှ ပြင်မှာမို့ Nullable ထားသည်
    public string? ModifiedBy { get; set; }
}

public class RegistrationPaymentVerifyRequestModel
{
    public string Status { get; set; } = null!; // Approved သို့မဟုတ် Rejected
    public int? VerifyBy { get; set; } // 💡 စစ်ဆေးပေးသည့် Staff/Admin ရဲ့ User_Id
}

public class RegistrationPaymentResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public RegistrationPaymentModel? Data { get; set; }
}

public class RegistrationPaymentModel
{
    public int PaymentId { get; set; }
    public int RegistrationId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public string ReceiptImage { get; set; } = null!; // Image URL Path
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = "Pending";
    public int? VerifyBy { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
    public string? ModifiedBy { get; set; }
}