namespace Smart_Campus_PUMUB.WebApi.Models;

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

// ==========================================
// Shared Base Response Model
// ==========================================
public class ActionResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

// ==========================================
// ၇။ Rules & Regulations DTOs
// ==========================================
public class RuleCreateRequestModel
{
    [Required(ErrorMessage = "Title သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Title သည် စာလုံးရေ ၁၅၀ ထက် မကျော်ရပါ။")]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [StringLength(255, ErrorMessage = "Penalty သည် စာလုံးရေ ၂၅၅ ထက် မကျော်ရပါ။")]
    public string? Penalty { get; set; }
    public string? CreatedBy { get; set; }
}

public class RuleUpdateRequestModel
{
    [Required(ErrorMessage = "Title သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Title သည် စာလုံးရေ ၁၅၀ ထက် မကျော်ရပါ။")]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [StringLength(255, ErrorMessage = "Penalty သည် စာလုံးရေ ၂၅၅ ထက် မကျော်ရပါ။")]
    public string? Penalty { get; set; }
    public string? ModifiedBy { get; set; }
}

public class RuleResponseModel : ActionResponseModel
{
    public RuleModel? Data { get; set; }
}

public class RuleModel
{
    public int RuleId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Penalty { get; set; }
}

// ==========================================
// ၈။ Payment Fees DTOs
// ==========================================
public class PaymentFeeCreateRequestModel
{
    [Required(ErrorMessage = "Class Year သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50, ErrorMessage = "Class Year သည် စာလုံးရေ ၅၀ ထက် မကျော်ရပါ။")]
    public string? ClassYear { get; set; }

    [Required(ErrorMessage = "Monthly Amount သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(0.00, 99999999.99, ErrorMessage = "Amount သည် တရားဝင်သော ပမာဏ ဖြစ်ရပါမည်။")]
    public decimal MontlyAmount { get; set; }

    [StringLength(20, ErrorMessage = "Status သည် စာလုံးရေ ၂၀ ထက် မကျော်ရပါ။")]
    public string? Status { get; set; } = "Active";
    public string? CreatedBy { get; set; }
}

public class PaymentFeeUpdateRequestModel
{
    [Required(ErrorMessage = "Class Year သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50, ErrorMessage = "Class Year သည် စာလုံးရေ ၅၀ ထက် မကျော်ရပါ။")]
    public string? ClassYear { get; set; }

    [Required(ErrorMessage = "Monthly Amount သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(0.00, 99999999.99, ErrorMessage = "Amount သည် တရားဝင်သော ပမာဏ ဖြစ်ရပါမည်。")]
    public decimal MontlyAmount { get; set; }

    [StringLength(20, ErrorMessage = "Status သည် စာလုံးရေ ၂၀ ထက် မကျော်ရပါ။")]
    public string? Status { get; set; }
    public string? ModifiedBy { get; set; }
}

public class PaymentFeeResponseModel : ActionResponseModel
{
    public PaymentFeeModel? Data { get; set; }
}

public class PaymentFeeModel
{
    public int FeesId { get; set; }
    public string? ClassYear { get; set; }
    public decimal MontlyAmount { get; set; }
    public string? Status { get; set; }
}

// ==========================================
// ၉။ Department DTOs
// ==========================================
public class DepartmentCreateRequestModel
{
    [Required(ErrorMessage = "Faculty ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, int.MaxValue, ErrorMessage = "မှန်ကန်သော Faculty ID ကို ထည့်ပေးပါ။")]
    public int FacultyId { get; set; }

    [Required(ErrorMessage = "Department Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Department Name သည် စာလုံးရေ ၁၅၀ ထက် မကျော်ရပါ။")]
    public string? DepartmentName { get; set; }
    public string? CreatedBy { get; set; }
}

public class DepartmentUpdateRequestModel
{
    [Required(ErrorMessage = "Faculty ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, int.MaxValue, ErrorMessage = "မှန်ကန်သော Faculty ID ကို ထည့်ပေးပါ။")]
    public int FacultyId { get; set; }

    [Required(ErrorMessage = "Department Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Department Name သည် စာလုံးရေ ၁၅၀ ထက် မကျော်ရပါ။")]
    public string? DepartmentName { get; set; }
    public string? ModifiedBy { get; set; }
}

public class DepartmentResponseModel : ActionResponseModel
{
    public DepartmentModel? Data { get; set; }
}

public class DepartmentModel
{
    public int DepartmentId { get; set; }
    public int FacultyId { get; set; }
    public string? DepartmentName { get; set; }
}

// ==========================================
// ၁၀။ Book DTOs
// ==========================================
public class BookCreateRequestModel
{
    [Required(ErrorMessage = "Category ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Book Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150)]
    public string? BookName { get; set; }

    // Role: .jpg သို့မဟုတ် .png ဖိုင်ကို တိုက်ရိုက်လက်ခံရန်
    public IFormFile? ImageFile { get; set; }

    public string? CreatedBy { get; set; }
}

public class BookUpdateRequestModel
{
    [Required(ErrorMessage = "Category ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Book Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150)]
    public string? BookName { get; set; }

    // Role: ပုံအသစ် ပြောင်းချင်ရင် တင်ရန်
    public IFormFile? ImageFile { get; set; }

    public string? ModifiedBy { get; set; }
}

public class BookResponseModel : ActionResponseModel
{
    public BookModel? Data { get; set; }
}

public class BookModel
{
    public int BookId { get; set; }
    public int CategoryId { get; set; }
    public string? BookName { get; set; }
    public string? Image { get; set; }
}

// ==========================================
// ၁၁။ Subject DTOs
// ==========================================
public class SubjectCreateRequestModel
{
    [Required(ErrorMessage = "Semester ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, int.MaxValue, ErrorMessage = "မှန်ကန်သော Semester ID ကို ထည့်ပေးပါ။")]
    public int SemesterId { get; set; }

    [Required(ErrorMessage = "Subject Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Subject Name သည် စာလုံးရေ ၁၁၀ ထက် မကျော်ရပါ။")]
    public string? SubjectName { get; set; }

    [Required(ErrorMessage = "Subject Code သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50, ErrorMessage = "Subject Code သည် စာလုံးရေ ၅၀ ထက် မကျော်ရပါ။")]
    public string? SubjectCode { get; set; }
    public string? CreatedBy { get; set; }
}

public class SubjectUpdateRequestModel
{
    [Required(ErrorMessage = "Semester ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, int.MaxValue, ErrorMessage = "မှန်ကန်သော Semester ID ကို ထည့်ပေးပါ။")]
    public int SemesterId { get; set; }

    [Required(ErrorMessage = "Subject Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Subject Name သည် စာလုံးရေ ၁၅၀ ထက် မကျော်ရပါ။")]
    public string? SubjectName { get; set; }

    [Required(ErrorMessage = "Subject Code သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50, ErrorMessage = "Subject Code သည် စာလုံးရေ ၅၀ ထက် မကျော်ရပါ။")]
    public string? SubjectCode { get; set; }
    public string? ModifiedBy { get; set; }
}

public class SubjectResponseModel : ActionResponseModel
{
    public SubjectModel? Data { get; set; }
}

public class SubjectModel
{
    public int SubjectId { get; set; }
    public int SemesterId { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
}