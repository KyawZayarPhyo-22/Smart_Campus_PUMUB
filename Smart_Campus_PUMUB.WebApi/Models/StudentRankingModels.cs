using System;
using System.Collections.Generic;

namespace Smart_Campus_PUMUB.WebApi.Models
{
    /// <summary>
    /// Generic Paged Result Container for API Responses
    /// </summary>
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Model for Subject-wise Student Ranking (ဘာသာရပ်အလိုက် အဆင့်သတ်မှတ်ချက်)
    /// </summary>
    public class StudentSubjectRankItemModel
    {
        public int Rank { get; set; }
        public int StudentId { get; set; }
        public int? RegistrationId { get; set; }
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? FacultyName { get; set; }
        public string? MajorName { get; set; }
        public string? AcademicYear { get; set; }
        public int SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int CreditUnit { get; set; } = 3;
        public decimal? MarksObtained { get; set; }
        public string Grade { get; set; } = string.Empty;
        public decimal GradePoint { get; set; }
        public decimal GradePointEarned { get; set; }
        public bool IsPass { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReexamGrade { get; set; }
        public bool? ReexamIsPass { get; set; }
        public bool IsDegreeEligible { get; set; } = true;
        public string EligibilityStatus { get; set; } = "Eligible";
    }

    /// <summary>
    /// Model for Semester Total Marks & GPA Ranking (Semester အလိုက် ဘာသာစုံအမှတ်စုစုပေါင်း အဆင့်သတ်မှတ်ချက်)
    /// </summary>
    public class StudentSemesterRankItemModel
    {
        public int Rank { get; set; }
        public int StudentId { get; set; }
        public int? RegistrationId { get; set; }
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? FacultyName { get; set; }
        public string? MajorName { get; set; }
        public string? AcademicYear { get; set; }
        public int SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public int TotalSubjectsCount { get; set; }
        public int TotalCredits { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal AverageMarks { get; set; }
        public decimal TotalGradePointsEarned { get; set; }
        public decimal SemesterGPA { get; set; }
        public bool IsPassAll { get; set; }
        public int FailedSubjectsCount { get; set; }
        public bool IsDegreeEligible { get; set; } = true;
        public string EligibilityStatus { get; set; } = "Eligible";
        public List<SubjectGradeDetailDto> SubjectDetails { get; set; } = new();
    }

    /// <summary>
    /// Individual subject grade detail for transcript modal & semester breakdown
    /// </summary>
    public class SubjectGradeDetailDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int CreditUnit { get; set; }
        public decimal? MarksObtained { get; set; }
        public string Grade { get; set; } = string.Empty;
        public decimal GradePoint { get; set; }
        public decimal GradePointEarned { get; set; }
        public bool IsPass { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReexamGrade { get; set; }
    }

    /// <summary>
    /// Model for Master Degree Eligibility Determination (CGPA >= 3.00 မဟာဘွဲ့ အရည်အချင်းစစ်)
    /// </summary>
    public class StudentMasterEligibilityItemModel
    {
        public int Rank { get; set; }
        public int StudentId { get; set; }
        public int? RegistrationId { get; set; }
        public string? RollNo { get; set; }
        public string? StudentName { get; set; }
        public string? FacultyName { get; set; }
        public string? MajorName { get; set; }
        public string? AcademicYear { get; set; }
        public int CompletedSemestersCount { get; set; }
        public int TotalCompletedCredits { get; set; }
        public decimal TotalCumulativeMarks { get; set; }
        public decimal CumulativeGPA { get; set; } // CGPA
        public bool IsMasterEligible { get; set; } // CGPA >= 3.00m && IsGraduated
        public bool IsGraduated { get; set; }
        public bool IsDisqualified { get; set; }
        public string MasterEligibilityStatus { get; set; } = string.Empty; // "MasterEligible", "BachelorOnly", "CgpaIneligible", "Disqualified", "Studying"
        public string StatusBadgeClass { get; set; } = string.Empty;
        public string StatusBadgeTextMm { get; set; } = string.Empty;
        public string StatusBadgeTextEn { get; set; } = string.Empty;
        public List<SemesterGpaSummaryDto> SemesterHistory { get; set; } = new();
    }

    /// <summary>
    /// Semester-by-semester GPA history item
    /// </summary>
    public class SemesterGpaSummaryDto
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal SemesterGPA { get; set; }
        public bool IsPassAll { get; set; }
    }

    /// <summary>
    /// Executive Summary & KPI Stats for Dashboard Cards
    /// </summary>
    public class StudentRankingSummaryStatsDto
    {
        public int TotalStudentsEvaluated { get; set; }
        public int MasterEligibleCount { get; set; }
        public decimal MasterEligiblePercentage { get; set; }
        public int NonEligibleCount { get; set; }
        public decimal HighestCGPA { get; set; }
        public string? TopStudentName { get; set; }
        public string? TopStudentRollNo { get; set; }
        public decimal FacultyAverageCGPA { get; set; }
        public int TotalDistinctionStudents { get; set; } // CGPA >= 3.67 (A / A+)
    }

    /// <summary>
    /// Filter dropdown options DTO
    /// </summary>
    public class StudentRankingFilterOptionsDto
    {
        public List<string> Faculties { get; set; } = new();
        public List<MajorDropdownItemDto> Majors { get; set; } = new();
        public List<string> AcademicYears { get; set; } = new();
        public List<SemesterDropdownItemDto> Semesters { get; set; } = new();
        public List<SubjectDropdownItemDto> Subjects { get; set; } = new();
    }

    public class MajorDropdownItemDto
    {
        public int MajorId { get; set; }
        public string MajorName { get; set; } = string.Empty;
        public string? FacultyName { get; set; }
    }

    public class SemesterDropdownItemDto
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int? Sequence { get; set; }
    }

    public class SubjectDropdownItemDto
    {
        public int SubjectId { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string? FacultyName { get; set; }
        public string? MajorName { get; set; }
        public string? SemesterName { get; set; }
        public int? SemesterId { get; set; }
    }
}
