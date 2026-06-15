using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Payment;

public partial class Page_RegistrationPaymentList
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<RegistrationPaymentModel> PaymentList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool ShowModal { get; set; } = false;
    private RegistrationPaymentModel? SelectedPayment { get; set; }

    // Search Logic (PaymentMethod ကို ရှာဖွေခြင်း)
    private IEnumerable<RegistrationPaymentModel> FilteredPayments => string.IsNullOrWhiteSpace(SearchTerm)
        ? PaymentList
        : PaymentList.Where(p => p.PaymentMethod != null && p.PaymentMethod.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync() => await LoadPayments();

    private async Task LoadPayments()
    {
        IsLoading = true;
        try
        {
            // API Path: "registrationpayment"
            var response = await HttpClientService.ExecuteAsync<List<RegistrationPaymentModel>>("registrationpayment", EnumHttpMethod.Get);
            if (response != null) PaymentList = response;
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    private void OpenDeleteModal(RegistrationPaymentModel payment) 
    { 
        SelectedPayment = payment; 
        ShowModal = true; 
    }
    
    private void CloseDeleteModal() { SelectedPayment = null; ShowModal = false; }

    private async Task DeletePayment()
    {
        if (SelectedPayment == null) return;
        IsProcessing = true;
        try
        {
            // DELETE: api/registrationpayment/{id}
            var response = await HttpClientService.ExecuteAsync<RegistrationPaymentResponseModel>($"registrationpayment/{SelectedPayment.PaymentId}", EnumHttpMethod.Delete);
            
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", "ဖျက်သိမ်းမှု အောင်မြင်ပါသည်။");
                CloseDeleteModal();
                await LoadPayments();
            }
            else { await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ဖျက်သိမ်း၍ မရပါ။"); }
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
        finally { IsProcessing = false; }
    }
}