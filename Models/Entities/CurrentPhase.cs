using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class CurrentPhase
{
    public int PhaseId { get; set; }

    public string ApplicationId { get; set; } = null!;

    public string ReceivedOn { get; set; } = null!;

    public string Officer { get; set; } = null!;

    public int AccessCode { get; set; }

    public string ActionTaken { get; set; } = null!;

    public string Remarks { get; set; } = null!;

    public string? File { get; set; }

    public bool CanPull { get; set; }

    public int Previous { get; set; }

    public int Next { get; set; }

    public virtual Application Application { get; set; } = null!;
}
