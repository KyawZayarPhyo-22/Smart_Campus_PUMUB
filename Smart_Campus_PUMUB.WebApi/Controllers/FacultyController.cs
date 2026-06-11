using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FacultyController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public FacultyController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/faculties
        [HttpGet]
        public IActionResult GetFaculties()
        {
            var lst = _db.Faculties
                         .Where(x => !x.IsDelete) // ဖျက်ထားသည့် data များ မပါဝင်စေရန်
                         .OrderByDescending(x => x.FacultyId)
                         .ToList();
            return Ok(lst);
        }

        // GET /api/faculties/{id}
        [HttpGet("{id}")]
        public IActionResult GetFaculty(int id)
        {
            var item = _db.Faculties.FirstOrDefault(x => x.FacultyId == id && !x.IsDelete);
            if (item is null) return NotFound("Faculty ကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/faculties
        [HttpPost]
        public IActionResult CreateFaculty(FacultyCreateRequestModel request)
        {
            // Validation: Faculty Name တူနေခြင်း ရှိ/မရှိ စစ်ဆေးခြင်း
            if (_db.Faculties.Any(x => x.FacultyName == request.FacultyName && !x.IsDelete))
            {
                return BadRequest(new FacultyCreateResponseModel { IsSuccess = false, Message = "Faculty အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            _db.Faculties.Add(new Faculty { FacultyName = request.FacultyName, IsDelete = false });
            int result = _db.SaveChanges();

            return StatusCode(201, new FacultyCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/faculties/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateFaculty(int id, FacultyUpdateRequestModel request)
        {
            var item = _db.Faculties.FirstOrDefault(x => x.FacultyId == id && !x.IsDelete);
            if (item is null) return NotFound(new FacultyUpdateResponseModel { IsSuccess = false, Message = "Faculty ကို ရှာမတွေ့ပါ။" });

            // Validation: အခြားသော Faculty Name များတွင် တူနေခြင်း ရှိ/မရှိ စစ်ဆေးခြင်း
            if (_db.Faculties.Any(x => x.FacultyName == request.FacultyName && x.FacultyId != id && !x.IsDelete))
            {
                return BadRequest(new FacultyUpdateResponseModel { IsSuccess = false, Message = "Faculty အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            item.FacultyName = request.FacultyName;
            int result = _db.SaveChanges();

            return Ok(new FacultyUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new FacultyModel { FacultyId = item.FacultyId, FacultyName = item.FacultyName }
            });
        }

        // DELETE /api/faculties/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteFaculty(int id)
        {
            var item = _db.Faculties.FirstOrDefault(x => x.FacultyId == id && !x.IsDelete);
            if (item is null) return NotFound(new FacultyDeleteResponseModel { IsSuccess = false, Message = "Faculty ကို ရှာမတွေ့ပါ။" });

            // Soft Delete အသုံးပြုခြင်း
            item.IsDelete = true;
            int result = _db.SaveChanges();

            return Ok(new FacultyDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။" : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
            });
        }
    }
}
