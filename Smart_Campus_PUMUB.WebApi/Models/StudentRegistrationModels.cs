using Microsoft.AspNetCore.Http;
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
    public string MatricRollNo { get; set; } = null!;
    public int MatricPassedYear { get; set; }
    public string ExamCenter { get; set; } = null!;
    public string? FatherOccupation { get; set; }
    public string? MotherOccupation { get; set; }
    public string? PastExamMajor { get; set; }
    public string? PastExamRollNo { get; set; }
    public int? PastExamYear { get; set; }
    public string? PastExamStatus { get; set; }
    public string? PreviousYearRollNo { get; set; }
    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianOccupation { get; set; }
    public string? GuardianAddressPhone { get; set; }
    public string? AppGuardianName { get; set; }
    public string? AppGuardianNrc { get; set; }
    public string? AppGuardianPhone { get; set; }
    public string? AppGuardianAddress { get; set; }
    public string? AppStudentName { get; set; }
    public string? AppStudentPhone { get; set; }
    public bool StipendRequested { get; set; }
    public string Status { get; set; } = "Pending";
    public string? StudentImagePath { get; set; }
    public string? SignatureImagePath { get; set; }
}

public class StudentRegistrationCreateRequestModel
{
    public int? UserId { get; set; }
    public string? AdmissionSerialNo { get; set; }
    public string academic_year_range { get; set; } = null!;
    public string academic_year_level { get; set; } = null!;
    public string major { get; set; } = null!;
    public string? roll_no { get; set; }
    public string? university_reg_no { get; set; }
    public int? admission_year { get; set; }
    public string student_name_mm { get; set; } = null!;
    public string student_name_en { get; set; } = null!;
    public string mother_name { get; set; } = null!;
    public string father_name { get; set; } = null!;
    public string gender_relation { get; set; } = null!;
    public string ethnicity { get; set; } = null!;
    public string religion { get; set; } = null!;
    public string pob { get; set; } = null!;
    public string birth_place_region { get; set; } = null!;
    public string student_nrc_no { get; set; } = null!;
    public string nationality_status { get; set; } = null!;
    public DateTime dob { get; set; }
    public string? email { get; set; }
    public string blood_type { get; set; } = null!;
    public string? covid_vaccine_status { get; set; }
    public string? current_address { get; set; }
    public string permanent_address_mm { get; set; } = null!;
    public string permanent_address_en { get; set; } = null!;
    public string matric_roll_no { get; set; } = null!;
    public int matric_passed_year { get; set; }
    public string exam_center { get; set; } = null!;
    public string? father_occupation { get; set; }
    public string? mother_occupation { get; set; }
    public string? past_exam_major { get; set; }
    public string? past_exam_roll_no { get; set; }
    public int? past_exam_year { get; set; }
    public string? past_exam_status { get; set; }
    public string? previous_year_roll_no { get; set; }
    public string? guardian_name { get; set; }
    public string? guardian_relationship { get; set; }
    public string? guardian_occupation { get; set; }
    public string? guardian_address_phone { get; set; }
    public string? app_guardian_name { get; set; }
    public string? app_guardian_nrc { get; set; }
    public string? app_guardian_phone { get; set; }
    public string? app_guardian_address { get; set; }
    public string? app_student_name { get; set; }
    public string? app_student_phone { get; set; }
    public bool? stipend_requested { get; set; }
    public string? created_by { get; set; }
    public string? nrc_state { get; set; }
    public string? nrc_township { get; set; }
    public string? nrc_number { get; set; }

    public IFormFile? StudentImageFile { get; set; }
    public IFormFile? SignatureImageFile { get; set; }
}

public class StudentRegistrationStatusPatchModel
{
    public string Status { get; set; } = null!;
    public string? modified_by { get; set; }
}

public class StudentRegistrationResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}