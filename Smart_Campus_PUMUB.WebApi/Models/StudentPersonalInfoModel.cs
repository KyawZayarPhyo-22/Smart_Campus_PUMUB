using System;
using System.ComponentModel.DataAnnotations;

namespace Smart_Campus_PUMUB.WebApi.Models;

public class StudentPersonalInfoRequest
{
    public int? UserId { get; set; }
    public int? NewStudentAccId { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
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
    public string? gender_relation { get; set; }
    public string? ethnicity { get; set; }
    public string? religion { get; set; }
    public string? pob { get; set; }
    public string? birth_place_region { get; set; }
    public string? student_nrc_no { get; set; }
    public string? nationality_status { get; set; }
    public DateTime? dob { get; set; }
    public string? email { get; set; }
    public string? blood_type { get; set; }
    public string? covid_vaccine_status { get; set; }
    public string? current_address { get; set; }
    public string? permanent_address_mm { get; set; }
    public string? permanent_address_en { get; set; }
    public string? matric_roll_no { get; set; }
    public int? matric_passed_year { get; set; }
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
    public string? nrc_state { get; set; }
    public string? nrc_township { get; set; }
    public string? nrc_type { get; set; }
    public string? nrc_number { get; set; }
    public string? student_image { get; set; }
    public string? nrc_front_image { get; set; }
    public string? nrc_back_image { get; set; }
    public string? census_image { get; set; }
    public string? father_nrc_front_image { get; set; }
    public string? father_nrc_back_image { get; set; }
    public string? mother_nrc_front_image { get; set; }
    public string? mother_nrc_back_image { get; set; }
}

public class StudentPersonalInfoResponse : StudentPersonalInfoRequest
{
    public int Id { get; set; }
    public string? Status { get; set; }
    public bool IsGraduated { get; set; }
    public bool IsDisqualified { get; set; }
    public string? GraduationStatus { get; set; }
    public int EarnedCredits { get; set; }
    public StudentRetakeStatusModel? RetakeStatus { get; set; }
    public StudentModel? StudentData { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
}
