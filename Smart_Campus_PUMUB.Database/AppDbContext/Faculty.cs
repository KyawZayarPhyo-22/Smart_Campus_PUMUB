using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Smart_Campus_PUMUB.Database.AppDbContext;

[Table("Faculty")]
public partial class Faculty
{
    [Key]
    [Column("Faculty_Id")]
    public int FacultyId { get; set; }

    [Column("Faculty_Name")]
    [StringLength(150)]
    public string FacultyName { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedDateTime { get; set; }

    public string? CreatedBy { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ModifiedDateTime { get; set; }

    public string? ModifiedBy { get; set; }

    public bool? IsDelete { get; set; }

    [InverseProperty("Faculty")]
    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

    [InverseProperty("Faculty")]
    public virtual ICollection<Major> Majors { get; set; } = new List<Major>();

    [InverseProperty("Faculty")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();

    [InverseProperty("Faculty")]
    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
