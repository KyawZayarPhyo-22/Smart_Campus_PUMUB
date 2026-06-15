using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Payment;

public partial class Page_RegistrationPaymentList : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;

    private List<RegistrationPaymentModel> PaymentList { get; set; } = new();
    private string SearchTerm { get; set; } = "";
    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;

    // Delete Modal 
    private bool ShowModal { get; set; } = false;
    private RegistrationPaymentModel? SelectedPayment { get; set; }

    // 💡 View Details & Status Change Modal
    private bool ShowDetailModal { get; set; } = false;
    private RegistrationPaymentModel? SelectedPaymentDetail { get; set; }

    private IEnumerable<RegistrationPaymentModel> FilteredPayments => string.IsNullOrWhiteSpace(SearchTerm)
        ? PaymentList
        : PaymentList.Where(p =>
            (p.PaymentMethod != null && p.PaymentMethod.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
            (p.RegistrationId.ToString().Contains(SearchTerm))
        );

    protected override async Task OnInitializedAsync() => await LoadPayments();

    private async Task LoadPayments()
    {
        IsLoading = true;
        try
        {
            var response = await HttpClientService.ExecuteAsync<List<RegistrationPaymentModel>>("registrationpayment", EnumHttpMethod.Get);
            if (response != null) PaymentList = response;
        }
        catch (Exception ex) { await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    // ── 💡 View Details Modal Actions ──
    private async Task OpenDetailModal(int paymentId)
    {
        try
        {
            // List ထဲကနေပဲ အသေးစိတ်အချက်အလက်ကို ပြန်ဆွဲယူပါသည်
            var payment = PaymentList.FirstOrDefault(x => x.PaymentId == paymentId);
            if (payment != null)
            {
                SelectedPaymentDetail = payment;
                ShowDetailModal = true;
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", $"Error: {ex.Message}");
        }
    }

    private void CloseDetailModal()
    {
        SelectedPaymentDetail = null;
        ShowDetailModal = false;
    }

    // ── 💡 Approve / Reject API Patch Call ──
    private async Task UpdatePaymentStatus(int paymentId, string status)
    {
        bool confirm = await JSRuntime.InvokeAsync<bool>("confirm", $"ဤငွေသွင်းမှတ်တမ်းကို '{status}' အဖြစ် ပြောင်းလဲရန် သေချာပါသလား ကိုကို?");
        if (!confirm) return;

        IsProcessing = true;
        try
        {
            // API သို့ Status Patch လှမ်းလုပ်မည့် Object
            var patchModel = new
            {
                Status = status,
                modified_by = "Admin_Panel"
            };

            var response = await HttpClientService.ExecuteAsync<RegistrationPaymentResponseModel>(
                $"registrationpayment/{paymentId}/status",
                EnumHttpMethod.Patch,
                patchModel
            );

            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", $"Status updated to {status} successfully!");
                CloseDetailModal();
                await LoadPayments();
            }
            else
            {
                await JSRuntime.InvokeVoidAsync("alert", response?.Message ?? "ပြောင်းလဲမှု မအောင်မြင်ပါ။");
            }
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", ex.Message);
        }
        finally { IsProcessing = false; }
    }

    // ── Delete Modal Actions ──
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