namespace Smart_Campus_PUMUB.WebApi.Models;

using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

// ==========================================
// Shared Base Response Model
// =======================================

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

    public string? SemesterName {get;set;}
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
}