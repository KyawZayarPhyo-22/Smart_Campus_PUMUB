using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using Smart_Campus_PUMUB.Database.AppDbContext; 
using Smart_Campus_PUMUB.WebApi.Models;

namespace Smart_Campus_PUMUB.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegistrationPaymentController : ControllerBase
{
    private const string ApprovedStatus = "Approved";

    private readonly SmartCampusDbContext _db;
    private readonly IWebHostEnvironment _env;

    public RegistrationPaymentController(SmartCampusDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // GET: api/registrationpayment (Read All)
    [HttpGet]
    public IActionResult GetPayments()
    {
        var lst = _db.RegistrationPayments
            .Where(x => x.IsDelete == false || x.IsDelete == null)
            .OrderByDescending(p => p.PaymentId)
            .Select(p => new RegistrationPaymentModel
            {
                PaymentId = p.PaymentId,
                RegistrationId = p.RegistrationId,
                AmountPaid = p.AmountPaid,
                PaymentMethod = p.PaymentMethod,
                ReceiptImage = p.ReceiptImage,
                PaymentDate = p.PaymentDate,
                Status = p.Status ?? "Pending",
                VerifyBy = p.VerifyBy,
                CreatedDateTime = p.CreatedDateTime,
                CreatedBy = p.CreatedBy,
                ModifiedDateTime = p.ModifiedDateTime,
                ModifiedBy = p.ModifiedBy
            })
            .ToList();

        return Ok(lst);
    }

    // GET: api/registrationpayment/{id} (Read One)
    [HttpGet("{id}")]
    public IActionResult GetPayment(int id)
    {
        var item = _db.RegistrationPayments
            .FirstOrDefault(x => x.PaymentId == id && (x.IsDelete == false || x.IsDelete == null));

        if (item is null)
        {
            return NotFound(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "ငွေသွင်းမှတ်တမ်း ရှာမတွေ့ပါ။" });
        }

        var data = new RegistrationPaymentModel
        {
            PaymentId = item.PaymentId,
            RegistrationId = item.RegistrationId,
            AmountPaid = item.AmountPaid,
            PaymentMethod = item.PaymentMethod,
            ReceiptImage = item.ReceiptImage,
            PaymentDate = item.PaymentDate,
            Status = item.Status ?? "Pending",
            VerifyBy = item.VerifyBy,
            CreatedDateTime = item.CreatedDateTime,
            CreatedBy = item.CreatedBy,
            ModifiedDateTime = item.ModifiedDateTime,
            ModifiedBy = item.ModifiedBy
        };

        return Ok(data);
    }

    // POST: api/registrationpayment (Create - Upload Slip)
    [HttpPost]
    public IActionResult CreatePayment([FromForm] RegistrationPaymentCreateRequestModel request)
    {
        if (request.RegistrationId <= 0 || request.AmountPaid <= 0 || 
            string.IsNullOrEmpty(request.PaymentMethod))
        {
            return BadRequest(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "ဒေတာများကို ပြည့်စုံစွာ ဖြည့်သွင်းပါ။" });
        }

