using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Filters;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public CategoryController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/categories
        [HttpGet]
        [Permission("Category.View")]
        public IActionResult GetCategories()
        {
            var data = _db.Categories
                .Where(c => c.IsDelete == false)
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,

                    Books = _db.Books
                        .Where(b => b.CategoryId == c.CategoryId && b.IsDelete == false)
                        .Select(b => new
                        {
                            b.BookId,
                            b.BookName
                        })
                        .ToList()
                })
                .ToList();

            return Ok(data);
        }

        // GET /api/categories/{id}
        [HttpGet("{id}")]
        [Permission("Category.View")]
        public IActionResult GetCategory(int id)
        {
            var item = _db.Categories.Include(c => c.Books).FirstOrDefault(x => x.CategoryId == id && x.IsDelete == false);
            if (item is null) return NotFound("Category ကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/categories
        [HttpPost]
        [Permission("Category.Create")]
        public IActionResult CreateCategory(CategoryCreateRequestModel request)
        {
            // Validation: Category Name တူနေခြင်း ရှိ/မရှိ စစ်ဆေးခြင်း
            if (_db.Categories.Any(x => x.CategoryName == request.CategoryName && x.IsDelete == false))
            {
                return BadRequest(new CategoryCreateResponseModel { IsSuccess = false, Message = "Category အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            _db.Categories.Add(new Category { CategoryName = request.CategoryName, IsDelete = false });
            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Category Added",
                Description = $"{request.CategoryName} was added to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return StatusCode(201, new CategoryCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/categories/{id}
        [HttpPut("{id}")]
        [Permission("Category.Edit")]
        public IActionResult UpdateCategory(int id, CategoryUpdateRequestModel request)
        {
            var item = _db.Categories.FirstOrDefault(x => x.CategoryId == id && x.IsDelete == false);
            if (item is null) return NotFound(new CategoryUpdateResponseModel { IsSuccess = false, Message = "Category ကို ရှာမတွေ့ပါ။" });

            // Validation: အခြား Category Name များတွင် တူနေခြင်း ရှိ/မရှိ စစ်ဆေးခြင်း
            if (_db.Categories.Any(x => x.CategoryName == request.CategoryName && x.CategoryId != id && x.IsDelete == false))
            {
                return BadRequest(new CategoryUpdateResponseModel { IsSuccess = false, Message = "Category အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            item.CategoryName = request.CategoryName;
            int result = _db.SaveChanges();

            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Category Updated",
                Description = $"{request.CategoryName} was updated to the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new CategoryUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new CategoryModel { CategoryId = item.CategoryId, CategoryName = item.CategoryName }
            });
        }

        // DELETE /api/categories/{id}
        //[HttpDelete("{id}")]
        //public IActionResult DeleteCategory(int id)
        //{
        //    var item = _db.Categories.FirstOrDefault(x => x.CategoryId == id && x.IsDelete == false);
        //    if (item is null) return NotFound(new CategoryDeleteResponseModel { IsSuccess = false, Message = "Category ကို ရှာမတွေ့ပါ။" });

        //    // Soft Delete
        //    item.IsDelete = true;
        //    int result = _db.SaveChanges();

        //    return Ok(new CategoryDeleteResponseModel
        //    {
        //        IsSuccess = result > 0,
        //        Message = result > 0 ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။" : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
        //    });
        //}

        // DELETE /api/categories/{id}
        [HttpDelete("{id}")]
        [Permission("Category.Delete")]
        public IActionResult DeleteCategory(int id)
        {
            var item = _db.Categories
                .FirstOrDefault(x => x.CategoryId == id && x.IsDelete == false);

            if (item is null)
            {
                return NotFound(new CategoryDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "Category ကို ရှာမတွေ့ပါ။"
                });
            }

            // 🚨 CHECK FK (Book exists or not)
            var hasBooks = _db.Books.Any(x => x.CategoryId == id && x.IsDelete == false);

            if (hasBooks)
            {
                return BadRequest(new CategoryDeleteResponseModel
                {
                    IsSuccess = false,
                    Message = "ဒီ Category ကို Book တွေက အသုံးပြုနေပါတယ်။ ဖျက်လို့မရပါ။"
                });
            }

            // ✅ Soft Delete
            item.IsDelete = true;
            item.ModifiedDateTime = DateTime.Now; // optional but good practice

            int result = _db.SaveChanges();
            _db.Activities.Add(new Activity
            {
                ActivityTitle = "Category Deleted",
                Description = $"{item.CategoryName} was deleted from the System.",
                CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30)
            });
            _db.SaveChanges();

            return Ok(new CategoryDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0
                    ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။"
                    : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
            });
        }

        [HttpGet("paginate")]
        [Permission("Category.View")]
        public IActionResult GetCategoriesPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _db.Categories
                .AsNoTracking()
                .Where(x => x.IsDelete == false || x.IsDelete == null);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.CategoryName != null && x.CategoryName.Contains(searchTerm));
            }

            var totalCount = query.Count();

            var items = query
                .OrderByDescending(x => x.CategoryId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CategoryModel
                {
                    CategoryId = x.CategoryId,
                    CategoryName = x.CategoryName
                })
                .ToList();

            var result = new PagedResult<CategoryModel>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }
    }
}


