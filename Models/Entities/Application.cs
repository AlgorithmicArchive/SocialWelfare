using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Application
{
    public string ApplicationId { get; set; } = null!;

    public int CitizenId { get; set; }

    public int ServiceId { get; set; }

    public string ApplicantName { get; set; } = null!;

    public string ApplicantImage { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string Relation { get; set; } = null!;

    public string RelationName { get; set; } = null!;

    public string DateOfBirth { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string ServiceSpecific { get; set; } = null!;

    public string PresentAddressId { get; set; } = null!;

    public string PermanentAddressId { get; set; } = null!;

    public string BankDetails { get; set; } = null!;

    public string Documents { get; set; } = null!;

    public string EditList { get; set; } = null!;

    public int Phase { get; set; }

    public string ApplicationStatus { get; set; } = null!;

    public string SubmissionDate { get; set; } = null!;

    public virtual ICollection<ApplicationsHistory> ApplicationsHistories { get; set; } = new List<ApplicationsHistory>();

    public virtual User Citizen { get; set; } = null!;

    public virtual ICollection<CurrentPhase> CurrentPhases { get; set; } = new List<CurrentPhase>();

    public virtual Service Service { get; set; } = null!;
}
