using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Payment_Fees")]
public partial class PaymentFee
{
    [Key]
    [Column("Fees_Id")]
    public int FeesId { get; set; }

    [Column("Class_Year")]
    [StringLength(50)]
    public string ClassYear { get; set; } = null!;

    [Column("Montly_Amount", TypeName = "decimal(10, 2)")]
    public decimal MontlyAmount { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }
}
