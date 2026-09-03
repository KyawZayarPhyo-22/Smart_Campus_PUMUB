using System.Threading.Tasks;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Services
{
    public interface IKpayGatewayService
    {
        Task<KpayPrecreateResponseModel> PrecreateQrPaymentAsync(KpayPrecreateRequestModel request);
    }
}
