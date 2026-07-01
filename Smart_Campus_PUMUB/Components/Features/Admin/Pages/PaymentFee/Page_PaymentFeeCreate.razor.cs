using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.PaymentFee
{
    public partial class Page_PaymentFeeCreate
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public NavigationManager NavigationManager { get; set; } = null!;

        [SupplyParameterFromForm]
        private PaymentFeeCreateRequestModel feeModel { get; set; } = new() { Status = "Active" };

        private List<SemesterModel> SemesterList { get; set; } = new();
        private string statusMessage = "";
        private bool isProcessing = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var sems = await HttpClientService.ExecuteAsync<List<SemesterModel>>("Semester", EnumHttpMethod.Get);
                if (sems != null)
                {
                    SemesterList = sems;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading semesters: {ex.Message}");
            }
        }

        private async Task SaveFee()
        {
            if (string.IsNullOrWhiteSpace(feeModel.ClassYear))
            {
                statusMessage = "Semester / Class Year ရွေးချယ်ရန် လိုအပ်ပါသည်။";
                return;
            }

            if (string.IsNullOrWhiteSpace(feeModel.FeeName))
            {
                statusMessage = "Fee Name ဖြည့်စွက်ရန် လိုအပ်ပါသည်။";
                return;
            }

            if (feeModel.MontlyAmount <= 0)
            {
                statusMessage = "Amount သည် သုညထက် ကြီးရပါမည်။";
                return;
            }

            isProcessing = true;
            statusMessage = "သိမ်းဆည်းနေပါသည်...";

            try
            {
                var response = await HttpClientService.ExecuteAsync<ActionResponseModel>("payment-fees", EnumHttpMethod.Post, feeModel);
                if (response != null && response.IsSuccess)
                {
                    NavigationManager.NavigateTo("/admin/payment-fees");
                }
                else
                {
                    statusMessage = response?.Message ?? "သိမ်းဆည်းမှု မအောင်မြင်ပါ။";
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("400"))
                {
                    statusMessage = "ဤ Semester တွင် ဤ Fee Name သတ်မှတ်ပြီးသား ဖြစ်နေပါသည်။";
                }
                else
                {
                    statusMessage = $"Error: {ex.Message}";
                }
            }
            finally
            {
                isProcessing = false;
            }
        }
    }
}
