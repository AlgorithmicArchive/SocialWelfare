using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Citizen
{
    public int CitizenId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string? BackupCodes { get; set; }

    public bool EmailValidated { get; set; }

    public DateTime RegisteredDate { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
