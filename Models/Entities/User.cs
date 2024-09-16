using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public byte[] Password { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string UserSpecificDetails { get; set; } = null!;

    public string UserType { get; set; } = null!;

    public string BackupCodes { get; set; } = null!;

    public bool EmailValid { get; set; }

    public string RegisteredDate { get; set; } = null!;

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
}
