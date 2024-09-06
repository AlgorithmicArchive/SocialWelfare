using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class RecordCount
{
    public int RecordId { get; set; }

    public int ServiceId { get; set; }

    public string Officer { get; set; } = null!;

    public int AccessCode { get; set; }

    public int Pending { get; set; }

    public int PendingWithCitizen { get; set; }

    public int Forward { get; set; }

    public int Sanction { get; set; }

    public int Return { get; set; }

    public int Reject { get; set; }
}
