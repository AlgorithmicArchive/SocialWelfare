using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class ApplicationList
{
    public int Uuid { get; set; }

    public int ServiceId { get; set; }

    public string Officer { get; set; } = null!;

    public string AccessLevel { get; set; } = null!;

    public int AccessCode { get; set; }

    public string ApprovalList { get; set; } = null!;

    public string PoolList { get; set; } = null!;
}
