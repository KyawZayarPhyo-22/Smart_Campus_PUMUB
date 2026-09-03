using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Subject")]
public partial class Subject
{
    [Key]
    [Column("Subject_Id")]
    public int SubjectId { get; set; }

    [Column("Semester_Id")]
    public int SemesterId { get; set; }

    [Column("Faculty_Id")]
    public int? FacultyId { get; set; }

    [Column("Major_Id")]
    public int? MajorId { get; set; }

    [Column("Subject_Name")]
    [StringLength(150)]
    public string SubjectName { get; set; } = null!;

    [Column("Subject_Code")]
    [StringLength(50)]
    [Unicode(false)]
    public string SubjectCode { get; set; } = null!;

    [Column("Subject_Type")]
    public EnumSubjectType SubjectType { get; set; } = EnumSubjectType.None;

    [Column("Credit")]
    public int Credit { get; set; } = 3;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [ForeignKey("SemesterId")]
    [InverseProperty("Subjects")]
    public virtual Semester Semester { get; set; } = null!;

    [ForeignKey("FacultyId")]
    public virtual Faculty? Faculty { get; set; }

    [ForeignKey("MajorId")]
    [InverseProperty("Subjects")]
    public virtual Major? Major { get; set; }

    [InverseProperty("Subject")]
    public virtual ICollection<SubjectPrerequisite> Prerequisites { get; set; } = new List<SubjectPrerequisite>();

    [InverseProperty("PrerequisiteSubject")]
    public virtual ICollection<SubjectPrerequisite> PrerequisiteFor { get; set; } = new List<SubjectPrerequisite>();

    [InverseProperty("Subject")]
    public virtual ICollection<StudentSubjectEnrollment> Enrollments { get; set; } = new List<StudentSubjectEnrollment>();

    [InverseProperty("Subject")]
    public virtual ICollection<StudentSubjectResult> Results { get; set; } = new List<StudentSubjectResult>();

}
