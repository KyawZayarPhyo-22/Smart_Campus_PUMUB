using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext;
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
        public IActionResult GetCategories()
        {
            var lst = _db.Categories
                         .Where(x => x.IsDelete == false)
                         .OrderByDescending(x => x.CategoryId)
                         .ToList();
            return Ok(lst);
        }

        // GET /api/categories/{id}
        [HttpGet("{id}")]
        public IActionResult GetCategory(int id)
        {
            var item = _db.Categories.FirstOrDefault(x => x.CategoryId == id && x.IsDelete == false);
            if (item is null) return NotFound("Category ကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/categories
        [HttpPost]
        public IActionResult CreateCategory(CategoryCreateRequestModel request)
        {
            // Validation: Category Name တူနေခြင်း ရှိ/မရှိ စစ်ဆေးခြင်း
            if (_db.Categories.Any(x => x.CategoryName == request.CategoryName && x.IsDelete == false))
            {
                return BadRequest(new CategoryCreateResponseModel { IsSuccess = false, Message = "Category အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            _db.Categories.Add(new Category { CategoryName = request.CategoryName, IsDelete = false });
            int result = _db.SaveChanges();

            return StatusCode(201, new CategoryCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/categories/{id}
        [HttpPut("{id}")]
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

            return Ok(new CategoryUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new CategoryModel { CategoryId = item.CategoryId, CategoryName = item.CategoryName }
            });
        }

        // DELETE /api/categories/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteCategory(int id)
        {
            var item = _db.Categories.FirstOrDefault(x => x.CategoryId == id && x.IsDelete == false);
            if (item is null) return NotFound(new CategoryDeleteResponseModel { IsSuccess = false, Message = "Category ကို ရှာမတွေ့ပါ။" });

            // Soft Delete
            item.IsDelete = true;
            int result = _db.SaveChanges();

            return Ok(new CategoryDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။" : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
            });
        }
    }
}
