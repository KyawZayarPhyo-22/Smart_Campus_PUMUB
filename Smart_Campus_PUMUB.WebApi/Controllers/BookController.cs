using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System.IO;

namespace NLADotNetInternshipTraining.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly IWebHostEnvironment _env; // Server ပတ်ဝန်းကျင်လမ်းကြောင်း ယူရန်

    public BookController(SmartCampusDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public IActionResult GetBooks()
    {
        var lst = _db.Books.Where(x => x.IsDelete == false || x.IsDelete == null).ToList();
        return Ok(lst);
    }
    [HttpGet("{id}")] // 👈 ဤ method အသစ်ကို ထည့်ပါ
    public IActionResult GetBookById(int id)
    {
        var book = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (book == null) return NotFound();
        return Ok(book);
    }

    [HttpPost]
    public IActionResult CreateBook([FromForm] BookCreateRequestModel request) // Role: ဖိုင်ပါဝင်သဖြင့် [FromForm] သုံးရပါမည်
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Foreign Key Validation
        var category = _db.Categories.FirstOrDefault(c => c.CategoryId == request.CategoryId);
        if (category is null || category.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Category ID ရှာမတွေ့ပါ။" });

        string? dbImagePath = null;

        // Role: .jpg ဖိုင် ဟုတ်၊ မဟုတ် စစ်ဆေးပြီး Server ပေါ် တင်ခြင်း Validation
        if (request.ImageFile != null)
        {
            var extension = Path.GetExtension(request.ImageFile.FileName).ToLower();

            // .jpg သို့မဟုတ် .jpeg သီးသန့် ဖြစ်ရမည့် Role
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = ".jpg , .png သို့မဟုတ် .jpeg ဖိုင်အမျိုးအစားသာ လက်ခံပါသဖြင့် တင်၍မရပါ။" });
            }

            // Server ပေါ်က wwwroot/uploads Folder ထဲမှာ ပုံသွားသိမ်းခြင်း
            string uploadFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            // နာမည်မထပ်အောင် GUID ဖြင့် နာမည်အသစ်ပေးခြင်း
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + request.ImageFile.FileName;
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                request.ImageFile.CopyTo(fileStream);
            }

            // Database ထဲ သိမ်းမည့် လမ်းကြောင်း
            dbImagePath = "/uploads/" + uniqueFileName;
        }

        _db.Books.Add(new Book
        {
            CategoryId = request.CategoryId,
            BookName = request.BookName.Trim(),
            Image = dbImagePath,
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.CreatedBy,
            IsDelete = false
        });

        int result = _db.SaveChanges();
        return StatusCode(201, new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Book created with .jpg image!" : "Saving Failed" });
    }

   [HttpPost("update/{id}")]
    public IActionResult UpdateBook(int id, [FromForm] BookUpdateRequestModel request)
    {
        if (id <= 0) return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Invalid ID" });
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var item = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null) return NotFound(new ActionResponseModel { IsSuccess = false, Message = "Book not found" });

        // ပုံအသစ် ထပ်တင်ခဲ့ရင်
        if (request.ImageFile != null)
        {
            var extension = Path.GetExtension(request.ImageFile.FileName).ToLower();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = ".jpg & .png ဖိုင်သာ လက်ခံပါသည်။" });
            }

            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + request.ImageFile.FileName;
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                request.ImageFile.CopyTo(fileStream);
            }

            // ပုံဟောင်းရှိရင် Server ပေါ်ကနေ ဖျက်ပစ်ခြင်း Role
            if (!string.IsNullOrEmpty(item.Image))
            {
                string oldFilePath = Path.Combine(_env.WebRootPath, item.Image.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
            }

            item.Image = "/uploads/" + uniqueFileName;
        }

        item.CategoryId = request.CategoryId;
        item.BookName = request.BookName.Trim();
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy = request.ModifiedBy;

        int result = _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = result > 0, Message = "Update Successful" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteBook(int id)
    {
        if (id <= 0) return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Invalid ID" });

        var item = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null) return NotFound(new ActionResponseModel { IsSuccess = false, Message = "Book not found" });

        item.IsDelete = true;
        int result = _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = result > 0, Message = "Delete Successfully" });
    }
}