using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Faculty_Semester_Credit")]
public partial class FacultySemesterCredit
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Faculty_Id")]
    public int FacultyId { get; set; }

    [Column("Semester_Id")]
    public int SemesterId { get; set; }

    [Column("Required_Credits")]
    public int RequiredCredits { get; set; } = 24;

    [Column("Min_Credits")]
    public int? MinCredits { get; set; }

    [Column("Max_Credits")]
    public int? MaxCredits { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; } = false;

    [ForeignKey("FacultyId")]
    public virtual Faculty Faculty { get; set; } = null!;

    [ForeignKey("SemesterId")]
    public virtual Semester Semester { get; set; } = null!;
}
