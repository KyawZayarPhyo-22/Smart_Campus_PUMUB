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
    private string PreviewImageUrl = "";
    private string statusMessage = "";
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        // Category စာရင်းကို API မှ ဆွဲယူခြင်း
        CategoryList = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get) ?? new();
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        selectedImage = e.File;
        // Image Preview အတွက်
        var format = "image/png";
        var resizedImage = await selectedImage.RequestImageFileAsync(format, 400, 400);
        var buffer = new byte[resizedImage.Size];
        await resizedImage.OpenReadStream(maxAllowedSize: 2097152).ReadAsync(buffer); // 2MB limit
        PreviewImageUrl = $"data:{format};base64,{Convert.ToBase64String(buffer)}";
    }

    private async Task SaveBook()
    {
        isProcessing = true;
        statusMessage = "Processing...";

        try
        {
            // FormData ကို တည်ဆောက်ခြင်း
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(bookRequest.CategoryId.ToString()), nameof(bookRequest.CategoryId));
            content.Add(new StringContent(bookRequest.BookName ?? ""), nameof(bookRequest.BookName));
            content.Add(new StringContent(bookRequest.FilePath ?? ""), nameof(bookRequest.FilePath));

            if (selectedImage != null)
            {
                var ms = new MemoryStream();
                await selectedImage.OpenReadStream(maxAllowedSize: 2097152).CopyToAsync(ms);
                content.Add(new ByteArrayContent(ms.ToArray()), "ImageFile", selectedImage.Name);
            }

            // API ခေါ်ဆိုခြင်း
            var response = await HttpClientService.ExecuteMultipartAsync<BookResponseModel>("book", content);
            if (response != null && response.IsSuccess)
            {
                statusMessage = "စာအုပ်အသစ်ထည့်သွင်းခြင်း အောင်မြင်ပါသည်။";
                await Task.Delay(1000); // User မြင်အောင် ခဏစောင့်ပေးခြင်း
                NavigationManager.NavigateTo("/admin/books");
            }
            else
            {
                statusMessage = response?.Message ?? "ထည့်သွင်း၍ မရပါ။";
            }
        }
        catch (Exception ex)
        {
            statusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }
}