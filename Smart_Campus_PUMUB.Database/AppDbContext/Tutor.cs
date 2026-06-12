using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Tutor")]
public partial class Tutor
{
    [Key]
    [Column("Tutor_Id")]
    public int TutorId { get; set; }

    [Column("User_Id")]
    public int UserId { get; set; }

    [Column("Position_id")]
    public int PositionId { get; set; }

    [Column("Department_Id")]
    public int DepartmentId { get; set; }

    [Column("Tutor_Name")]
    [StringLength(100)]
    public string TutorName { get; set; } = null!;

    [StringLength(100)]
    [Unicode(false)]
    public string? Email { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string? Profile { get; set; }

    public string? About { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [ForeignKey("DepartmentId")]
    [InverseProperty("Tutors")]
    public virtual Department Department { get; set; } = null!;

    [ForeignKey("PositionId")]
    [InverseProperty("Tutors")]
    public virtual Position Position { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Tutors")]
    public virtual User User { get; set; } = null!;
}
