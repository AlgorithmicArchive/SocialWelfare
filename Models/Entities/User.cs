using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string UserSpecificDetails { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public string BackupCodes { get; set; } = null!;

    public bool EmailValid { get; set; }

    public DateTime RegisteredDate { get; set; }
}
