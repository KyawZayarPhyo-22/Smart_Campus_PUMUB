using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Subject_Prerequisite")]
public class SubjectPrerequisite
{
    [Key]
    public int Id { get; set; }

    [Column("Subject_Id")]
    public int SubjectId { get; set; }

    [Column("Prerequisite_Subject_Id")]
    public int PrerequisiteSubjectId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [ForeignKey("SubjectId")]
    [InverseProperty("Prerequisites")]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey("PrerequisiteSubjectId")]
    [InverseProperty("PrerequisiteFor")]
    public virtual Subject PrerequisiteSubject { get; set; } = null!;
}
