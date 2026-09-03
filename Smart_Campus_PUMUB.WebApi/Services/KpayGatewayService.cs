using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Services
{
    public class KpayGatewayService : IKpayGatewayService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<KpayGatewayService> _logger;

        public KpayGatewayService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<KpayGatewayService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<KpayPrecreateResponseModel> PrecreateQrPaymentAsync(KpayPrecreateRequestModel request)
        {
            var gatewaySection = _configuration.GetSection("PaymentGateway");
            var baseUrl = gatewaySection["BaseUrl"] ?? "http://localhost:5283";
            var mockServerUrl = gatewaySection["MockServerUrl"] ?? "http://localhost:5028";
            var merchantId = gatewaySection["MerchantId"] ?? "M000007";
            var merchCode = gatewaySection["MerchCode"] ?? "10000";
            var appId = gatewaySection["AppId"] ?? "kp1234567890";
            var appKey = gatewaySection["AppKey"] ?? "mysecretkey";
            var serviceType = gatewaySection["ServiceType"] ?? "Kpay:Payment";
            var transCurrency = gatewaySection["TransCurrency"] ?? "MMK";
            var timeoutExpress = gatewaySection["TimeoutExpress"] ?? "100m";

            var orderId = $"REG{request.RegistrationId}_{DateTime.UtcNow:yyyyMMddHHmmss}";
            var txnId = "kp" + Guid.NewGuid().ToString("N")[2..];
            var title = string.IsNullOrWhiteSpace(request.Title) ? "Student Registration Fee" : request.Title;

            var payload = new
            {
                TxnId = txnId,
                MerchantId = merchantId,
                OrderId = orderId,
                ServiceType = serviceType,
                Data = new
                {
                    MerchOrderId = orderId,
                    MerchCode = merchCode,
                    AppId = appId,
                    TradeType = "PAY_BY_QRCODE",
                    Title = title,
                    TotalAmount = request.Amount.ToString("0.00"),
                    TransCurrency = transCurrency,
                    TimeoutExpress = timeoutExpress,
                    CallbackInfo = $"RegistrationId={request.RegistrationId}",
                    AppKey = appKey
                }
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            string qrCodeString = string.Empty;
            string returnedTxnId = txnId;

            try
            {
                _logger.LogInformation("Calling Payment Gateway at {Url}/api/payment/pay for OrderId {OrderId}", baseUrl, orderId);
                var response = await _httpClient.PostAsync($"{baseUrl}/api/payment/pay", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Payment Gateway Response: {Response}", responseString);

                    var responseJObj = JsonConvert.DeserializeObject<JObject>(responseString);
                    if (responseJObj != null)
                    {
                        var dataObj = responseJObj["data"] as JObject ?? responseJObj["Data"] as JObject;
                        qrCodeString = dataObj?["qrCode"]?.ToString() 
                                     ?? dataObj?["QrCode"]?.ToString() 
                                     ?? responseJObj["qrCode"]?.ToString() 
                                     ?? responseJObj["QrCode"]?.ToString() 
                                     ?? string.Empty;

                        var resTxn = responseJObj["txnId"]?.ToString() ?? responseJObj["TxnId"]?.ToString();
                        if (!string.IsNullOrEmpty(resTxn))
                        {
                            returnedTxnId = resTxn;
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Payment Gateway returned non-success code {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not reach Payment Gateway directly. Generating standard MMQR fallback string.");
            }

            // If gateway didn't return a QR code string or was unreachable, generate standard MMQR/Mock QR string
            if (string.IsNullOrEmpty(qrCodeString))
            {
                qrCodeString = $"00020101021229370016A0000007270401200113{merchantId}520460115303104540{request.Amount:0.00}5802MM5911SmartCampus6006YANGON62280124{orderId}6304";
            }

            var mockUrl = $"{mockServerUrl}/Kpay?merchOrderId={orderId}&totalAmount={request.Amount}&tradeType=PAY_BY_QRCODE&title={Uri.EscapeDataString(title)}";

            return new KpayPrecreateResponseModel
            {
                IsSuccess = true,
                Message = "QR Code Generated Successfully",
                OrderId = orderId,
                TxnId = returnedTxnId,
                QrCode = qrCodeString,
                Amount = request.Amount,
                Status = "Pending",
                MockPaymentUrl = mockUrl
            };
        }
    }
}
