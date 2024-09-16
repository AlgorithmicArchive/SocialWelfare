using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Service
{
    public int ServiceId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string Department { get; set; } = null!;

    public string? FormElement { get; set; }

    public string? WorkForceOfficers { get; set; }

    public string? UpdateColumn { get; set; }

    public string? LetterUpdateDetails { get; set; }

    public decimal CreatedAt { get; set; }

    public bool Active { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();

    public virtual ICollection<BankFile> BankFiles { get; set; } = new List<BankFile>();

    public virtual ICollection<RecordCount> RecordCounts { get; set; } = new List<RecordCount>();
}
