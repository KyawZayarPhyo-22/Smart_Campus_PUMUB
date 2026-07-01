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
    public partial class Page_PaymentFeeList
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

        private List<PaymentFeeModel> FeeList { get; set; } = new();
        private string SearchTerm { get; set; } = "";
        private bool IsLoading { get; set; } = true;
        private string ErrorMessage { get; set; } = "";
        private bool IsProcessing { get; set; } = false;

        private string SearchInput = "";

        private void ApplyFilter()
        {
            SearchTerm = SearchInput;
            CurrentPage = 1;
            StateHasChanged();
        }

        private void ResetFilter()
        {
            SearchInput = "";
            SearchTerm = "";
            CurrentPage = 1;
            StateHasChanged();
        }

        private void HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                ApplyFilter();
            }
        }

        private string statusMessage = "";

        public bool IsSuccess { get; private set; }
        private bool ShowModal { get; set; } = false;
        private PaymentFeeModel? SelectedFee { get; set; }

        // Pagination Variables
        private int CurrentPage { get; set; } = 1;
        private int PageSize { get; set; } = 10;
        private int TotalPages { get; set; } = 1;

        private IEnumerable<PaymentFeeModel> GetFilteredFees()
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
            {
                return FeeList;
            }
            return FeeList.Where(f => 
                (f.ClassYear != null && f.ClassYear.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (f.FeeName != null && f.FeeName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
            );
        }

        private IEnumerable<PaymentFeeModel> FilteredFees
        {
            get
            {
                var allFiltered = GetFilteredFees();
                int count = allFiltered.Count();
                int calcPages = (int)Math.Ceiling((decimal)count / PageSize);
                TotalPages = calcPages < 1 ? 1 : calcPages;
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                return allFiltered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
            }
        }

        private void OnPageChanged(int newPage)
        {
            CurrentPage = newPage;
            StateHasChanged();
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadFees();
        }

        private async Task LoadFees()
        {
            IsLoading = true;
            ErrorMessage = "";
            try
            {
                var response = await HttpClientService.ExecuteAsync<List<PaymentFeeModel>>("payment-fees", EnumHttpMethod.Get);
                if (response != null) FeeList = response;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}";
            }
            finally { IsLoading = false; }
        }

        private void OpenDeleteModal(PaymentFeeModel fee)
        {
            SelectedFee = fee;
            ShowModal = true;
            statusMessage = "";
            IsSuccess = false;
        }

        private void CloseDeleteModal()
        {
            SelectedFee = null;
            ShowModal = false;
            statusMessage = "";
            IsSuccess = false;
        }

        private async Task DeleteFee()
        {
            if (SelectedFee == null) return;

            IsProcessing = true;
            statusMessage = "ဖျက်သိမ်းနေပါသည်...";
            IsSuccess = false;

            try
            {
                var response = await HttpClientService.ExecuteAsync<ActionResponseModel>(
                    $"payment-fees/{SelectedFee.FeesId}",
                    EnumHttpMethod.Delete
                );

                if (response != null && response.IsSuccess)
                {
                    statusMessage = "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။";
                    IsSuccess = true;

                    await Task.Delay(1500);
                    CloseDeleteModal();
                    await LoadFees();
                }
                else
                {
                    statusMessage = response?.Message ?? "ဖျက်သိမ်းမှု မအောင်မြင်ပါ။";
                    IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                statusMessage = $"Error: {ex.Message}";
                IsSuccess = false;
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }
}
