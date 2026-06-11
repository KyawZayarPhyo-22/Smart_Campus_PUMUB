using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Campus_PUMUB.Database.AppDbContext; // ကိုကို့ DbContext နာမည်အတိုင်း ပြောင်းထားပါတယ်
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public DepartmentController(SmartCampusDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetDepartments()
    {
        var lst = _db.Departments
                    .Include(x => x.Tutors)
                     .Where(x => x.IsDelete == false || x.IsDelete == null)
                     .OrderByDescending(x => x.DepartmentId)
                     .ToList();
        return Ok(lst);
    }

    [HttpGet("{id}")]
    public IActionResult GetDepartment(int id)
    {
        // Role: ID Validation
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.Departments.Include(x => x.Tutors).FirstOrDefault(x => x.DepartmentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ဒေတာ ရှာမတွေ့ပါ။" });

        return Ok(item);
    }

    [HttpPost]
    public IActionResult CreateDepartment([FromBody] DepartmentCreateRequestModel request) // Role: [FromBody] သေချာစေရန်
    {
        // Role: Model State Validation
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Role: Foreign Key (Faculty_Id) Active ဖြစ်၊ မဖြစ် စစ်ဆေးခြင်း Validation
        var faculty = _db.Faculties.FirstOrDefault(f => f.FacultyId == request.FacultyId);
        if (faculty is null || faculty.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Faculty ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });

        // Role: Business Duplicate Validation (Faculty တစ်ခုတည်းအောက်မှာ Department နာမည် တူတာ ရှိမရှိ စစ်ခြင်း)
        var isDuplicate = _db.Departments.Any(x => x.FacultyId == request.FacultyId
                                                && x.DepartmentName.Trim().ToLower() == request.DepartmentName.Trim().ToLower()
                                                && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicate)
        {
            return BadRequest(new ActionResponseModel
            {
                IsSuccess = false,
                Message = $"ပေးထားသော Faculty အောက်တွင် '{request.DepartmentName}' ဟူသော ဌာနခွဲ အမည် ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။"
            });
        }

        _db.Departments.Add(new Department
        {
            FacultyId = request.FacultyId,
            DepartmentName = request.DepartmentName.Trim(),
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.CreatedBy, // string? အနေနဲ့ သိမ်းသွားပါမယ်
            IsDelete = false // Column Name အမှန် IsDelete ကို သုံးထားပါတယ်
        });

        int result = _db.SaveChanges();
        return StatusCode(201, new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Saving Successful" : "Saving Failed" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateDepartment(int id, [FromBody] DepartmentUpdateRequestModel request) // Role: [FromBody] သေချာစေရန်
    {
        // Role: ID Validation
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // ပြင်မယ့် Department ရှိမရှိ အရင်ရှာမယ်
        var item = _db.Departments.FirstOrDefault(x => x.DepartmentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        // Role: Foreign Key Validation
        var faculty = _db.Faculties.FirstOrDefault(f => f.FacultyId == request.FacultyId);
        if (faculty is null || faculty.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Faculty ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });

        // Role: Business Duplicate Validation on Update (မိမိ ID မဟုတ်ဘဲ အခြားဌာနမှာ နာမည် သွားတူနေတာ စစ်ခြင်း)
        var isDuplicateOnOther = _db.Departments.Any(x => x.FacultyId == request.FacultyId
                                                        && x.DepartmentName.Trim().ToLower() == request.DepartmentName.Trim().ToLower()
                                                        && x.DepartmentId != id
                                                        && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicateOnOther)
        {
            return BadRequest(new ActionResponseModel
            {
                IsSuccess = false,
                Message = $"'{request.DepartmentName}' သည် အခြားမှတ်တမ်းတစ်ခုတွင် အသုံးပြုထားပြီး ဖြစ်ပါသည်။"
            });
        }

        item.FacultyId = request.FacultyId;
        item.DepartmentName = request.DepartmentName.Trim();
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy = request.ModifiedBy; // string? အနေနဲ့ သိမ်းသွားပါမယ်

        int result = _db.SaveChanges();
        return Ok(new DepartmentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "Update Successful" : "Update Failed",
            Data = new DepartmentModel { DepartmentId = item.DepartmentId, FacultyId = item.FacultyId, DepartmentName = item.DepartmentName }
        });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteDepartment(int id)
    {
        // Role: ID Validation
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.Departments.FirstOrDefault(x => x.DepartmentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ဖျက်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        item.IsDelete = true; // Soft Delete Role (IsDelete column ကို သုံးပါတယ်)
        int result = _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Delete Successfully" : "Delete Failed" });
    }
}