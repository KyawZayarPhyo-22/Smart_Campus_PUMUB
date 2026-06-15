using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Payment;

public partial class Page_PaymentEdit
{
    [Parameter] public int Id { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private RegistrationPaymentUpdateRequestModel paymentModel = new();
    private string statusMessage = "";
    private bool IsLoading = true;
    private bool isProcessing = false;
    private string? PreviewImageUrl { get; set; }
    private IBrowserFile? selectedFile;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await HttpClientService.ExecuteAsync<RegistrationPaymentModel>($"registrationpayment/{Id}", EnumHttpMethod.Get);
            if (response != null)
            {
                paymentModel.AmountPaid = response.AmountPaid;
                paymentModel.PaymentMethod = response.PaymentMethod;
                PreviewImageUrl = $"https://localhost:7297/{response.ReceiptImage}";
            }
        }
        catch (Exception ex) { statusMessage = $"Error: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        if (selectedFile != null)
        {
            using var ms = new MemoryStream();
            await selectedFile.OpenReadStream(maxAllowedSize: 1024 * 1024 * 5).CopyToAsync(ms);
            PreviewImageUrl = $"data:{selectedFile.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
        }
    }

    private async Task UpdatePayment()
    {
        isProcessing = true;
        statusMessage = "သိမ်းဆည်းနေပါသည်...";

        try
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(paymentModel.AmountPaid.ToString()), "AmountPaid");
            content.Add(new StringContent(paymentModel.PaymentMethod ?? ""), "PaymentMethod");

            if (selectedFile != null)
            {
                var streamContent = new StreamContent(selectedFile.OpenReadStream(maxAllowedSize: 1024 * 1024 * 5));
                content.Add(streamContent, "ReceiptImage", selectedFile.Name);
            }

            // API endpoint အတိုင်း ပြင်ဆင်သုံးပါ
// Method ကို HttpMethod.Post သို့ပြောင်းပြီး endpoint ကို အထက်ပါအတိုင်း ပြင်ပါ
var response = await HttpClientService.ExecuteMultipartAsync<RegistrationPaymentResponseModel>(
    $"registrationpayment/update/{Id}", 
    content);
            if (response != null && response.IsSuccess)
            {
                await JSRuntime.InvokeVoidAsync("alert", "ပြင်ဆင်မှု အောင်မြင်ပါသည်။");
                NavigationManager.NavigateTo("/admin/payments");
            }
            else { statusMessage = response?.Message ?? "ပြင်ဆင်၍မရပါ။"; }
        }
        catch (Exception ex) { statusMessage = $"Error: {ex.Message}"; }
        finally { isProcessing = false; }
    }
}