using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AspnetcoreJwtTest.Entities;

[Table("User")]
public partial class User
{
    [Key]
    [StringLength(250)]
    public string UserId { get; set; } = null!;

    [StringLength(250)]
    public string? UserName { get; set; }

    [StringLength(250)]
    public string? Email { get; set; }

    [StringLength(250)]
    public string? Password { get; set; }
    public string? Role { get; set; }
    //public string Role { get; internal set; }
}
