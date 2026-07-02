using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("RegisterAcc")]
public partial class RegisterAccount
{
    [Key]
    [Column("RegisterAccId")]
    public int RegisterAccId { get; set; }

    [Column("Full_Name")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>တက္ကသိုလ်ဝင်ရတဲ့ Form နံပါတ်</summary>
    [Column("Form_No")]
    [StringLength(50)]
    public string? FormNo { get; set; }

    /// <summary>တက္ကသိုလ်ဝင်ခုံအမှတ်</summary>
    [Column("Exam_Seat_No")]
    [StringLength(50)]
    public string? ExamSeatNo { get; set; }

    /// <summary>Pending / Approved / Rejected</summary>
    [StringLength(20)]
    public string? Status { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ReviewedDateTime { get; set; }

    [StringLength(100)]
    public string? ReviewedBy { get; set; }
}
