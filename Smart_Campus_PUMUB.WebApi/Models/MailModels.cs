using System.ComponentModel.DataAnnotations;

namespace Smart_Campus_PUMUB.WebApi.Models;

// ==========================================
// Mail DTOs
// ==========================================

/// <summary>Send a single email.</summary>
public class SendMailRequestModel
{
    [Required(ErrorMessage = "To Email မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [EmailAddress(ErrorMessage = "မှန်ကန်သော Email ပုံစံ ဖြည့်ပါ။")]
    public string? ToEmail { get; set; }

    public string? ToName { get; set; }

    [Required(ErrorMessage = "Subject မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(255, ErrorMessage = "Subject သည် ၂၅၅ လုံးထက် မကျော်ရပါ။")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Body မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public string? Body { get; set; }

    /// <summary>true = HTML body, false = plain text wrapped in &lt;pre&gt;</summary>
    public bool IsHtml { get; set; } = true;
}

/// <summary>A single recipient for bulk mail.</summary>
public class MailRecipientModel
{
    [Required(ErrorMessage = "Email မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [EmailAddress(ErrorMessage = "မှန်ကန်သော Email ပုံစံ ဖြည့်ပါ။")]
    public string? Email { get; set; }

    public string? Name { get; set; }
}

/// <summary>Send the same email to multiple recipients.</summary>
public class SendBulkMailRequestModel
{
    [Required]
    [MinLength(1, ErrorMessage = "အနည်းဆုံး Recipients တစ်ဦး လိုအပ်ပါသည်။")]
    public List<MailRecipientModel> Recipients { get; set; } = new();

    [Required(ErrorMessage = "Subject မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(255, ErrorMessage = "Subject သည် ၂၅၅ လုံးထက် မကျော်ရပါ။")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Body မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public string? Body { get; set; }

    public bool IsHtml { get; set; } = true;
}
