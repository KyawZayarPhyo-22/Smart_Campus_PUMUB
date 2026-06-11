using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SemesterController : ControllerBase
    {
        private readonly SmartCampusDbContext _db;

        public SemesterController(SmartCampusDbContext db)
        {
            _db = db;
        }

        // GET /api/semesters
        [HttpGet]
        public IActionResult GetSemesters()
        {
            var lst = _db.Semesters
                         .Where(x => x.IsDelete == false)
                         .OrderByDescending(x => x.SemesterId)
                         .ToList();
            return Ok(lst);
        }

        // GET /api/semesters/{id}
        [HttpGet("{id}")]
        public IActionResult GetSemester(int id)
        {
            var item = _db.Semesters.FirstOrDefault(x => x.SemesterId == id && x.IsDelete == false);
            if (item is null) return NotFound("Semester ကို ရှာမတွေ့ပါ။");
            return Ok(item);
        }

        // POST /api/semesters
        [HttpPost]
        public IActionResult CreateSemester(SemesterCreateRequestModel request)
        {
            // Validation: နာမည်တူ စစ်ဆေးခြင်း
            if (_db.Semesters.Any(x => x.SemesterName == request.SemesterName && x.IsDelete == false))
            {
                return BadRequest(new SemesterCreateResponseModel { IsSuccess = false, Message = "Semester အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            _db.Semesters.Add(new Semester { SemesterName = request.SemesterName, IsDelete = false });
            int result = _db.SaveChanges();

            return StatusCode(201, new SemesterCreateResponseModel { IsSuccess = result > 0, Message = result > 0 ? "သိမ်းဆည်းမှု အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။" });
        }

        // PUT /api/semesters/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateSemester(int id, SemesterUpdateRequestModel request)
        {
            var item = _db.Semesters.FirstOrDefault(x => x.SemesterId == id && x.IsDelete == false);
            if (item is null) return NotFound(new SemesterUpdateResponseModel { IsSuccess = false, Message = "Semester ကို ရှာမတွေ့ပါ။" });

            // Validation: အခြားနာမည်တူ ရှိမရှိ စစ်ဆေးခြင်း
            if (_db.Semesters.Any(x => x.SemesterName == request.SemesterName && x.SemesterId != id && x.IsDelete == false))
            {
                return BadRequest(new SemesterUpdateResponseModel { IsSuccess = false, Message = "Semester အမည်မှာ ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။" });
            }

            item.SemesterName = request.SemesterName;
            int result = _db.SaveChanges();

            return Ok(new SemesterUpdateResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ပြင်ဆင်မှု အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။",
                Data = new SemesterModel { SemesterId = item.SemesterId, SemesterName = item.SemesterName }
            });
        }

        // DELETE /api/semesters/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteSemester(int id)
        {
            var item = _db.Semesters.FirstOrDefault(x => x.SemesterId == id && x.IsDelete == false);
            if (item is null) return NotFound(new SemesterDeleteResponseModel { IsSuccess = false, Message = "Semester ကို ရှာမတွေ့ပါ။" });

            // Soft Delete
            item.IsDelete = true;
            int result = _db.SaveChanges();

            return Ok(new SemesterDeleteResponseModel
            {
                IsSuccess = result > 0,
                Message = result > 0 ? "ဖျက်ဆီးမှု အောင်မြင်ပါသည်။" : "ဖျက်ဆီးမှု မအောင်မြင်ပါ။"
            });
        }
    }
}
