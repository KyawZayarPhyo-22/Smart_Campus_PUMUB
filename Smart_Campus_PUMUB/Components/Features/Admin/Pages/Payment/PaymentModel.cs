using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

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

public class PaymentFeeModel
{
    public int FeesId { get; set; }
    public string? ClassYear { get; set; }
    public string? FeeName { get; set; }
    public decimal MontlyAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
}

public class PaymentFeeCreateRequestModel
{
    [Required(ErrorMessage = "Class Year သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public string? ClassYear { get; set; }
    public string? FeeName { get; set; }
    [Required(ErrorMessage = "Monthly Amount သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public decimal MontlyAmount { get; set; }
    public string? Status { get; set; } = "Active";
    public string? CreatedBy { get; set; }
}

public class PaymentFeeUpdateRequestModel
{
    [Required(ErrorMessage = "Class Year သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public string? ClassYear { get; set; }
    public string? FeeName { get; set; }
    [Required(ErrorMessage = "Monthly Amount သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public decimal MontlyAmount { get; set; }
    public string? Status { get; set; }
    public string? ModifiedBy { get; set; }
}

public class PaymentFeeResponseModel : ActionResponseModel
{
    public PaymentFeeModel? Data { get; set; }
}

public class KpayPrecreateRequestModel
{
    public int RegistrationId { get; set; }
    public decimal Amount { get; set; }
    public string? Title { get; set; }
    public string? CreatedBy { get; set; }
}

public class KpayPrecreateResponseModel
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string TxnId { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int PaymentId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? MockPaymentUrl { get; set; }
}

public class KpayCallbackRequestModel
{
    public string? OrderId { get; set; }
    public string? MerchOrderId { get; set; }
    public string? TxnId { get; set; }
    public string? Status { get; set; }
    public string? TradeStatus { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Sign { get; set; }
}

public class PaymentStatusCheckResponseModel
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsPaid { get; set; }
    public int PaymentId { get; set; }
    public int RegistrationId { get; set; }
    public string Message { get; set; } = string.Empty;
}