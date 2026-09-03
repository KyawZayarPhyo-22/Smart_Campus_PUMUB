using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.PaymentFee
{
    public partial class Page_PaymentFeeList : IDisposable
    {
        [Inject] public HttpClientService HttpClientService { get; set; } = null!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] public Smart_Campus_PUMUB.Components.Features.Services.AdminLanguageService LangService { get; set; } = null!;

        private List<PaymentFeeModel> FeeList { get; set; } = new();
        private string SearchTerm { get; set; } = "";
        private bool IsLoading { get; set; } = true;
        private string ErrorMessage { get; set; } = "";
        private bool IsProcessing { get; set; } = false;

        // Permissions Variables
        private List<string> userPermissions = new();
        private bool canManagePaymentFee = true;

        private string SearchInput = "";

        private async Task ApplyFilter()
        {
            SearchTerm = SearchInput;
            CurrentPage = 1;
            await LoadFees();
        }

        private async Task ResetFilter()
        {
            SearchInput = "";
            SearchTerm = "";
            CurrentPage = 1;
            await LoadFees();
        }

        private async Task HandleKeyUp(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await ApplyFilter();
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

        private IEnumerable<PaymentFeeModel> FilteredFees => FeeList;

        private async Task OnPageChanged(int newPage)
        {
            CurrentPage = newPage;
            await LoadFees();
        }

        protected override async Task OnInitializedAsync()
        {
            LangService.OnLanguageChanged += StateHasChanged;
            await LoadFees();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                var savedLang = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_dashboard_lang");
                if (!string.IsNullOrEmpty(savedLang))
                {
                    LangService.SetLanguage(savedLang);
                }
            }
            catch { }

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                userPermissions = user.Claims
                                      .Where(c => c.Type == "Permission")
                                      .Select(c => c.Value)
                                      .ToList();
                                      
                canManagePaymentFee = userPermissions.Contains("PaymentFee.Edit") || userPermissions.Contains("PaymentFee.Delete");

                StateHasChanged();
            }
        }

        private async Task LoadFees()
        {
            IsLoading = true;
            ErrorMessage = "";
            StateHasChanged();
            try
            {
                var delayTask = Task.Delay(500);
                var fetchTask = HttpClientService.ExecuteAsync<PagedResult<PaymentFeeModel>>(
                    $"payment-fees/paginate?pageNumber={CurrentPage}&pageSize={PageSize}&searchTerm={Uri.EscapeDataString(SearchTerm)}", 
                    EnumHttpMethod.Get
                );

                await Task.WhenAll(fetchTask, delayTask);
                var response = await fetchTask;
                if (response != null)
                {
                    FeeList = response.Items;
                    TotalPages = response.TotalPages;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = LangService.IsMyanmar ? $"ဒေတာဆွဲယူရာတွင် အမှားရှိပါသည်။ Error: {ex.Message}" : $"Failed to load data. Error: {ex.Message}";
            }
            finally 
            { 
                IsLoading = false; 
                StateHasChanged();
            }
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
            statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းနေပါသည်..." : "Deleting...";
            IsSuccess = false;

            try
            {
                var response = await HttpClientService.ExecuteAsync<ActionResponseModel>(
                    $"payment-fees/{SelectedFee.FeesId}",
                    EnumHttpMethod.Delete
                );

                if (response != null && response.IsSuccess)
                {
                    statusMessage = LangService.IsMyanmar ? "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။" : (response.Message ?? "Deleted successfully.");
                    IsSuccess = true;

                    await Task.Delay(800);
                    CloseDeleteModal();
                    await LoadFees();
                }
                else
                {
                    statusMessage = LangService.IsMyanmar ? "ဤ နှုန်းထား ကို ဖျက်၍ မရပါ။" : (response?.Message ?? "Cannot delete this fee.");
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

        public void Dispose()
        {
            LangService.OnLanguageChanged -= StateHasChanged;
        }
    }
}
