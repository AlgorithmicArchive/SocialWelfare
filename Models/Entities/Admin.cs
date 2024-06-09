using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Admin
{
    public int Uuid { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;
}
