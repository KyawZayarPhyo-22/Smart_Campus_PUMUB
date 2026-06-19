using Microsoft.AspNetCore.Http;
using System;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class StudentRegistrationCreateRequestModel
{
    public int? UserId { get; set; }
    public string? AdmissionSerialNo { get; set; }
    public string? academic_year_range { get; set; }
    public string? academic_year_level { get; set; }
    public string? major { get; set; }
    public string? roll_no { get; set; }
    public string? university_reg_no { get; set; }
    public int? admission_year { get; set; }
    public string? student_name_mm { get; set; }
    public string? student_name_en { get; set; }
    public string? mother_name { get; set; }
    public string? father_name { get; set; }
    public string? gender_relation { get; set; } // Dropdown Field
    public string? ethnicity { get; set; }
    public string? religion { get; set; }
    public string? pob { get; set; }
    public string? birth_place_region { get; set; }
    public string? student_nrc_no { get; set; }
    public string? nationality_status { get; set; }
    public DateTime dob { get; set; }
    public string? email { get; set; }
    public string? blood_type { get; set; } // Dropdown Field
    public string? covid_vaccine_status { get; set; }
    public string? current_address { get; set; }
    public string? permanent_address_mm { get; set; }
    public string? permanent_address_en { get; set; }
    public string? matric_roll_no { get; set; }
    public int matric_passed_year { get; set; }
    public string? exam_center { get; set; }
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
    public string? nrc_state { get; set; }     // Dropdown ဖိုး (ဥပမာ - "11")
    public string? nrc_township { get; set; }  // Dropdown ဖိုး (ဥပမာ - "THAKANA")
    public string? nrc_type { get; set; }
    public string? nrc_number { get; set; }    // Textbox ရိုက်ချက် (ဥပမာ - "122344")

    // 📷 Images Upload Fields
    public IFormFile? StudentImageFile { get; set; }
    public IFormFile? SignatureImageFile { get; set; }

}


public class StudentRegistrationUpdateRequestModel : StudentRegistrationCreateRequestModel
{
    public string? modified_by { get; set; }
}

public class StudentRegistrationStatusPatchModel
{
    public string Status { get; set; } = null!; // Pending Confirmation, Approved, Rejected. Pending accepted for legacy clients.
    public string? modified_by { get; set; }
}

public class StudentRegistrationResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
