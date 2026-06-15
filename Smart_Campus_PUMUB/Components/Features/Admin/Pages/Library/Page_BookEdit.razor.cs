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
    private string PreviewImageUrl = "";
    private string statusMessage = "";
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        CategoryList = await HttpClientService.ExecuteAsync<List<CategoryModel>>("category", EnumHttpMethod.Get) ?? new();

        // Edit လုပ်ရန်အတွက် Data အဟောင်းများကို API မှ ဆွဲယူခြင်း
        var book = await HttpClientService.ExecuteAsync<BookModel>($"book/{BookId}", EnumHttpMethod.Get);
        if (book != null)
        {
            bookRequest.CategoryId = book.CategoryId;
            bookRequest.BookName = book.BookName;
            bookRequest.ExistingImage = book.Image; // UI တွင် ပြရန်အတွက်
        }
    }

    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        selectedImage = e.File;
        // Preview logic အတူတူပင်...
    }

    private async Task UpdateBook()
    {
        isProcessing = true;
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(bookRequest.CategoryId.ToString()), nameof(bookRequest.CategoryId));
        content.Add(new StringContent(bookRequest.BookName), nameof(bookRequest.BookName));

        if (selectedImage != null)
        {
            var ms = new MemoryStream();
            await selectedImage.OpenReadStream().CopyToAsync(ms);
            content.Add(new ByteArrayContent(ms.ToArray()), "ImageFile", selectedImage.Name);
        }

        // WebAPI controller ၏ [HttpPut("{id}")] သို့ ပို့ခြင်း
        // လမ်းကြောင်းကို အပြောင်းအလဲလုပ်ပါ
        var response = await HttpClientService.ExecuteMultipartAsync<ActionResponseModel>($"book/update/{BookId}", content);
        if (response?.IsSuccess == true) NavigationManager.NavigateTo("/admin/books");
        else statusMessage = response?.Message ?? "Update failed.";

        isProcessing = false;
    }
}