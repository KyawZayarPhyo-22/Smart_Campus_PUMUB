namespace Smart_Campus_PUMUB.WebApi.Models;

using System.ComponentModel.DataAnnotations;

// ==========================================
// NewStudentAcc — Request & Response Models
// ==========================================

/// <summary>Admin manual create request</summary>
public class NewStudentAccCreateRequest
{
    [Required(ErrorMessage = "Full Name မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Email မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>Optional: link back to a RegisterAcc entry</summary>
    public int? RegisterAccId { get; set; }

    /// <summary>Who created this record</summary>
    public string? CreatedBy { get; set; }
}

/// <summary>Response / list item DTO</summary>
public class NewStudentAccResponse
{
    public int NewStudentAccId { get; set; }
    public int? RegisterAccId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Username { get; set; } = null!;

    /// <summary>'Active' or 'Inactive'</summary>
    public string AccountStatus { get; set; } = "Active";
    public bool MustChangePassword { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>Toggle Active / Inactive status</summary>
public class NewStudentAccUpdateStatusRequest
{
    /// <summary>'Active' or 'Inactive'</summary>
    [Required]
    public string AccountStatus { get; set; } = null!;

    public string? ModifiedBy { get; set; }
}

/// <summary>Login request for new-student account</summary>
public class NewStudentAccLoginRequest
{
    [Required(ErrorMessage = "Username မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Password မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public string Password { get; set; } = null!;
}

/// <summary>Generic action response</summary>
public class NewStudentAccActionResponse
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

/// <summary>Paged list response</summary>
public class NewStudentAccPagedResponse
{
    public bool IsSuccess { get; set; }
    public List<NewStudentAccResponse> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}
