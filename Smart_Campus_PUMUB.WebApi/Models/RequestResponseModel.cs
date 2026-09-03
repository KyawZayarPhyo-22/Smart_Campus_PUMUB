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
    public DateTime CreatedDateTime { get; set; }
}

// ==========================================
// ၈။ Payment Fees DTOs
// ==========================================
public class PaymentFeeCreateRequestModel
{
    [Required(ErrorMessage = "Class Year သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(50, ErrorMessage = "Class Year သည် စာလုံးရေ ၅၀ ထက် မကျော်ရပါ။")]
    public string? ClassYear { get; set; }

    [StringLength(100, ErrorMessage = "Fee Name သည် စာလုံးရေ ၁၀၀ ထက် မကျော်ရပါ။")]
    public string? FeeName { get; set; }

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

    [StringLength(100, ErrorMessage = "Fee Name သည် စာလုံးရေ ၁၀၀ ထက် မကျော်ရပါ။")]
    public string? FeeName { get; set; }

    [Required(ErrorMessage = "Monthly Amount သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [Range(0.00, 99999999.99, ErrorMessage = "Amount သည် တရားဝင်သော ပမာဏ ဖြစ်ရပါမည်။")]
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
    public string? FeeName { get; set; }
    public decimal MontlyAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
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

    public string? FacultyName { get; set; }
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

    // Cover image file (.jpg / .png)
    public IFormFile? ImageFile { get; set; }

    // PDF file upload
    public IFormFile? PdfFile { get; set; }

    public string? CreatedBy { get; set; }
}

public class BookUpdateRequestModel
{
    [Required(ErrorMessage = "Category ID သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Book Name သည် မဖြစ်မနေ လိုအပ်ပါသည်။")]
    [StringLength(150)]
    public string? BookName { get; set; }

    // Cover image update
    public IFormFile? ImageFile { get; set; }

    // PDF file update (optional)
    public IFormFile? PdfFile { get; set; }

    public string? ModifiedBy { get; set; }

    public string? ExistingImage { get; set; }
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
    public string? FilePath { get; set; }
    public string? CategoryName { get; set; }
}

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
    public Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType SubjectType { get; set; } = Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.None;

    [Range(0, 15, ErrorMessage = "Credit Point သည် ၀ မှ ၁၅ ကြား ဖြစ်ရပါမည်။")]
    public int Credit { get; set; } = 3;

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
    public Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType SubjectType { get; set; } = Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.None;

    [Range(0, 15, ErrorMessage = "Credit Point သည် ၀ မှ ၁၅ ကြား ဖြစ်ရပါမည်။")]
    public int Credit { get; set; } = 3;

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
    public string? SemesterName { get; set; }
    public int SemesterId { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public int? MajorId { get; set; }
    public string? MajorName { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public int Credit { get; set; } = 3;
    public Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType SubjectType { get; set; } = Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.None;
    public string SubjectTypeName => SubjectType.ToString();
    public List<int> PrerequisiteSubjectIds { get; set; } = new();
    public string? PrerequisiteInfo { get; set; }
    public bool IsPrerequisiteSatisfied { get; set; } = true;
    public string? PrerequisiteStatusMessage { get; set; }
    public bool IsRetake { get; set; } = false;
    public bool IsCarriedOver { get; set; } = false;
    public bool IsSelected { get; set; } = false;
    public bool IsSubjectDisqualified { get; set; } = false;
    public bool IsReexamPending { get; set; } = false;
}

public class FacultySemesterCreditModel
{
    public int Id { get; set; }
    public int FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int? Sequence { get; set; }
    public int RequiredCredits { get; set; } = 24;
    public int? MinCredits { get; set; }
    public int? MaxCredits { get; set; }
}


public class FacultySemesterCreditUpdateRequest
{
    public int FacultyId { get; set; }
    public int SemesterId { get; set; }
    public int RequiredCredits { get; set; } = 24;
    public int? MinCredits { get; set; } = 18;
    public int? MaxCredits { get; set; } = 24;
}

public class StudentEnrollmentResultModel
{
    public int EnrollmentId { get; set; }
    public int? RegistrationId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? RollNo { get; set; }
    public string? Major { get; set; }
    public string? MajorName { get; set; }
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public decimal? MaxMarks { get; set; }
    public decimal? MarksObtained { get; set; }
    public string? Grade { get; set; }
    public bool IsPass { get; set; }
}

public class StudentSubjectGradeItemModel
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? SemesterName { get; set; }
    public Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType SubjectType { get; set; } = Smart_Campus_PUMUB.Database.AppDbContext.EnumSubjectType.None;
    public string SubjectTypeName => SubjectType.ToString();
    public string? PrerequisiteInfo { get; set; }
    public int CreditUnit { get; set; } = 3;
    public decimal? MarksObtained { get; set; }
    public string? Grade { get; set; }
    public decimal GradePoint { get; set; } = 0.0m;
    public decimal GradePointEarned { get; set; } = 0.0m;
    public string Status { get; set; } = string.Empty;
    public int? ResultId { get; set; }
    public bool IsPass { get; set; }
    public string? ReexamGrade { get; set; }
    public decimal? ReexamMarksObtained { get; set; }
    public bool? ReexamIsPass { get; set; }
    public bool IsRetake { get; set; } = false;
    public bool IsCarriedOver { get; set; } = false;
    public bool IsSubjectDisqualified { get; set; } = false;
    public bool IsReexam { get; set; } = false;
    public int AttemptNumber { get; set; } = 1;
}

public class StudentEnrollmentDetailResponseModel
{
    public int RegistrationId { get; set; }
    public int? StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RollNo { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public List<decimal> PriorSemesterGPAs { get; set; } = new();
    public List<StudentSubjectGradeItemModel> Subjects { get; set; } = new();
}

public class SaveStudentGradesRequestModel
{
    public int RegistrationId { get; set; }
    public int? StudentId { get; set; }
    public int SemesterId { get; set; }
    public List<SaveGradeItemModel> Grades { get; set; } = new();
}

public class SaveGradeItemModel
{
    public int SubjectId { get; set; }
    public string? Grade { get; set; }
    public decimal? MarksObtained { get; set; }
}

public class SaveReexamGradesRequestModel
{
    public int RegistrationId { get; set; }
    public int? StudentId { get; set; }
    public int SemesterId { get; set; }
    public List<SaveReexamGradeItemModel> ReexamGrades { get; set; } = new();
}

public class SaveReexamGradeItemModel
{
    public int SubjectId { get; set; }
    public string? ReexamGrade { get; set; }
    public decimal? ReexamMarksObtained { get; set; }
}

public class StudentRetakeStatusModel
{
    public int TotalRetakesTaken { get; set; }
    public int MaxRetakeLimit { get; set; } = 25;
    public int RemainingRetakes { get; set; }
    public double UsagePercentage { get; set; }
    public string DangerLevel { get; set; } = "Safe"; // Safe, Warning, HighDanger, Disqualified
    public string DangerStatusText { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "#10b981";
    public bool IsDisqualified { get; set; } = false;
    public int FailedSubjectsCount { get; set; }
    public List<string> RetakeHistoryList { get; set; } = new();
}

public class StudentGraduationStatusModel
{
    public int? StudentId { get; set; }
    public int? UserId { get; set; }
    public int? NewStudentAccId { get; set; }
    public string RollNo { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public int TotalEarnedCredits { get; set; }
    public int TargetGraduationCredits { get; set; } = 155;
    public int Sem1To7Credits { get; set; }
    public int TargetSem1To7Credits { get; set; } = 143;
    public int Sem8Credits { get; set; }
    public int TargetSem8Credits { get; set; } = 12;
    public double ProgressPercentage { get; set; }
    public bool IsGraduated { get; set; }
    public string GraduationStatus { get; set; } = "Studying"; // "Graduated" or "In Progress"
    public string GraduationStatusText { get; set; } = string.Empty;
    public int CompletedSemestersCount { get; set; }
    public int TotalPassedSubjects { get; set; }
    public int PendingFailedSubjects { get; set; }
    public bool AllRetakesCleared { get; set; }
    public bool AllCurriculumCompleted { get; set; }
    public bool HasDisqualifiedSubject { get; set; } = false;
    public DateTime? GraduationDate { get; set; }
    public decimal CumulativeGPA { get; set; } = 0.0m;
    public decimal TotalGradePointsEarned { get; set; } = 0.0m;
    public string OverallAcademicStanding { get; set; } = string.Empty;
    public List<SemesterGpaBreakdownModel> SemesterGpaList { get; set; } = new();
}

public class SemesterGpaBreakdownModel
{
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public int TotalCredits { get; set; }
    public decimal TotalGradePointsEarned { get; set; }
    public decimal SemesterGPA { get; set; }
    public decimal CumulativeGPAUpToThisSemester { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<SemesterSubjectResultItemModel> SubjectResults { get; set; } = new();
}

public class SemesterSubjectResultItemModel
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int CreditUnit { get; set; }
    public decimal? MarksObtained { get; set; }
    public string Grade { get; set; } = string.Empty;
    public decimal GradeScore { get; set; }
    public decimal GradePointEarned { get; set; }
    public string GradeStatus { get; set; } = string.Empty;
    public string? ReexamGrade { get; set; }
    public bool? ReexamIsPass { get; set; }
    public bool IsPass { get; set; }
}

public class SystemSettingModel
{
    public int SettingId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ==========================================
// Student Subject Selection History Models
// ==========================================
public class StudentSubjectItemModel
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int Credit { get; set; }
    public string SubjectType { get; set; } = string.Empty; // Core, Elective, Retake, Carried-over
    public string? SemesterName { get; set; }
    public string? MajorName { get; set; }
    public string? FacultyName { get; set; }
    public string? Grade { get; set; }
    public bool? IsPass { get; set; }
    public string? ReexamGrade { get; set; }
    public bool? ReexamIsPass { get; set; }
    public bool IsSubjectDisqualified { get; set; } = false;
    public bool IsRetake { get; set; }
    public bool IsCarriedOver { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsEnrolledLater { get; set; } = false;
    public string? EnrolledLaterSemester { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Selected";
}

public class StudentSemesterSubjectSelectionModel
{
    public int RegistrationId { get; set; }
    public int SemesterId { get; set; }
    public string SemesterName { get; set; } = string.Empty;
    public string AcademicYearLevel { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string RollNo { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = string.Empty;
    public DateTime? RegistrationDate { get; set; }
    
    public int TotalSelectedCredits { get; set; }
    public int TotalEarnedCredits { get; set; }
    public int TotalUnselectedCredits { get; set; }
    public int SelectedCount => SelectedSubjects?.Count ?? 0;
    public int UnselectedCount => UnselectedSubjects?.Count ?? 0;
    public int PendingRequiredCount => UnselectedSubjects?.Count(s => s.IsRequired && !s.IsEnrolledLater) ?? 0;
    
    public List<StudentSubjectItemModel> SelectedSubjects { get; set; } = new();
    public List<StudentSubjectItemModel> UnselectedSubjects { get; set; } = new();
}

public class StudentSubjectSelectionResponseModel
{
    public int UserId { get; set; }
    public int? NewStudentAccId { get; set; }
    public int? StudentId { get; set; }
    public string StudentNameMm { get; set; } = string.Empty;
    public string StudentNameEn { get; set; } = string.Empty;
    public string RollNo { get; set; } = string.Empty;
    public string FacultyName { get; set; } = string.Empty;
    public string CurrentMajor { get; set; } = string.Empty;
    public int TotalEarnedCredits { get; set; }
    public int TotalPendingRequiredSubjects { get; set; }
    public int TotalPendingRequiredCredits { get; set; }
    public List<StudentSemesterSubjectSelectionModel> SemesterSelections { get; set; } = new();
}
