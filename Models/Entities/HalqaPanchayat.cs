using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class HalqaPanchayat
{
    public int Uuid { get; set; }

    public int BlockId { get; set; }

    public string PanchayatName { get; set; } = null!;

    public virtual Block Block { get; set; } = null!;

    public virtual ICollection<Village> Villages { get; set; } = new List<Village>();
}