        // Verify Student Registration record exists
        var registrationCheck = _db.StudentRegistrations.FirstOrDefault(x => x.RegistrationId == request.RegistrationId && (x.IsDelete == false || x.IsDelete == null));
        if (registrationCheck is null)
        {
            return NotFound(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "သက်ဆိုင်ရာ ကျောင်းအပ်ဖောင် (RegistrationId) ကို ရှာမတွေ့ပါ။" });
        }

        if (!string.Equals(registrationCheck.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new RegistrationPaymentResponseModel
            {
                IsSuccess = false,
                Message = "Registration information is still under admin review. Payment can be submitted only after admin approval."
            });
        }

        // Receipt Image File Upload Handling
        string? dbImagePath = null;
        if (request.ReceiptImage != null && request.ReceiptImage.Length > 0)
        {
            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ReceiptImage.FileName);
            string fullPath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                request.ReceiptImage.CopyTo(stream);
            }
            dbImagePath = "/uploads/receipts/" + fileName; 
        }
        else
        {
            return BadRequest(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "ငွေလွှဲပြေစာပုံ (Receipt Image) တင်ရန် လိုအပ်သည်။" });
        }

        var newPayment = new RegistrationPayment
        {
            RegistrationId = request.RegistrationId,
            AmountPaid = request.AmountPaid,
            PaymentMethod = request.PaymentMethod,
            ReceiptImage = dbImagePath, // Save image path in DB
            PaymentDate = DateTime.UtcNow.AddHours(6).AddMinutes(30), // Payment date
            Status = "Pending", 
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = request.CreatedBy,
            IsDelete = false
        };

        _db.RegistrationPayments.Add(newPayment);
        int result = _db.SaveChanges();

        return StatusCode(201, new RegistrationPaymentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ငွေသွင်းလွှာ တင်သွင်းခြင်း အောင်မြင်ပါသည်။" : "သိမ်းဆည်းမှု မအောင်မြင်ပါ။"
        });
    }

    // PUT: api/registrationpayment/{id} (Update)
    [HttpPost("update/{id}")]
    public IActionResult UpdatePayment(int id, [FromForm] RegistrationPaymentUpdateRequestModel request)
    {
        var item = _db.RegistrationPayments.FirstOrDefault(x => x.PaymentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "ပြင်ဆင်မည့် ငွေသွင်းမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        if (item.Status == "Approved")
        {
            return BadRequest(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "Approved ဖြစ်ပြီးသား ပြေစာများကို ပြင်ဆင်ခွင့်မရှိပါ။" });
        }

        // Replace image if new file uploaded
        if (request.ReceiptImage != null && request.ReceiptImage.Length > 0)
        {
            if (!string.IsNullOrEmpty(item.ReceiptImage))
            {
                string oldPath = Path.Combine(_env.WebRootPath, item.ReceiptImage.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string uploadDir = Path.Combine(_env.WebRootPath, "uploads", "receipts");
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(request.ReceiptImage.FileName);
            string fullPath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                request.ReceiptImage.CopyTo(stream);
            }
            item.ReceiptImage = "/uploads/receipts/" + fileName;
        }

        item.AmountPaid = request.AmountPaid;
        item.PaymentMethod = request.PaymentMethod;
        item.Status = "Pending"; // Reset status to Pending on edit
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
        item.ModifiedBy = request.ModifiedBy;

        int result = _db.SaveChanges();

        return Ok(new RegistrationPaymentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ငွေသွင်းမှတ်တမ်း ပြင်ဆင်ခြင်း အောင်မြင်ပါသည်။" : "ပြင်ဆင်မှု မအောင်မြင်ပါ။"
        });
    }

    // DELETE: api/registrationpayment/{id} (Soft Delete)
    [HttpDelete("{id}")]
    public IActionResult DeletePayment(int id)
    {
        var item = _db.RegistrationPayments.FirstOrDefault(x => x.PaymentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "ဖျက်သိမ်းမည့် ငွေသွင်းမှတ်တမ်းကို ရှာမတွေ့ပါ။" });
        }

        item.IsDelete = true;
        item.Status = "Deleted";
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        return Ok(new RegistrationPaymentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? "ငွေသွင်းမှတ်တမ်း ပယ်ဖျက်ခြင်း အောင်မြင်ပါသည်။" : "ပယ်ဖျက်ခြင်း မအောင်မြင်ပါ။"
        });
    }

    // PATCH: api/registrationpayment/{id}/verify (Staff Verification)
    [HttpPatch("{id}/verify")]
    public IActionResult VerifyPayment(int id, [FromBody] RegistrationPaymentVerifyRequestModel request)
    {
        var item = _db.RegistrationPayments.FirstOrDefault(x => x.PaymentId == id && (x.IsDelete == false || x.IsDelete == null));
        if (item is null)
        {
            return NotFound(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "စစ်ဆေးမည့် ငွေသွင်းမှတ်တမ်း ရှာမတွေ့ပါ။" });
        }

        var validStatuses = new[] { "Approved", "Rejected" };
        if (!validStatuses.Contains(request.Status))
        {
            return BadRequest(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "Status ပြောင်းလဲမှုပုံစံ မှားယွင်းနေပါသည်။ (Approved သို့မဟုတ် Rejected သာ ဖြစ်ရမည်)" });
        }

        // Verify staff user exists
        var staffCheck = _db.Users.FirstOrDefault(x => x.UserId == request.VerifyBy && (x.IsDelete == false || x.IsDelete == null));
        if (staffCheck is null)
        {
            return BadRequest(new RegistrationPaymentResponseModel { IsSuccess = false, Message = "စစ်ဆေးသူ ဝန်ထမ်းအကောင့် (VerifyBy) ကို စနစ်ထဲတွင် ရှာမတွေ့ပါ။" });
        }

        item.Status = request.Status;
        item.VerifyBy = request.VerifyBy; // Save verifier UserId
        item.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);

        int result = _db.SaveChanges();

        return Ok(new RegistrationPaymentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? $"ငွေသွင်းပြေစာကို အောင်မြင်စွာ {request.Status} ပြုလုပ်ပြီးပါပြီ။" : "အတည်ပြုခြင်း မအောင်မြင်ပါ။"
        });
    }
}


