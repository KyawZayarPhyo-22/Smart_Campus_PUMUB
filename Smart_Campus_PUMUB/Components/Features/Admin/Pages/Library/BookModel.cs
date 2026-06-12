namespace Smart_Campus_PUMUB.WebApi.Models;

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

// ==========================================
// Shared Base Response Model
// ==========================================




// ==========================================
// ၁၀။ Book DTOs
// ==========================================
public class BookCreateRequestModel
{
    [Required(ErrorMessage = "Category ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Book Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150)]
    public string? BookName { get; set; }

    // Role: .jpg သို့မဟုတ် .png ဖိုင်ကို တိုက်ရိုက်လက်ခံရန်
    public IFormFile? ImageFile { get; set; }

    public string? CreatedBy { get; set; }
    public string? FilePath { get; set; }

}

public class BookUpdateRequestModel
{
    [Required(ErrorMessage = "Category ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Book Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150)]
    public string? BookName { get; set; }

    // Role: ပုံအသစ် ပြောင်းချင်ရင် တင်ရန်
    public IFormFile? ImageFile { get; set; }

    public string? ModifiedBy { get; set; }

    public string? ExistingImage { get; set; }
}

public class BookResponseModel : ActionResponseModel
{
    public BookModel? Data { get; set; }
}

public class BookModel
{
    public int BookId { get; set; }
    public int CategoryId { get; set; }
    public string? BookName { get; set; }
    public string? Image { get; set; }
    public string? FilePath { get; set; }
}

// ==========================================
// ၁၁။ Subject DTOs
// ==========================================
