using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("User")]
[Index("UserName", Name = "UQ__User__C9F28456D3FE7075", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("User_Id")]
    public int UserId { get; set; }

    [Column("Role_id")]
    public int RoleId { get; set; }

    [Column("Full_Name")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(50)]
    [Unicode(false)]
    public string UserName { get; set; } = null!;

    [Column("Role_No")]
    [StringLength(50)]
    public string? RoleNo { get; set; }

    [StringLength(255)]
    [Unicode(false)]
    public string Password { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [NotMapped]
    public int? PasswordLength { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [InverseProperty("VerifyByNavigation")]
    public virtual ICollection<RegistrationPayment> RegistrationPayments { get; set; } = new List<RegistrationPayment>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role Role { get; set; } = null!;

    [InverseProperty("User")]
    public virtual ICollection<StudentRegistration> StudentRegistrations { get; set; } = new List<StudentRegistration>();

    [InverseProperty("User")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    [InverseProperty("User")]
    public virtual ICollection<Tutor> Tutors { get; set; } = new List<Tutor>();
}
