using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Rules_Regulations")]
public partial class RulesRegulation
{
    [Key]
    [Column("Rule_Id")]
    public int RuleId { get; set; }

    [StringLength(150)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [StringLength(255)]
    public string? Penalty { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }
}
