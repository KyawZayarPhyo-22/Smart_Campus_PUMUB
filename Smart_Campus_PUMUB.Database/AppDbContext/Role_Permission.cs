using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Role_Permissions")]
public partial class RolePermission
{
    [Key]
    [Column("Role_Permission_Id")]
    public int RolePermissionId { get; set; }

    [Column("Role_Id")]
    public int RoleId { get; set; }

    [Column("Permission_Id")]
    public int PermissionId { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("RolePermissions")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    [InverseProperty("RolePermissions")]
    public virtual Permission Permission { get; set; } = null!;
}