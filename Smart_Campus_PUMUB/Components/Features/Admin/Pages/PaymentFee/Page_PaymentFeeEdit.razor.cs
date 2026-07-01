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
    public partial class Page_PaymentFeeEdit
    {
        [Parameter] public int Id { get; set; }

        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public NavigationManager NavigationManager { get; set; } = null!;

        [SupplyParameterFromForm]
        private PaymentFeeUpdateRequestModel feeModel { get; set; } = new();

        private List<SemesterModel> SemesterList { get; set; } = new();
        private string statusMessage = "";
        private bool isProcessing = false;
        private bool IsLoading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            try
            {
                // Load semesters
                var sems = await HttpClientService.ExecuteAsync<List<SemesterModel>>("Semester", EnumHttpMethod.Get);
                if (sems != null)
                {
                    SemesterList = sems;
                }

                // Load existing fee data
                var response = await HttpClientService.ExecuteAsync<PaymentFeeModel>($"payment-fees/{Id}", EnumHttpMethod.Get);
                if (response != null)
                {
                    feeModel.ClassYear = response.ClassYear;
                    feeModel.FeeName = response.FeeName;
                    feeModel.MontlyAmount = response.MontlyAmount;
                    feeModel.Status = response.Status;
                }
            }
            catch (Exception ex)
            {
                statusMessage = $"Error loading fee data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateFee()
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
            statusMessage = "ပြင်ဆင်နေပါသည်...";

            try
            {
                var response = await HttpClientService.ExecuteAsync<PaymentFeeResponseModel>($"payment-fees/{Id}", EnumHttpMethod.Put, feeModel);
                if (response != null && response.IsSuccess)
                {
                    NavigationManager.NavigateTo("/admin/payment-fees");
                }
                else
                {
                    statusMessage = response?.Message ?? "ပြင်ဆင်မှု မအောင်မြင်ပါ။";
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
