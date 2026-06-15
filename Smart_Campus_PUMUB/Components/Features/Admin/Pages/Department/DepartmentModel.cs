using System.ComponentModel.DataAnnotations;

namespace Smart_Campus_PUMUB.WebApi.Models;

// ==========================================
// ၉။ Department DTOs (Blazor Frontend Local Copies)
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

public class DepartmentResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public DepartmentModel? Data { get; set; }
}

public class DepartmentModel
{
    public int DepartmentId { get; set; }
    public int FacultyId { get; set; }
    public string? DepartmentName { get; set; }
    public string? FacultyName { get; set; }
}

