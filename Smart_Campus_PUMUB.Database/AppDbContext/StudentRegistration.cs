using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Student_Registrations")]
public partial class StudentRegistration
{
    [Key]
    [Column("registration_id")]
    public int RegistrationId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("admission_serial_no")]
    [StringLength(50)]
    [Unicode(false)]
    public string? AdmissionSerialNo { get; set; }

    [Column("academic_year_range")]
    [StringLength(20)]
    [Unicode(false)]
    public string AcademicYearRange { get; set; } = null!;

    [Column("academic_year_level")]
    [StringLength(50)]
    public string AcademicYearLevel { get; set; } = null!;

    [Column("major")]
    [StringLength(100)]
    public string Major { get; set; } = null!;

    [Column("roll_no")]
    [StringLength(20)]
    [Unicode(false)]
    public string? RollNo { get; set; }

    [Column("university_reg_no")]
    [StringLength(50)]
    [Unicode(false)]
    public string? UniversityRegNo { get; set; }

    [Column("admission_year")]
    public int? AdmissionYear { get; set; }

    [Column("application_date", TypeName = "datetime")]
    public DateTime? ApplicationDate { get; set; }

    [Column("student_name_mm")]
    [StringLength(100)]
    public string StudentNameMm { get; set; } = null!;

    [Column("student_name_en")]
    [StringLength(100)]
    [Unicode(false)]
    public string StudentNameEn { get; set; } = null!;

    [Column("mother_name")]
    [StringLength(100)]
    public string MotherName { get; set; } = null!;

    [Column("father_name")]
    [StringLength(100)]
    public string FatherName { get; set; } = null!;

    [Column("gender_relation")]
    [StringLength(50)]
    public string GenderRelation { get; set; } = null!;

    [Column("ethnicity")]
    [StringLength(50)]
    public string Ethnicity { get; set; } = null!;

    [Column("religion")]
    [StringLength(50)]
    public string Religion { get; set; } = null!;

    [Column("pob")]
    [StringLength(255)]
    public string Pob { get; set; } = null!;

    [Column("birth_place_region")]
    [StringLength(100)]
    public string BirthPlaceRegion { get; set; } = null!;

    [Column("student_nrc_no")]
    [StringLength(50)]
    [Unicode(false)]
    public string StudentNrcNo { get; set; } = null!;

    [Column("nationality_status")]
    [StringLength(100)]
    public string NationalityStatus { get; set; } = null!;

    [Column("dob")]
    public DateOnly Dob { get; set; }

    [Column("email")]
    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [Column("blood_type")]
    [StringLength(5)]
    [Unicode(false)]
    public string BloodType { get; set; } = null!;

    [Column("covid_vaccine_status")]
    [StringLength(255)]
    public string? CovidVaccineStatus { get; set; }

    [Column("current_address")]
    public string? CurrentAddress { get; set; }

    [Column("permanent_address_mm")]
    public string PermanentAddressMm { get; set; } = null!;

    [Column("permanent_address_en")]
    [Unicode(false)]
    public string PermanentAddressEn { get; set; } = null!;

    [Column("matric_roll_no")]
    [StringLength(20)]
    [Unicode(false)]
    public string MatricRollNo { get; set; } = null!;

    [Column("matric_passed_year")]
    public int MatricPassedYear { get; set; }

    [Column("exam_center")]
    [StringLength(150)]
    public string ExamCenter { get; set; } = null!;

    [Column("father_occupation")]
    [StringLength(150)]
    public string? FatherOccupation { get; set; }

    [Column("mother_occupation")]
    [StringLength(150)]
    public string? MotherOccupation { get; set; }

    [Column("past_exam_major")]
    [StringLength(100)]
    public string? PastExamMajor { get; set; }

    [Column("past_exam_roll_no")]
    [StringLength(20)]
    [Unicode(false)]
    public string? PastExamRollNo { get; set; }

    [Column("past_exam_year")]
    public int? PastExamYear { get; set; }

    [Column("past_exam_status")]
    [StringLength(50)]
    public string? PastExamStatus { get; set; }

    [Column("previous_year_roll_no")]
    [StringLength(20)]
    [Unicode(false)]
    public string? PreviousYearRollNo { get; set; }

    [Column("guardian_name")]
    [StringLength(100)]
    public string? GuardianName { get; set; }

    [Column("guardian_relationship")]
    [StringLength(50)]
    public string? GuardianRelationship { get; set; }

    [Column("guardian_occupation")]
    [StringLength(150)]
    public string? GuardianOccupation { get; set; }

    [Column("guardian_address_phone")]
    public string? GuardianAddressPhone { get; set; }

    [Column("app_guardian_name")]
    [StringLength(100)]
    public string? AppGuardianName { get; set; }

    [Column("app_guardian_nrc")]
    [StringLength(50)]
    [Unicode(false)]
    public string? AppGuardianNrc { get; set; }

    [Column("app_guardian_phone")]
    [StringLength(20)]
    [Unicode(false)]
    public string? AppGuardianPhone { get; set; }

    [Column("app_guardian_address")]
    public string? AppGuardianAddress { get; set; }

    [Column("app_student_name")]
    [StringLength(100)]
    public string? AppStudentName { get; set; }

    [Column("app_student_phone")]
    [StringLength(20)]
    [Unicode(false)]
    public string? AppStudentPhone { get; set; }

    [Column("stipend_requested")]
    public bool? StipendRequested { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("created_datetime", TypeName = "datetime")]
    public DateTime? CreatedDatetime { get; set; }

    [Column("created_by")]
    public string? CreatedBy { get; set; }

    [Column("modified_datetime", TypeName = "datetime")]
    public DateTime? ModifiedDatetime { get; set; }

    [Column("modified_by")]
    public string? ModifiedBy { get; set; }

    [Column("is_delete")]
    public bool? IsDelete { get; set; }

    [InverseProperty("Registration")]
    public virtual ICollection<RegistrationPayment> RegistrationPayments { get; set; } = new List<RegistrationPayment>();

    [ForeignKey("UserId")]
    [InverseProperty("StudentRegistrations")]
    public virtual User? User { get; set; }
}
