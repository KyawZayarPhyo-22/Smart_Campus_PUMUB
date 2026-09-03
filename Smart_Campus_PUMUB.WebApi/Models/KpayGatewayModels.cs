using System;

namespace Smart_Campus_PUMUB.WebApi.Models
{
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
}
