using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Officer
{
    public int OfficerId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string Designation { get; set; } = null!;

    public int DivisionCode { get; set; }

    public int DistrictCode { get; set; }

    public int TehsilCode { get; set; }

    public string? BackupCodes { get; set; }

    public bool EmailValidated { get; set; }

    public DateTime RegisteredDate { get; set; }
}
