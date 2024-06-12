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

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
