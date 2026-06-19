using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
using System.IO;

namespace NLADotNetInternshipTraining.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookController : ControllerBase
{
    private readonly SmartCampusDbContext _db;
    private readonly IWebHostEnvironment _env;

    public BookController(SmartCampusDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public IActionResult GetBooks()
    {
        var lst = _db.Books
            .AsNoTracking()
            .Include(b => b.Category) // 👈 ဒီ Include လေး ထည့်လိုက်ရုံပါပဲ
            .Where(x => x.IsDelete == false || x.IsDelete == null)
            .OrderByDescending(x => x.CreatedDateTime)
.Select(x => new BookModel
{
    BookId = x.BookId,
    BookName = x.BookName,
    Image = x.Image,
    CategoryId = x.CategoryId,
    CategoryName = x.Category != null ? x.Category.CategoryName : "N/A"
})
            .ToList();

        return Ok(lst);
    }

    [HttpGet("{id}")]
    public IActionResult GetBookById(int id)
    {
        var book = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (book == null) return NotFound();
        return Ok(book);
    }

    [HttpPost]
    public IActionResult CreateBook([FromForm] BookCreateRequestModel request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var category = _db.Categories.FirstOrDefault(c => c.CategoryId == request.CategoryId);
        if (category is null || category.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Category ID ရှာမတွေ့ပါ။" });

        string? dbImagePath = null;

        if (request.ImageFile != null)
        {
            var extension = Path.GetExtension(request.ImageFile.FileName).ToLower();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = ".jpg , .png သို့မဟုတ် .jpeg ဖိုင်အမျိုးအစားသာ လက်ခံပါသည်။" });
            }

            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            // Guid ကိုသုံးပြီး နာမည်သစ်ပေးခြင်း (Special Characters ပြဿနာဖြေရှင်းရန်)
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                request.ImageFile.CopyTo(fileStream);
            }

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

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "New Book Uploaded",
            Description = $"{request.BookName.Trim()} was added to the Library.",
            CreatedDateTime = DateTime.UtcNow
        });

        _db.SaveChanges();
        return StatusCode(201, new ActionResponseModel { IsSuccess = true, Message = "Book created successfully!" });
    }

    [HttpPost("update/{id}")]
    public IActionResult UpdateBook(int id, [FromForm] BookUpdateRequestModel request)
    {
        var item = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null) return NotFound(new ActionResponseModel { IsSuccess = false, Message = "Book not found" });

        if (request.ImageFile != null)
        {
            var extension = Path.GetExtension(request.ImageFile.FileName).ToLower();
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                request.ImageFile.CopyTo(fileStream);
            }

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

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "Book Updated",
            Description = $"{item.BookName.Trim()} was updated.",
            CreatedDateTime = DateTime.UtcNow
        });

        _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Update Successful" });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteBook(int id)
    {
        var item = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null) return NotFound();

        item.IsDelete = true;
        _db.Activities.Add(new Activity
        {
            ActivityTitle = "Book Deleted",
            Description = $"{item.BookName.Trim()} was deleted.",
            CreatedDateTime = DateTime.UtcNow
        });

        _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Delete Successfully" });
    }

    [HttpGet("count/active")]
    public async Task<IActionResult> GetActiveBookCount()
    {
        int count = await _db.Books.CountAsync(x => x.IsDelete == false || x.IsDelete == null);
        return Ok(new { Count = count });
    }
}