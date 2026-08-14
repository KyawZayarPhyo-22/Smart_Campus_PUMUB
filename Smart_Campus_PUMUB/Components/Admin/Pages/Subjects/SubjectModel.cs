namespace Smart_Campus_PUMUB.WebApi.Models;

using System.ComponentModel.DataAnnotations;
using Smart_Campus_PUMUB.Database.AppDbContext;

// ==========================================
// ၁၁။ Subject DTOs
// ==========================================
public class SubjectCreateRequestModel
{
    [Required(ErrorMessage = "Semester ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, int.MaxValue, ErrorMessage = "မှန်ကန်သော Semester ID ကို ထည့်ပေးပါ။")]
    public int SemesterId { get; set; }

    public int? FacultyId { get; set; }
    public int? MajorId { get; set; }

    [Required(ErrorMessage = "Subject Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Subject Name သည် စာလုံးရေ ၁၁၀ ထက် မကျော်ရပါ။")]
    public string? SubjectName { get; set; }

    [Required(ErrorMessage = "Subject Code သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50, ErrorMessage = "Subject Code သည် စာလုံးရေ ၅၀ ထက် မကျော်ရပါ။")]
    public string? SubjectCode { get; set; }

    [Required(ErrorMessage = "Subject Type သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, 2, ErrorMessage = "မှန်ကန်သော Subject Type ကို ရွေးချယ်ပေးပါ။")]
    public EnumSubjectType SubjectType { get; set; } = EnumSubjectType.None;

    public string? CreatedBy { get; set; }

    public List<int> PrerequisiteSubjectIds { get; set; } = new();
}

public class SubjectUpdateRequestModel
{
    [Required(ErrorMessage = "Semester ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, int.MaxValue, ErrorMessage = "မှန်ကန်သော Semester ID ကို ထည့်ပေးပါ။")]
    public int SemesterId { get; set; }

    public int? FacultyId { get; set; }
    public int? MajorId { get; set; }

    [Required(ErrorMessage = "Subject Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150, ErrorMessage = "Subject Name သည် စာလုံးရေ ၁၅၀ ထက် မကျော်ရပါ။")]
    public string? SubjectName { get; set; }

    [Required(ErrorMessage = "Subject Code သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50, ErrorMessage = "Subject Code သည် စာလုံးရေ ၅၀ ထက် မကျော်ရပါ။")]
    public string? SubjectCode { get; set; }

    [Required(ErrorMessage = "Subject Type သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(1, 2, ErrorMessage = "မှန်ကန်သော Subject Type ကို ရွေးချယ်ပေးပါ။")]
    public EnumSubjectType SubjectType { get; set; } = EnumSubjectType.None;

    public string? ModifiedBy { get; set; }

    public List<int> PrerequisiteSubjectIds { get; set; } = new();
}

public class SubjectResponseModel : ActionResponseModel
{
    public SubjectModel? Data { get; set; }
}

public class SubjectModel
{
    public int SubjectId { get; set; }
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public int? MajorId { get; set; }
    public string? MajorName { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public EnumSubjectType SubjectType { get; set; } = EnumSubjectType.None;
    public string SubjectTypeName => SubjectType.ToString();

    public List<int> PrerequisiteSubjectIds { get; set; } = new();
}