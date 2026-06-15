

using System;
using System.Collections.Generic;
namespace Smart_Campus_PUMUB.WebApi.Models;



public class StudentRegistrationModel
{
    public int RegistrationId { get; set; }
    public int? UserId { get; set; }
    public string? AdmissionSerialNo { get; set; }
    public string AcademicYearRange { get; set; } = null!;
    public string AcademicYearLevel { get; set; } = null!;
    public string Major { get; set; } = null!;
    public string? RollNo { get; set; }
    public string? UniversityRegNo { get; set; }
    public int? AdmissionYear { get; set; }
    public DateTime ApplicationDate { get; set; }

    // Student Info
    public string StudentNameMm { get; set; } = null!;
    public string StudentNameEn { get; set; } = null!;
    public string MotherName { get; set; } = null!;
    public string FatherName { get; set; } = null!;
    public string GenderRelation { get; set; } = null!;
    public string Ethnicity { get; set; } = null!;
    public string Religion { get; set; } = null!;
    public string Pob { get; set; } = null!;
    public string BirthPlaceRegion { get; set; } = null!;
    public string StudentNrcNo { get; set; } = null!;
    public string NationalityStatus { get; set; } = null!;
    public DateTime Dob { get; set; }
    public string? Email { get; set; }
    public string BloodType { get; set; } = null!;
    public string? CovidVaccineStatus { get; set; }
    public string? CurrentAddress { get; set; }
    public string PermanentAddressMm { get; set; } = null!;
    public string PermanentAddressEn { get; set; } = null!;

    // Matriculation Info
    public string MatricRollNo { get; set; } = null!;
    public int MatricPassedYear { get; set; }
    public string ExamCenter { get; set; } = null!;
    public string? FatherOccupation { get; set; }
    public string? MotherOccupation { get; set; }

    // Past Exam Info
    public string? PastExamMajor { get; set; }
    public string? PastExamRollNo { get; set; }
    public int? PastExamYear { get; set; }
    public string? PastExamStatus { get; set; }
    public string? PreviousYearRollNo { get; set; }

    public class StudentRegistrationResponseModel
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }

    public class StudentRegistrationStatusPatchModel
    {
        public string Status { get; set; } = null!; // Approved, Rejected, Pending
        public string? modified_by { get; set; }
    }
    public class StudentModel
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string CurrentClassYear { get; set; } = null!;
        public string CurrentMajor { get; set; } = null!;
        public string? CurrentRollNo { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDateTime { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
        public string? ModifiedBy { get; set; }
        public bool? IsDelete { get; set; }
    }

    // Guardian Info
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? GuardianAddressPhone { get; set; }

    // Application Signatures
    public string? AppGuardianName { get; set; }
    public string? AppGuardianNrc { get; set; }
    public string? AppGuardianPhone { get; set; }
    public string? AppGuardianAddress { get; set; }
    public string? AppStudentName { get; set; }
    public string? AppStudentPhone { get; set; }

    // System & Audit Fields
    public bool StipendRequested { get; set; }
    public string Status { get; set; } = "Pending";
    public string? StudentImagePath { get; set; } // API မှ ပုံလမ်းကြောင်း ပြန်ပေးပါက ဖတ်ရန်
    public string? SignatureImagePath { get; set; }
    public DateTime CreatedDatetime { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDatetime { get; set; }
    public string? ModifiedBy { get; set; }
    public bool? IsDelete { get; set; }

    // ချိတ်ဆက်ထားသော ဒေတာများတွဲဖတ်ရန်
    public List<RegistrationPaymentModel> RegistrationPayments { get; set; } = new();
}

public class StudentCreateRequestModel
{
    public int UserId { get; set; }
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; } // ဥပမာ - MUB-1098
}

public class StudentUpdateRequestModel
{
    public int UserId{get;set;}
    public string? UserName {get;set;}
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; }
    public string Status { get; set; } = "Active";
}

public class StudentResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public StudentModel? Data { get; set; }
}

public class StudentModel
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string? UserName {get;set;}
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; }
    public string Status { get; set; } = "Active";
}
public class StudentPatchRequestModel
{
    public string? CurrentClassYear { get; set; }
    public string? CurrentMajor { get; set; }
    public string? CurrentRollNo { get; set; }
    public string? Status { get; set; }
}