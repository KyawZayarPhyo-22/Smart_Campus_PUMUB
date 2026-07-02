namespace Smart_Campus_PUMUB.WebApi.Models;

using System.ComponentModel.DataAnnotations;

// ==========================================
// RegisterAcc — Request & Response Models
// ==========================================

/// <summary>Semester I ကျောင်းသားအသစ် Account Request ပေးပို့ရန်</summary>
public class RegisterAccCreateRequest
{
    [Required(ErrorMessage = "Full Name မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [EmailAddress(ErrorMessage = "မှန်ကန်သော Email ဖော်မတ် ထည့်ပေးပါ။")]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "တက္ကသိုလ်ဝင်ရတဲ့ Form No မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50)]
    public string FormNo { get; set; } = null!;

    [Required(ErrorMessage = "တက္ကသိုလ်ဝင်ခုံအမှတ် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50)]
    public string ExamSeatNo { get; set; } = null!;
}

/// <summary>Admin table row data</summary>
public class RegisterAccListItem
{
    public int RegisterAccId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? FormNo { get; set; }
    public string? ExamSeatNo { get; set; }
    public string? Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? ReviewedDateTime { get; set; }
    public string? ReviewedBy { get; set; }
}

/// <summary>Approve or Reject action payload</summary>
public class RegisterAccActionRequest
{
    /// <summary>"Approved" or "Rejected"</summary>
    public string Status { get; set; } = null!;

    /// <summary>Optional reason shown in rejection email</summary>
    public string? RejectionReason { get; set; }

    public string? ReviewedBy { get; set; }
}

/// <summary>Generic action response</summary>
public class RegisterAccActionResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

/// <summary>Paged list response wrapper</summary>
public class RegisterAccPagedResponse
{
    public bool IsSuccess { get; set; }
    public List<RegisterAccListItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
