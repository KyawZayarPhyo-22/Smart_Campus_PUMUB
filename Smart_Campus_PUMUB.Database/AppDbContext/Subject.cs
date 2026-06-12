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

    [Column("Subject_Name")]
    [StringLength(150)]
    public string SubjectName { get; set; } = null!;

    [Column("Subject_Code")]
    [StringLength(50)]
    [Unicode(false)]
    public string SubjectCode { get; set; } = null!;

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
}
