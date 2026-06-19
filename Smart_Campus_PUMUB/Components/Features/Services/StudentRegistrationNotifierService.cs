using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services
{
    public class StudentRegistrationNotifierService
    {
        public event Func<Task>? OnRegistrationSubmitted;
        public event Func<StudentRegistrationStatusChangedEventArgs, Task>? OnRegistrationStatusChanged;

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
    }

    public sealed record StudentRegistrationStatusChangedEventArgs(int RegistrationId, int? UserId, string Status);
}
