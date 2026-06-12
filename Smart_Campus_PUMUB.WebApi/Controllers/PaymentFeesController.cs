using Microsoft.AspNetCore.Mvc;
using Smart_Campus_PUMUB.Database.AppDbContext;
using Smart_Campus_PUMUB.WebApi.Models;
namespace Smart_Campus_PUMUB.WebApi.Controllers;



[ApiController]
[Route("api/payment-fees")]
public class PaymentFeesController : ControllerBase
{
    private readonly SmartCampusDbContext _db;

    public PaymentFeesController(SmartCampusDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetPaymentFees()
    {
        var lst = _db.PaymentFees
                     .Where(x => x.IsDelete == false || x.IsDelete == null)
                     .OrderByDescending(x => x.FeesId)
                     .ToList();
        return Ok(lst);
    }

    [HttpGet("{id}")]
    public IActionResult GetPaymentFee(int id)
    {
        // Role: ID Validation (ID သည် သုည သို့မဟုတ် အနှုတ်ကိန်း မဖြစ်ရ)
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.PaymentFees.FirstOrDefault(x => x.FeesId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "တောင်းဆိုထားသော နှုန်းထားဒေတာ ရှာမတွေ့ပါ။" });

        return Ok(item);
    }

    [HttpPost]
    public IActionResult CreatePaymentFee([FromBody] PaymentFeeCreateRequestModel request) // Role: [FromBody] ပါဝင်မှု သေချာစေရန်
    {
        // Role: Model Attributes Validation (Required, Range စတာတွေ စစ်ဆေးခြင်း)
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Role: Business Duplicate Validation (ClassYear တူညီပြီးသား ရှိမရှိ စစ်ဆေးခြင်း)
        var isDuplicate = _db.PaymentFees.Any(x => x.ClassYear == request.ClassYear && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicate)
        {
            return BadRequest(new ActionResponseModel
            {
                IsSuccess = false,
                Message = $"'{request.ClassYear}' အတွက် လစဉ်ကြေး သတ်မှတ်ပြီးသား ဖြစ်နေသဖြင့် ထပ်မံထည့်သွင်း၍ မရနိုင်ပါ။"
            });
        }

        _db.PaymentFees.Add(new PaymentFee
        {
            ClassYear = request.ClassYear,
            MontlyAmount = request.MontlyAmount,
            Status = string.IsNullOrEmpty(request.Status) ? "Active" : request.Status,
            CreatedDateTime = DateTime.Now,
            CreatedBy = request.CreatedBy, // string? အဖြစ် အလုပ်လုပ်ပါလိမ့်မယ်
            IsDelete = false
        });

        int result = _db.SaveChanges();
        return StatusCode(201, new ActionResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "Saving Successful" : "Saving Failed"
        });
    }

    [HttpPut("{id}")]
    public IActionResult UpdatePaymentFee(int id, [FromBody] PaymentFeeUpdateRequestModel request) // Role: [FromBody] ပါဝင်မှု သေချာစေရန်
    {
        // Role: ID Validation
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        if (!ModelState.IsValid) return BadRequest(ModelState);

        var item = _db.PaymentFees.FirstOrDefault(x => x.FeesId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ပြင်ဆင်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        // Role: Business Duplicate Validation on Update 
        // (မိမိ ID မဟုတ်ဘဲ အခြား ID မှာ ClassYear တူနေတာ ရှိမရှိ စစ်ဆေးခြင်း)
        var isDuplicateOnOther = _db.PaymentFees.Any(x => x.ClassYear == request.ClassYear && x.FeesId != id && (x.IsDelete == false || x.IsDelete == null));
        if (isDuplicateOnOther)
        {
            return BadRequest(new ActionResponseModel
            {
                IsSuccess = false,
                Message = $"'{request.ClassYear}' သည် အခြားမှတ်တမ်းတစ်ခုတွင် အသုံးပြုထားပြီး ဖြစ်ပါသည်။"
            });
        }

        item.ClassYear = request.ClassYear;
        item.MontlyAmount = request.MontlyAmount;
        item.Status = request.Status;
        item.ModifiedDateTime = DateTime.Now;
        item.ModifiedBy = request.ModifiedBy;

        int result = _db.SaveChanges();
        return Ok(new PaymentFeeResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "Update Successful" : "Update Failed",
            Data = new PaymentFeeModel { FeesId = item.FeesId, ClassYear = item.ClassYear, MontlyAmount = item.MontlyAmount, Status = item.Status }
        });
    }

    [HttpDelete("{id}")]
    public IActionResult DeletePaymentFee(int id)
    {
        // Role: ID Validation
        if (id <= 0)
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "မှားယွင်းသော ID ပုံစံဖြစ်နေပါသည်။" });

        var item = _db.PaymentFees.FirstOrDefault(x => x.FeesId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = "ဖျက်ရန် ဒေတာ ရှာမတွေ့ပါ။" });

        item.IsDelete = true; // Soft Delete Role
        int result = _db.SaveChanges();
        return Ok(new ActionResponseModel { IsSuccess = result > 0, Message = result > 0 ? "Delete Successfully" : "Delete Failed" });
    }

    // Special API - Auto Calculate Fee By Class Year
    [HttpGet("calculate")]
    public IActionResult CalculateFee([FromQuery] string classYear)
    {
        // Role: Parameter Empty/Null Validation
        if (string.IsNullOrEmpty(classYear))
            return BadRequest(new ActionResponseModel { IsSuccess = false, Message = "Class Year သတ်မှတ်ပေးရန် လိုအပ်ပါသည်။" });

        // Role: Existence Validation (ဒေတာ ရှိမရှိ အရင်စစ်ဆေးခြင်း)
        var hasClassYear = _db.PaymentFees.Any(x => x.ClassYear == classYear && (x.IsDelete == false || x.IsDelete == null));
        if (!hasClassYear)
        {
            return NotFound(new ActionResponseModel { IsSuccess = false, Message = $"'{classYear}' အတွက် သတ်မှတ်ထားသော နှုန်းထားစာရင်း မရှိသေးပါ။" });
        }

        // သက်ဆိုင်ရာ Class အလိုက် စုစုပေါင်းလစဉ်ကြေးကို တွက်ထုတ်ပေးခြင်း
        var totalAmount = _db.PaymentFees
                             .Where(x => x.ClassYear == classYear && (x.IsDelete == false || x.IsDelete == null))
                             .Sum(x => x.MontlyAmount);

        return Ok(new { ClassYear = classYear, TotalCalculatedFee = totalAmount, CalculatedAt = DateTime.Now });
    }
}