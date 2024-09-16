using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Block
{
    public int DistrictId { get; set; }

    public int BlockId { get; set; }

    public string BlockName { get; set; } = null!;

    public virtual District District { get; set; } = null!;

    public virtual ICollection<HalqaPanchayat> HalqaPanchayats { get; set; } = new List<HalqaPanchayat>();
}
