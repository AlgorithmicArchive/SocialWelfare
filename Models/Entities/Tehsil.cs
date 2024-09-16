using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Tehsil
{
    public int DistrictId { get; set; }

    public int TehsilId { get; set; }

    public string TehsilName { get; set; } = null!;

    public virtual District District { get; set; } = null!;

    public virtual ICollection<Village> Villages { get; set; } = new List<Village>();
}
