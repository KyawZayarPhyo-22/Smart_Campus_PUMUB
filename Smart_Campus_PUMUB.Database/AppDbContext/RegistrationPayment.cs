using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Registration_Payment")]
public partial class RegistrationPayment
{
    [Key]
    [Column("Payment_Id")]
    public int PaymentId { get; set; }

    [Column("Registration_Id")]
    public int RegistrationId { get; set; }

    [Column("Amount_Paid", TypeName = "decimal(10, 2)")]
    public decimal AmountPaid { get; set; }

    [Column("Payment_Method")]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = null!;

    [Column("Receipt_Image")]
    [StringLength(255)]
    [Unicode(false)]
    public string ReceiptImage { get; set; } = null!;

    [Column("Payment_Date", TypeName = "datetime")]
    public DateTime PaymentDate { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }

    public int? VerifyBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [ForeignKey("RegistrationId")]
    [InverseProperty("RegistrationPayments")]
    public virtual StudentRegistration Registration { get; set; } = null!;

    [ForeignKey("VerifyBy")]
    [InverseProperty("RegistrationPayments")]
    public virtual User? VerifyByNavigation { get; set; }
}
