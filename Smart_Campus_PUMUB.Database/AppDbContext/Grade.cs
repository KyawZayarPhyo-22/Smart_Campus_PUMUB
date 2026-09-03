using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Grade")]
public partial class Grade
{
    [Key]
    [Column("Grade_Id")]
    public int GradeId { get; set; }

    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Column("Grade_Point", TypeName = "decimal(4, 2)")]
    public decimal GradePoint { get; set; } = 0.0m;

    [Column("Status")]
    [StringLength(50)]
    public string? Status { get; set; }

    [Column("Min_Mark", TypeName = "decimal(5, 2)")]
    public decimal? MinMark { get; set; }

    [Column("Max_Mark", TypeName = "decimal(5, 2)")]
    public decimal? MaxMark { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

}
