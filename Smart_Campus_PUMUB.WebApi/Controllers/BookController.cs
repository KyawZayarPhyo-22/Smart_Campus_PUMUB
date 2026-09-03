using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;
using System.IO;
using System.Security.Permissions;

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
    [AllowAnonymous]
    public IActionResult GetBooks()
    {
        var lst = _db.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(x => x.IsDelete == false || x.IsDelete == null)
            .OrderByDescending(x => x.CreatedDateTime)
            .Select(x => new BookModel
            {
                BookId = x.BookId,
                BookName = x.BookName,
                Image = x.Image,
                FilePath = x.FilePath,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.CategoryName : "N/A"
            })
            .ToList();

        return Ok(lst);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public IActionResult GetBookById(int id)
    {
        var book = _db.Books.Include(b => b.Category)
            .FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (book == null) return NotFound();
        return Ok(new BookModel
        {
            BookId = book.BookId,
            BookName = book.BookName,
            Image = book.Image,
            FilePath = book.FilePath,
            CategoryId = book.CategoryId,
            CategoryName = book.Category?.CategoryName
        });
    }

    [HttpPost]
    [Permission("Book.Create")]
    public IActionResult CreateBook([FromForm] BookCreateRequestModel request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var category = _db.Categories.FirstOrDefault(c => c.CategoryId == request.CategoryId);
        if (category is null || category.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Category ID ရသာမတွေ့ပါ။" });

        string? dbImagePath = null;
        string? dbFilePath = null;

        // --- Cover Image ---
        if (request.ImageFile != null)
        {
            var extension = Path.GetExtension(request.ImageFile.FileName).ToLower();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = ".jpg , .png သို့မဟုတ် .jpeg ဖိုင်အမျိုအစာသာ လက်ခံပါသည်။" });

            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
                request.ImageFile.CopyTo(fileStream);

            dbImagePath = "/uploads/" + uniqueFileName;
        }

        // --- PDF File ---
        if (request.PdfFile != null)
        {
            var ext = Path.GetExtension(request.PdfFile.FileName).ToLower();
            if (ext != ".pdf")
                return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "PDF ဖိုင်အမျိုအစာသာပေးလက်ခံပါသည်။" });

            string pdfFolder = Path.Combine(_env.WebRootPath, "uploads", "pdfs");
            if (!Directory.Exists(pdfFolder)) Directory.CreateDirectory(pdfFolder);

            string uniquePdfName = Guid.NewGuid().ToString() + ".pdf";
            string pdfDiskPath = Path.Combine(pdfFolder, uniquePdfName);
            using (var fs = new FileStream(pdfDiskPath, FileMode.Create))
                request.PdfFile.CopyTo(fs);

            dbFilePath = "/uploads/pdfs/" + uniquePdfName;
        }

        _db.Books.Add(new Book
        {
            CategoryId = request.CategoryId,
            BookName = request.BookName!.Trim(),
            Image = dbImagePath,
            FilePath = dbFilePath,
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.CreatedBy,
            IsDelete = false
        });

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "New Book Uploaded",
            Description = $"{request.BookName!.Trim()} was added to the Library.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });

        _db.SaveChanges();
        return StatusCode(201, new ActionResponseModel { IsSuccess = true, Message = "Book created successfully!" });
    }

    [HttpPost("update/{id}")]
    [Permission("Book.Edit")]
    public IActionResult UpdateBook(int id, [FromForm] BookUpdateRequestModel request)
    {
        var item = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null) return NotFound(new ActionResponseModel { IsSuccess = false, Message = "Book not found" });

        // --- Cover Image ---
        if (request.ImageFile != null)
        {
            var extension = Path.GetExtension(request.ImageFile.FileName).ToLower();
            string uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
                request.ImageFile.CopyTo(fileStream);

            if (!string.IsNullOrEmpty(item.Image))
            {
                string oldFilePath = Path.Combine(_env.WebRootPath, item.Image.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
            }

            item.Image = "/uploads/" + uniqueFileName;
        }

        // --- PDF File ---
        if (request.PdfFile != null)
        {
            var ext = Path.GetExtension(request.PdfFile.FileName).ToLower();
            if (ext == ".pdf")
            {
                string pdfFolder = Path.Combine(_env.WebRootPath, "uploads", "pdfs");
                if (!Directory.Exists(pdfFolder)) Directory.CreateDirectory(pdfFolder);

                string uniquePdfName = Guid.NewGuid().ToString() + ".pdf";
                string pdfDiskPath = Path.Combine(pdfFolder, uniquePdfName);

                using (var fs = new FileStream(pdfDiskPath, FileMode.Create))
                    request.PdfFile.CopyTo(fs);

                // Delete old PDF if exists
                if (!string.IsNullOrEmpty(item.FilePath))
                {
                    string oldPdf = Path.Combine(_env.WebRootPath, item.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPdf)) System.IO.File.Delete(oldPdf);
                }

                item.FilePath = "/uploads/pdfs/" + uniquePdfName;
            }
        }

        item.CategoryId = request.CategoryId;
        item.BookName = request.BookName!.Trim();
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy = request.ModifiedBy;

        _db.Activities.Add(new Activity
        {
            ActivityTitle = "Book Updated",
            Description = $"{item.BookName.Trim()} was updated.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
        });

        _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = true, Message = "Update Successful" });
    }

    [HttpDelete("{id}")]
    [Permission("Book.Delete")]
    public IActionResult DeleteBook(int id)
    {
        var item = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null) return NotFound();

        item.IsDelete = true;
        _db.Activities.Add(new Activity
        {
            ActivityTitle = "Book Deleted",
            Description = $"{item.BookName.Trim()} was deleted.",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
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

    [HttpGet("paginate")]
    [Permission("Book.View")]
    public IActionResult GetBooksPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? categoryId = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Books
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(x => x.IsDelete == false || x.IsDelete == null);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => x.BookName != null && x.BookName.Contains(searchTerm));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x.CreatedDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BookModel
            {
                BookId = x.BookId,
                BookName = x.BookName,
                Image = x.Image,
                FilePath = x.FilePath,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.CategoryName : "N/A"
            })
            .ToList();

        var result = new PagedResult<BookModel>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(result);
    }

    [HttpGet("download/{id}")]
    [AllowAnonymous]
    public IActionResult DownloadBook(int id)
    {
        var book = _db.Books.FirstOrDefault(x => x.BookId == id && (x.IsDelete == false || x.IsDelete == null));
        if (book == null || string.IsNullOrEmpty(book.FilePath)) return NotFound("PDF file not found.");

        string physicalPath = Path.Combine(_env.WebRootPath, book.FilePath.TrimStart('/'));
        if (!System.IO.File.Exists(physicalPath)) return NotFound("PDF file does not exist on server.");

        // Clean file name for download header
        string safeFileName = string.Join("_", book.BookName.Trim().Split(Path.GetInvalidFileNameChars()));
        if (!safeFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) safeFileName += ".pdf";

        return PhysicalFile(physicalPath, "application/pdf", safeFileName);
    }
}

