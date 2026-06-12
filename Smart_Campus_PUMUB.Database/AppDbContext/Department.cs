using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Department")]
public partial class Department
{
    [Key]
    [Column("Department_Id")]
    public int DepartmentId { get; set; }

    [Column("Faculty_Id")]
    public int FacultyId { get; set; }

    [Column("Department_Name")]
    [StringLength(150)]
    public string DepartmentName { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [ForeignKey("FacultyId")]
    [InverseProperty("Departments")]
    public virtual Faculty Faculty { get; set; } = null!;

    [InverseProperty("Department")]
    public virtual ICollection<Tutor> Tutors { get; set; } = new List<Tutor>();
}
