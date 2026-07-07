using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

/// <summary>
/// Semester I ကျောင်းသားအသစ်များအတွက် temporary account table
/// RegisterAcc approve ပြီးနောက် User table မဟုတ်ဘဲ ဤ table ထဲတွင်သာ သိမ်းသည်
/// </summary>
[Table("NewStudentAcc")]
public partial class NewStudentAcc
{
    [Key]
    [Column("NewStudentAccId")]
    public int NewStudentAccId { get; set; }

    /// <summary>RegisterAcc table ရဲ့ FK (optional, admin manual create ဆိုရင် null ဖြစ်နိုင်)</summary>
    [Column("RegisterAccId")]
    public int? RegisterAccId { get; set; }

    [Column("Full_Name")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(150)]
    public string Email { get; set; } = null!;

    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>Login username (unique)</summary>
    [StringLength(50)]
    public string Username { get; set; } = null!;

    /// <summary>BCrypt hashed password</summary>
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Account status: 'Active' or 'Inactive'
    /// Admin က Inactive လုပ်လိုက်ရင် login ဝင်ခွင့် ချက်ချင်းပိတ်မည်
    /// </summary>
    [StringLength(20)]
    public string AccountStatus { get; set; } = "Active";

    /// <summary>First login တွင် password ပြောင်းရမည်</summary>
    public bool MustChangePassword { get; set; } = true;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }
}
