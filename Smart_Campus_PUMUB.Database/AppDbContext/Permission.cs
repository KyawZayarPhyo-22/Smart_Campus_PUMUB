using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Permissions")]
public partial class Permission
{
    [Key]
    [Column("Permission_Id")]
    public int PermissionId { get; set; }

    [Column("Permission_Name")]
    [StringLength(100)]
    public string PermissionName { get; set; } = null!;

    [InverseProperty("Permission")]
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}