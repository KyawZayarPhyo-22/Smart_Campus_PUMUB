using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("RoleHierarchy")]
public partial class RoleHierarchy
{
    [Key]
    public int Id { get; set; }

    [Column("Parent_Role_Id")]
    public int ParentRoleId { get; set; }

    [Column("Child_Role_Id")]
    public int ChildRoleId { get; set; }

    public bool CanAccessAllFaculties { get; set; } = false;

    [ForeignKey("ParentRoleId")]
    public virtual Role ParentRole { get; set; } = null!;

    [ForeignKey("ChildRoleId")]
    public virtual Role ChildRole { get; set; } = null!;
}
