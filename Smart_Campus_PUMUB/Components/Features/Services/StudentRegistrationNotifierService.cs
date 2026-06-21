using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services
{
    public class StudentRegistrationNotifierService
    {
        public event Func<Task>? OnRegistrationSubmitted;
        public event Func<StudentRegistrationStatusChangedEventArgs, Task>? OnRegistrationStatusChanged;
        public event Func<StudentPaymentStatusChangedEventArgs, Task>? OnPaymentStatusChanged;

        public async Task NotifyRegistrationSubmitted()
        {
            if (OnRegistrationSubmitted != null)
            {
                await OnRegistrationSubmitted.Invoke();
            }
        }

        public async Task NotifyRegistrationStatusChanged(int registrationId, int? userId, string status)
        {
            if (OnRegistrationStatusChanged != null)
            {
                await OnRegistrationStatusChanged.Invoke(new StudentRegistrationStatusChangedEventArgs(registrationId, userId, status));
            }
        }

        public async Task NotifyPaymentStatusChanged(int paymentId, int? userId, string status)
        {
            if (OnPaymentStatusChanged != null)
            {
                await OnPaymentStatusChanged.Invoke(new StudentPaymentStatusChangedEventArgs(paymentId, userId, status));
            }
        }
    }

    public sealed record StudentRegistrationStatusChangedEventArgs(int RegistrationId, int? UserId, string Status);
    public sealed record StudentPaymentStatusChangedEventArgs(int PaymentId, int? UserId, string Status);
}
