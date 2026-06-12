using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext; 
using Smart_Campus_PUMUB.WebApi.Models;

namespace NLADotNetInternshipTraining.WebApi.Controllers;

[ApiController]
[Route("api/subjects")]
public class SubjectController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public SubjectController(SmartCampusDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetSubjects()
    {
        var lst = _db.Subjects
                     .Where(x => x.IsDelete == false || x.IsDelete == null)
                     .OrderByDescending(x => x.SubjectId)
                     .ToList();
        return Ok(lst);
    }

    [HttpGet("{id}")]
    public IActionResult GetSubject(int id)
    {
        // Role 1: ID Validation (ID သည် သုည သို့မဟုတ် အနှုတ်ကိန်း မဖြစ်ရ)
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.Subjects.FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "တောင်းဆိုထားသော ဘာသာရပ်ဒေတာ ရှာမတွေ့ပါ။" });

        return Ok(item);
    }

    [HttpPost]
    public IActionResult CreateSubject([FromBody] SubjectCreateRequestModel request) // Role 2: Swagger [FromBody] သေချာစေရန်
    {
        // Role 3: Model State Validations
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Role 4: Foreign Key Validation (Semester ရှိမရှိနှင့် IsDelete မဖြစ်သေးကြောင်း စစ်ဆေးခြင်း)
        var semester = _db.Semesters.FirstOrDefault(s => s.SemesterId == request.SemesterId);
        if (semester is null || semester.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Semester ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });

        // Role 5: Business Duplicate Validation (Semester တစ်ခုထဲအောက်မှာ Subject Name သို့မဟုတ် Code တူတာ ရှိမရှိ စစ်ခြင်း)
        var isDuplicate = _db.Subjects.Any(x => x.SemesterId == request.SemesterId
                                          && (x.SubjectName.Trim().ToLower() == request.SubjectName.Trim().ToLower()
                                              || x.SubjectCode.Trim().ToLower() == request.SubjectCode.Trim().ToLower())
                                          && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicate)
        {
            return BadRequest(new ActionResponseModel
            {
                IsSuccess = false,
                Message = $"ဤ Semester အောက်တွင် ဘာသာရပ်အမည် သို့မဟုတ် ဘာသာရပ်ကုဒ် (Subject Code) ရှိနှင့်ပြီးသား ဖြစ်နေပါသည်။"
            });
        }

        _db.Subjects.Add(new Subject
        {
            SemesterId = request.SemesterId,
            SubjectName = request.SubjectName.Trim(),
            SubjectCode = request.SubjectCode.Trim().ToUpper(), // Code ကို စာလုံးကြီးဖြင့် အမြဲသိမ်းဆည်းရန်
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.CreatedBy,
            IsDelete = false
        });

        int result = _db.SaveChanges();
        return StatusCode(201, new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Saving Successful" : "Saving Failed" });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateSubject(int id, [FromBody] SubjectUpdateRequestModel request) // Role 2: Swagger [FromBody] သေချာစေရန်
    {
        // Role 1: ID Validation
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        if (!ModelState.IsValid) return BadRequest(ModelState);

        // ပြင်ဆင်မယ့် ဘာသာရပ် ရှိမရှိ အရင်ရှာမယ်
        var item = _db.Subjects.FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် ဘာသာရပ်ဒေတာ ရှာမတွေ့ပါ။" });

        // Role 4: Foreign Key Validation
        var semester = _db.Semesters.FirstOrDefault(s => s.SemesterId == request.SemesterId);
        if (semester is null || semester.IsDelete == true)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "ပေးထားသော Semester ID သည် မရှိပါ (သို့မဟုတ်) ဖျက်သိမ်းထားပါသည်။" });

        // Role 5: Business Duplicate Validation on Update (မိမိ ID မဟုတ်ဘဲ အခြားမှတ်တမ်းမှာ သွားတူနေတာ စစ်ခြင်း)
        var isDuplicateOnOther = _db.Subjects.Any(x => x.SemesterId == request.SemesterId
                                                      && (x.SubjectName.Trim().ToLower() == request.SubjectName.Trim().ToLower()
                                                          || x.SubjectCode.Trim().ToLower() == request.SubjectCode.Trim().ToLower())
                                                      && x.SubjectId != id
                                                      && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicateOnOther)
        {
            return BadRequest(new ActionResponseModel
            {
                IsSuccess = false,
                Message = $"ဘာသာရပ်အမည် သို့မဟုတ် ကုဒ်သည် အခြားမှတ်တမ်းတစ်ခုတွင် အသုံးပြုထားပြီး ဖြစ်ပါသည်။"
            });
        }

        item.SemesterId = request.SemesterId;
        item.SubjectName = request.SubjectName.Trim();
        item.SubjectCode = request.SubjectCode.Trim().ToUpper();
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy = request.ModifiedBy;

        int result = _db.SaveChanges();
        return Ok(new SubjectResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "Update Successful" : "Update Failed",
            Data = new SubjectModel { SubjectId = item.SubjectId, SemesterId = item.SemesterId, SubjectName = item.SubjectName, SubjectCode = item.SubjectCode }
        });
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteSubject(int id)
    {
        // Role 1: ID Validation
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.Subjects.FirstOrDefault(x => x.SubjectId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ဖျက်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        item.IsDelete = true; // Soft Delete Role
        int result = _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Delete Successfully" : "Delete Failed" });
    }
}