using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Smart_Campus_PUMUB.BlazorServer.Frontend.Services;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.Components.Admin.Pages.Book;

public partial class Page_BookEdit : ComponentBase
{
    [Parameter] public int BookId { get; set; }
    [Inject] public HttpClientService HttpClientService { get; set; } = null!;
    [Inject] public NavigationManager NavigationManager { get; set; } = null!;

    private BookUpdateRequestModel bookRequest = new();
    private List<CategoryModel> CategoryList { get; set; } = new();
    private IBrowserFile? selectedImage;
    private IBrowserFile? selectedPdf;
    private string PreviewImageUrl = "";
    private string PdfFileName = "";
    private string ExistingPdfPath = "";
    private string statusMessage = "";
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            CategoryList = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get) ?? new();

            var book = await HttpClientService.ExecuteAsync<BookModel>($"book/{BookId}", EnumHttpMethod.Get);
            if (book != null)
            {
                bookRequest.CategoryId = book.CategoryId;
                bookRequest.BookName = book.BookName;
                bookRequest.ExistingImage = book.Image;
                ExistingPdfPath = book.FilePath ?? "";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing book edit: {ex.Message}");
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        try
        {
            selectedImage = e.File;
            if (selectedImage == null) return;

            if (selectedImage.Size > 10_000_000)
            {
                statusMessage = "Image size must be under 10MB.";
                selectedImage = null;
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
            statusMessage = "Unable to process selected image.";
        }
    }

    private void HandlePdfSelected(InputFileChangeEventArgs e)
    {
        try
        {
            selectedPdf = e.File;
            if (selectedPdf == null) return;

            if (selectedPdf.Size > 50_000_000)
            {
                statusMessage = "PDF size must be under 50MB.";
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
            statusMessage = "Unable to process selected PDF.";
        }
    }

    private async Task UpdateBook()
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
        statusMessage = "Updating...";
        StateHasChanged();

        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(bookRequest.CategoryId.ToString()), nameof(bookRequest.CategoryId));
            content.Add(new StringContent(bookRequest.BookName.Trim()), nameof(bookRequest.BookName));

            if (selectedImage != null)
            {
                using var ms = new MemoryStream();
                await selectedImage.OpenReadStream(maxAllowedSize: 10_000_000).CopyToAsync(ms);
                content.Add(new ByteArrayContent(ms.ToArray()), "ImageFile", selectedImage.Name);
            }

            if (selectedPdf != null)
            {
                using var pdfMs = new MemoryStream();
                await selectedPdf.OpenReadStream(maxAllowedSize: 50_000_000).CopyToAsync(pdfMs);
                content.Add(new ByteArrayContent(pdfMs.ToArray()), "PdfFile", selectedPdf.Name);
            }

            var response = await HttpClientService.ExecuteMultipartAsync<ActionResponseModel>($"book/update/{BookId}", content);
            if (response?.IsSuccess == true)
            {
                NavigationManager.NavigateTo("/admin/books");
            }
            else
            {
                statusMessage = response?.Message ?? "Update failed.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateBook error: {ex}");
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }
}