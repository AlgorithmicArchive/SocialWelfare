using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class CurrentPhase
{
    public int PhaseId { get; set; }

    public string ApplicationId { get; set; } = null!;

    public string ReceivedOn { get; set; } = null!;

    public int OfficerId { get; set; }

    public string Officer { get; set; } = null!;

    public int AccessCode { get; set; }

    public string ActionTaken { get; set; } = null!;

    public string Remarks { get; set; } = null!;

    public string File { get; set; } = null!;

    public bool CanPull { get; set; }

    public int Previous { get; set; }

    public int Next { get; set; }
}
