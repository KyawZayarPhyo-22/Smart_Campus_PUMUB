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
    private readonly Smart_Campus_PUMUB.WebApi.Services.IKpayGatewayService _kpayGatewayService;

    public RegistrationPaymentController(
        SmartCampusDbContext db, 
        IWebHostEnvironment env,
        Smart_Campus_PUMUB.WebApi.Services.IKpayGatewayService kpayGatewayService)
    {
        _db = db;
        _env = env;
        _kpayGatewayService = kpayGatewayService;
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

        if (request.Status == "Approved")
        {
            var registration = _db.StudentRegistrations.FirstOrDefault(r => r.RegistrationId == item.RegistrationId && (r.IsDelete == false || r.IsDelete == null));
            if (registration != null)
            {
                registration.Status = "Approved";
                registration.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
                registration.ModifiedBy = staffCheck.FullName ?? "Staff";
            }
            ProcessStudentEnrollmentOnApproval(item, staffCheck.FullName ?? "Staff");
        }

        int result = _db.SaveChanges();

        return Ok(new RegistrationPaymentResponseModel
        {
            IsSuccess = result > 0,
            Message = result > 0 ? $"ငွေသွင်းပြေစာကို အောင်မြင်စွာ {request.Status} ပြုလုပ်ပြီးပါပြီ။" : "အတည်ပြုခြင်း မအောင်မြင်ပါ။"
        });
    }

    #region KBZPay MMQR Integration Endpoints

    // POST: api/registrationpayment/initiate-kpay
    [HttpPost("initiate-kpay")]
    public async System.Threading.Tasks.Task<IActionResult> InitiateKpayPayment([FromBody] KpayPrecreateRequestModel request)
    {
        if (request.RegistrationId <= 0 || request.Amount <= 0)
        {
            return BadRequest(new KpayPrecreateResponseModel 
            { 
                IsSuccess = false, 
                Message = "ကျောင်းအပ်နှံမှု အချက်အလက်နှင့် ငွေပမာဏ မမှန်ကန်ပါ။" 
            });
        }

        var registration = _db.StudentRegistrations.FirstOrDefault(x => x.RegistrationId == request.RegistrationId && (x.IsDelete == false || x.IsDelete == null));
        if (registration == null)
        {
            return NotFound(new KpayPrecreateResponseModel 
            { 
                IsSuccess = false, 
                Message = "ကျောင်းအပ်နှံမှု မှတ်တမ်း ရှာမတွေ့ပါ။" 
            });
        }

        // Call gateway service to generate QR
        var precreateResult = await _kpayGatewayService.PrecreateQrPaymentAsync(request);

        if (!precreateResult.IsSuccess)
        {
            return BadRequest(precreateResult);
        }

        // Create pending payment record in DB
        var payment = new RegistrationPayment
        {
            RegistrationId = request.RegistrationId,
            AmountPaid = request.Amount,
            PaymentMethod = "KBZPay (MMQR)",
            ReceiptImage = precreateResult.OrderId, // Store OrderId in ReceiptImage field for tracking
            PaymentDate = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            Status = "Pending",
            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
            CreatedBy = request.CreatedBy ?? "Student",
            IsDelete = false
        };

        _db.RegistrationPayments.Add(payment);
        _db.SaveChanges();

        precreateResult.PaymentId = payment.PaymentId;
        return Ok(precreateResult);
    }

    // POST: api/registrationpayment/kpay-callback (Webhook from Payment Gateway / Mock Server)
    [AllowAnonymous]
    [HttpPost("kpay-callback")]
    public IActionResult KpayCallback([FromBody] KpayCallbackRequestModel payload)
    {
        var orderId = payload.OrderId ?? payload.MerchOrderId;
        if (string.IsNullOrEmpty(orderId))
        {
            return BadRequest(new { return_code = "FAIL", message = "OrderId is required" });
        }

        var payment = _db.RegistrationPayments
            .FirstOrDefault(p => (p.ReceiptImage == orderId || p.ReceiptImage.Contains(orderId)) && (p.IsDelete == false || p.IsDelete == null));

        if (payment == null)
        {
            return NotFound(new { return_code = "FAIL", message = "Payment order not found" });
        }

        if (payment.Status == "Approved")
        {
            return Ok(new { return_code = "SUCCESS", message = "Already approved" });
        }

        // Auto-approve payment
        payment.Status = "Approved";
        payment.ModifiedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
        payment.ModifiedBy = "KBZPay System Callback";

        var registration = _db.StudentRegistrations.FirstOrDefault(r => r.RegistrationId == payment.RegistrationId && (r.IsDelete == false || r.IsDelete == null));
        if (registration != null)
        {
            registration.Status = "Approved";
            registration.ModifiedDatetime = DateTime.UtcNow.AddHours(6).AddMinutes(30);
            registration.ModifiedBy = "KBZPay (MMQR) Auto-Approved";
        }

        ProcessStudentEnrollmentOnApproval(payment, "KBZPay Gateway");

        _db.SaveChanges();

        return Ok(new { return_code = "SUCCESS", message = "Payment successfully confirmed" });
    }

    // GET: api/registrationpayment/check-status/{orderId} (Polling check from Frontend)
    [AllowAnonymous]
    [HttpGet("check-status/{orderId}")]
    public IActionResult CheckPaymentStatus(string orderId)
    {
        var payment = _db.RegistrationPayments
            .FirstOrDefault(p => (p.ReceiptImage == orderId || p.ReceiptImage.Contains(orderId)) && (p.IsDelete == false || p.IsDelete == null));

        if (payment == null)
        {
            return NotFound(new PaymentStatusCheckResponseModel
            {
                IsSuccess = false,
                Message = "Payment order not found"
            });
        }

        bool isPaid = string.Equals(payment.Status, "Approved", StringComparison.OrdinalIgnoreCase);

        return Ok(new PaymentStatusCheckResponseModel
        {
            IsSuccess = true,
            Status = payment.Status ?? "Pending",
            IsPaid = isPaid,
            PaymentId = payment.PaymentId,
            RegistrationId = payment.RegistrationId,
            Message = isPaid ? "Payment confirmed successfully" : "Waiting for payment..."
        });
    }

    // POST: api/registrationpayment/mock-complete-kpay/{orderId} (Simulate instant success for testing)
    [AllowAnonymous]
    [HttpPost("mock-complete-kpay/{orderId}")]
    public IActionResult MockCompleteKpay(string orderId)
    {
        return KpayCallback(new KpayCallbackRequestModel
        {
            OrderId = orderId,
            Status = "SUCCESS",
            TradeStatus = "PAY_SUCCESS"
        });
    }

    private void ProcessStudentEnrollmentOnApproval(RegistrationPayment payment, string verifierName)
    {
        var registration = _db.StudentRegistrations.FirstOrDefault(r => r.RegistrationId == payment.RegistrationId && (r.IsDelete == false || r.IsDelete == null));
        if (registration != null && registration.UserId != null)
        {
            var student = _db.Students.FirstOrDefault(s => s.UserId == registration.UserId && (s.IsDelete == false || s.IsDelete == null));
            if (student == null)
            {
                student = new Student
                {
                    UserId = registration.UserId.Value,
                    CurrentClassYear = registration.AcademicYearLevel ?? "First Year",
                    CurrentMajor = registration.Major ?? "N/A",
                    CurrentRollNo = !string.IsNullOrWhiteSpace(registration.User?.RoleNo) ? registration.User.RoleNo : (registration.RollNo ?? string.Empty),
                    Status = "Active",
                    CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                    CreatedBy = verifierName,
                    IsDelete = false
                };
                _db.Students.Add(student);
                _db.SaveChanges();
            }

            var semester = _db.Semesters.FirstOrDefault(s => s.SemesterName == registration.AcademicYearLevel && (s.IsDelete == false || s.IsDelete == null));
            if (semester != null)
            {
                var subjects = _db.Subjects.Where(s => s.SemesterId == semester.SemesterId).ToList();
                foreach (var sub in subjects)
                {
                    bool alreadyEnrolled = _db.StudentSubjectEnrollments.Any(e => e.StudentId == student.StudentId && e.SubjectId == sub.SubjectId && e.SemesterId == semester.SemesterId && (e.IsDelete == false || e.IsDelete == null));
                    if (!alreadyEnrolled)
                    {
                        var newEnrollment = new StudentSubjectEnrollment
                        {
                            StudentId = student.StudentId,
                            SubjectId = sub.SubjectId,
                            SemesterId = semester.SemesterId,
                            EnrollmentDate = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                            Status = 1,
                            CreatedDateTime = DateTime.UtcNow.AddHours(6).AddMinutes(30),
                            CreatedBy = verifierName,
                            IsDelete = false
                        };
                        _db.StudentSubjectEnrollments.Add(newEnrollment);
                    }
                }
            }
        }
    }

    #endregion
}


