using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Student_Subject_Result")]
[Index(nameof(EnrollmentId), Name = "IX_Result_Enrollment")]
public class StudentSubjectResult
{
    [Key]
    [Column("Result_Id")]
    public int ResultId { get; set; }

    [Column("Enrollment_Id")]
    public int? EnrollmentId { get; set; }

    [Column("Student_Id")]
    public int? StudentId { get; set; }

    [Column("Subject_Id")]
    public int? SubjectId { get; set; }

    [Column("Semester_Id")]
    public int? SemesterId { get; set; }

    [Column("Registration_Id")]
    public int? RegistrationId { get; set; }

    [Column("Marks_Obtained", TypeName = "decimal(5, 2)")]
    public decimal? MarksObtained { get; set; }

    [Column("Max_Marks", TypeName = "decimal(5, 2)")]
    public decimal? MaxMarks { get; set; }

    [StringLength(5)]
    public string? Grade { get; set; }

    [Column("Is_Pass")]
    public bool IsPass { get; set; }

    [Column("Result_Date", TypeName = "datetime")]
    public DateTime? ResultDate { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    [ForeignKey("EnrollmentId")]
    [InverseProperty("Result")]
    public virtual StudentSubjectEnrollment? Enrollment { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Results")]
    public virtual Student? Student { get; set; }

    [ForeignKey("SubjectId")]
    [InverseProperty("Results")]
    public virtual Subject? Subject { get; set; }

    [ForeignKey("SemesterId")]
    public virtual Semester? Semester { get; set; }

    [ForeignKey("RegistrationId")]
    [InverseProperty("Results")]
    public virtual StudentRegistration? Registration { get; set; }
}
