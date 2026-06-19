namespace Smart_Campus_PUMUB.WebApi.Models;

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
    public string? FullName { get; set; }
    public string CurrentClassYear { get; set; } = null!;
    public string CurrentMajor { get; set; } = null!;
    public string? CurrentRollNo { get; set; }
    public string Status { get; set; } = "Active";
    public string? Sem1_Result { get; set; }
    public string? Sem2_Result { get; set; }
    public string? Sem3_Result { get; set; }
    public string? Sem4_Result { get; set; }
    public string? Sem5_Result { get; set; }
    public string? Sem6_Result { get; set; }
    public string? Sem7_Result { get; set; }
    public string? Sem8_Result { get; set; }
    public string? Sem9_Result { get; set; }
}
public class StudentPatchRequestModel
{
    public string? CurrentClassYear { get; set; }
    public string? CurrentMajor { get; set; }
    public string? CurrentRollNo { get; set; }
    public string? Status { get; set; }
    public string? Sem1_Result { get; set; }
    public string? Sem2_Result { get; set; }
    public string? Sem3_Result { get; set; }
    public string? Sem4_Result { get; set; }
    public string? Sem5_Result { get; set; }
    public string? Sem6_Result { get; set; }
    public string? Sem7_Result { get; set; }
    public string? Sem8_Result { get; set; }
    public string? Sem9_Result { get; set; }
}

// 💡 StudentRegister.razor တွင် အသုံးပြုသော Registration Form Model
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
    public string? gender_relation { get; set; }
    public string? ethnicity { get; set; }
    public string? religion { get; set; }
    public string? pob { get; set; }
    public string? birth_place_region { get; set; }
    public string? student_nrc_no { get; set; }
    public string? nationality_status { get; set; }
    public DateTime dob { get; set; }
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
    public string? created_by { get; set; }
    public string? nrc_state { get; set; }
    public string? nrc_township { get; set; }
    public string? nrc_type { get; set; }
    public string? nrc_number { get; set; }
}

public class StudentRegistrationResponseModel
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
}
