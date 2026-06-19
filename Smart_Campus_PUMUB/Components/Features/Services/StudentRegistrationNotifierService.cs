using System;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.BlazorServer.Frontend.Services
{
    public class StudentRegistrationNotifierService
    {
        public event Func<Task>? OnRegistrationSubmitted;

        public async Task NotifyRegistrationSubmitted()
        {
            if (OnRegistrationSubmitted != null)
            {
                await OnRegistrationSubmitted.Invoke();
            }
        }
    }
}
