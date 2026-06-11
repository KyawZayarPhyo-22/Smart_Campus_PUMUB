using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Activity")]
public partial class Activity
{
    [Key]
    [Column("Activity_Id")]
    public int ActivityId { get; set; }

    [Column("Activity_Title")]
    [StringLength(150)]
    public string ActivityTitle { get; set; } = null!;

    [StringLength(255)]
    [Unicode(false)]
    public string? Image { get; set; }

    public string? Description { get; set; }

    [StringLength(255)]
    public string? Location { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; } 
}
