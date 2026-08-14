using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Student")]
public partial class Student
{
    [Key]
    [Column("Student_Id")]
    public int StudentId { get; set; }

    [Column("User_Id")]
    public int UserId { get; set; }

    [Column("Current_Class_Year")]
    [StringLength(50)]
    public string CurrentClassYear { get; set; } = null!;

    [Column("Current_Major")]
    [StringLength(100)]
    public string CurrentMajor { get; set; } = null!;

    [Column("Current_Roll_No")]
    [StringLength(20)]
    [Unicode(false)]
    public string? CurrentRollNo { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [StringLength(20)]
    public string? Sem1_Result { get; set; }

    [StringLength(20)]
    public string? Sem2_Result { get; set; }

    [StringLength(20)]
    public string? Sem3_Result { get; set; }

    [StringLength(20)]
    public string? Sem4_Result { get; set; }

    [StringLength(20)]
    public string? Sem5_Result { get; set; }

    [StringLength(20)]
    public string? Sem6_Result { get; set; }

    [StringLength(20)]
    public string? Sem7_Result { get; set; }

    [StringLength(20)]
    public string? Sem8_Result { get; set; }

    [StringLength(20)]
    public string? Sem9_Result { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Students")]
    public virtual User User { get; set; } = null!;

    [Column("Student_Name")]
    [StringLength(150)]
    public string? StudentName { get; set; }

    [Column("Enrollment_No")]
    [StringLength(50)]
    public string? EnrollmentNo { get; set; }

    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("Faculty_Id")]
    public int? FacultyId { get; set; }

    [ForeignKey("FacultyId")]
    public virtual Faculty? Faculty { get; set; }

    [InverseProperty("Student")]
    public virtual ICollection<StudentSubjectEnrollment> Enrollments { get; set; } = new List<StudentSubjectEnrollment>();

    [InverseProperty("Student")]
    public virtual ICollection<StudentSubjectResult> Results { get; set; } = new List<StudentSubjectResult>();
}
