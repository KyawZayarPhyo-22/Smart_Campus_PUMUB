using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Payment;

public partial class Page_PaymentCreate
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager Nav { get; set; } = null!;

    private RegistrationPaymentCreateRequestModel createModel = new() { PaymentMethod = "Cash" };
    private IBrowserFile? selectedFile;
    private string? PreviewImageUrl;
    private string statusMessage = "";
    private bool isProcessing = false;

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        // Image Preview အတွက်
        var format = "image/png";
        var resizedImage = await selectedFile.RequestImageFileAsync(format, 300, 300);
        using var memoryStream = new MemoryStream();
        await resizedImage.OpenReadStream().CopyToAsync(memoryStream);
        var imageUrl = $"data:{format};base64,{Convert.ToBase64String(memoryStream.ToArray())}";
        PreviewImageUrl = imageUrl;
    }

    private async Task HandleCreate()
    {
        if (selectedFile == null) { statusMessage = "ကျေးဇူးပြု၍ ပြေစာပုံ တင်ပေးပါ။"; return; }

        isProcessing = true;
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(createModel.RegistrationId.ToString()), "RegistrationId");
        content.Add(new StringContent(createModel.AmountPaid.ToString()), "AmountPaid");
        content.Add(new StringContent(createModel.PaymentMethod), "PaymentMethod");
        
        var fileContent = new StreamContent(selectedFile.OpenReadStream(1024 * 1024 * 5));
        content.Add(fileContent, "ReceiptImage", selectedFile.Name);

        var response = await HttpClientService.ExecuteMultipartAsync<RegistrationPaymentResponseModel>("registrationpayment", content);

        if (response != null && response.IsSuccess) Nav.NavigateTo("/admin/payments");
        else { statusMessage = response?.Message ?? "Error occurred."; isProcessing = false; }
    }
}