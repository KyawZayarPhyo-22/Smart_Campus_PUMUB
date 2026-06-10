using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Position")]
public partial class Position
{
    [Key]
    [Column("Position_Id")]
    public int PositionId { get; set; }

    [Column("Position_Name")]
    [StringLength(100)]
    public string PositionName { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [InverseProperty("Position")]
    public virtual ICollection<Tutor> Tutors { get; set; } = new List<Tutor>();
}
