using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Student_Subject_Enrollment")]
public class StudentSubjectEnrollment
{
    [Key]
    [Column("Enrollment_Id")]
    public int EnrollmentId { get; set; }

    [Column("Student_Id")]
    public int StudentId { get; set; }

    [Column("Subject_Id")]
    public int SubjectId { get; set; }

    [Column("Semester_Id")]
    public int SemesterId { get; set; }

    [Column("Enrollment_Date", TypeName = "datetime")]
    public DateTime EnrollmentDate { get; set; }

    public byte Status { get; set; } = 1;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("Enrollments")]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey("SubjectId")]
    [InverseProperty("Enrollments")]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey("SemesterId")]
    public virtual Semester Semester { get; set; } = null!;

    [InverseProperty("Enrollment")]
    public virtual StudentSubjectResult? Result { get; set; }
}
