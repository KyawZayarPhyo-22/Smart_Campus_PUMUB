using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("System_Setting")]
[Index(nameof(SettingKey), IsUnique = true, Name = "UQ_System_Setting_Key")]
public class SystemSetting
{
    [Key]
    [Column("Setting_Id")]
    public int SettingId { get; set; }

    [Required]
    [Column("Setting_Key")]
    [StringLength(100)]
    public string SettingKey { get; set; } = null!;

    [Required]
    [Column("Setting_Value")]
    [StringLength(500)]
    public string SettingValue { get; set; } = null!;

    [Column("Description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("Updated_Date_Time", TypeName = "datetime")]
    public DateTime? UpdatedDateTime { get; set; }

    [Column("Updated_By")]
    [StringLength(100)]
    public string? UpdatedBy { get; set; }
}
