using System.Collections.Generic;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class GradeModel
{
    public int GradeId { get; set; }
    public string Name { get; set; } = null!;
    public decimal GradePoint { get; set; } = 0.0m;
    public string? Status { get; set; }
    public decimal? MinMark { get; set; }
    public decimal? MaxMark { get; set; }
}

public class GradeListResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public List<GradeModel>? Data { get; set; }
}

public class SubjectGradeBindingModel
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public string? SemesterName { get; set; }
    public string? Grade { get; set; }
    public string? ReexamGrade { get; set; }
    public bool? ReexamIsPass { get; set; }
    public bool IsRetake { get; set; } = false;
    public bool IsCarriedOver { get; set; } = false;
    public bool IsSubjectDisqualified { get; set; } = false;
    public bool IsReexam { get; set; } = false;
    public int AttemptNumber { get; set; } = 1;
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
    public bool IsRetake { get; set; }
    public bool IsCarriedOver { get; set; }
    public bool IsSubjectDisqualified { get; set; } = false;
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
