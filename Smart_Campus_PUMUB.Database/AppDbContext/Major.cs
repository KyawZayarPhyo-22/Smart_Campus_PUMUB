using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Major")]
public partial class Major
{
    [Key]
    [Column("Major_Id")]
    public int MajorId { get; set; }

    [Column("Major_Name")]
    [StringLength(200)]
    public string MajorName { get; set; } = null!;

    [Column("Faculty_Id")]
    public int FacultyId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [ForeignKey("FacultyId")]
    [InverseProperty("Majors")]
    public virtual Faculty Faculty { get; set; } = null!;
}
