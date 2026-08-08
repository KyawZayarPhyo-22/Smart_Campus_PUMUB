using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Book;

public partial class Page_BookCreate : ComponentBase
{
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private BookCreateRequestModel bookRequest = new();
    private List<CategoryModel> CategoryList { get; set; } = new();

    private IBrowserFile? selectedImage;
    private IBrowserFile? selectedPdf;
    private string PreviewImageUrl = "";
    private string PdfFileName = "";
    private string statusMessage = "";
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            CategoryList = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading categories: {ex.Message}");
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        try
        {
            selectedImage = e.File;
            if (selectedImage == null) return;

            if (selectedImage.Size > 10_000_000) // 10MB
            {
                statusMessage = "Image file size must be less than 10MB.";
                selectedImage = null;
                PreviewImageUrl = "";
                return;
            }

            var format = "image/png";
            var resizedImage = await selectedImage.RequestImageFileAsync(format, 400, 400);
            using var ms = new MemoryStream();
            await resizedImage.OpenReadStream(maxAllowedSize: 10_000_000).CopyToAsync(ms);
            PreviewImageUrl = $"data:{format};base64,{Convert.ToBase64String(ms.ToArray())}";
            statusMessage = "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Image select error: {ex.Message}");
            statusMessage = "Unable to process selected image file.";
        }
    }

    private void HandlePdfSelected(InputFileChangeEventArgs e)
    {
        try
        {
            selectedPdf = e.File;
            if (selectedPdf == null) return;

            if (selectedPdf.Size > 50_000_000) // 50MB limit
            {
                statusMessage = "PDF file size must be less than 50MB.";
                selectedPdf = null;
                PdfFileName = "";
                return;
            }

            PdfFileName = selectedPdf.Name;
            statusMessage = "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"PDF select error: {ex.Message}");
            statusMessage = "Unable to process selected PDF file.";
        }
    }

    private async Task SaveBook()
    {
        if (bookRequest.CategoryId <= 0)
        {
            statusMessage = "Please select a Category.";
            return;
        }

        if (string.IsNullOrWhiteSpace(bookRequest.BookName))
        {
            statusMessage = "Please enter Book Name.";
            return;
        }

        isProcessing = true;
        statusMessage = "Saving book...";
        StateHasChanged();

        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(bookRequest.CategoryId.ToString()), nameof(bookRequest.CategoryId));
            content.Add(new StringContent(bookRequest.BookName.Trim()), nameof(bookRequest.BookName));

            // Cover image
            if (selectedImage != null)
            {
                using var imageMs = new MemoryStream();
                await selectedImage.OpenReadStream(maxAllowedSize: 10_000_000).CopyToAsync(imageMs);
                var byteContent = new ByteArrayContent(imageMs.ToArray());
                content.Add(byteContent, "ImageFile", selectedImage.Name);
            }

            // PDF file
            if (selectedPdf != null)
            {
                using var pdfMs = new MemoryStream();
                await selectedPdf.OpenReadStream(maxAllowedSize: 50_000_000).CopyToAsync(pdfMs);
                var byteContent = new ByteArrayContent(pdfMs.ToArray());
                content.Add(byteContent, "PdfFile", selectedPdf.Name);
            }

            var response = await HttpClientService.ExecuteMultipartAsync<BookResponseModel>("book", content);
            if (response != null && response.IsSuccess)
            {
                statusMessage = "စာအုပ်အသစ်ထည့်သွင်းခြင်း အောင်မြင်ပါသည်။";
                StateHasChanged();
                await Task.Delay(800);
                NavigationManager.NavigateTo("/admin/books");
            }
            else
            {
                statusMessage = response?.Message ?? "ထည့်သွင်း၍ မရပါ။";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SaveBook exception: {ex}");
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }
}