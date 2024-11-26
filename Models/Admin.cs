using System;
using System.Collections.Generic;

namespace EWDProject.Models;

public partial class Admin
{
    public int AdminId { get; set; }

    public string Password { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;
}
