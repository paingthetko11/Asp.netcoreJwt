using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AspnetcoreJwtTest.Entities;

[Table("Employee")]
public partial class Employee
{
    [Key]
    [StringLength(50)]
    public string EmployeeId { get; set; } = null!;

    [StringLength(50)]
    public string? FullName { get; set; }

    [StringLength(50)]
    public string? Gender { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DateOfBirth { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? JoinDate { get; set; }
}
