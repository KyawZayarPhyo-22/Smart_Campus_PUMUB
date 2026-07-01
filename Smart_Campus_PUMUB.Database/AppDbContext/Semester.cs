using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Semester")]
public partial class Semester
{
    [Key]
    [Column("Semester_Id")]
    public int SemesterId { get; set; }

    [Column("Semester_Name")]
    [StringLength(100)]
    public string SemesterName { get; set; } = null!;

    /// <summary>
    /// Ordinal position of this semester (1 = first, 9 = last). Used for progression validation.
    /// </summary>
    [Column("Sequence")]
    public int? Sequence { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [InverseProperty("Semester")]
    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
