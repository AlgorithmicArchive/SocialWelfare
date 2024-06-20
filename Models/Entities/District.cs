using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class District
{
    public int Uuid { get; set; }

    public int DistrictId { get; set; }

    public string DistrictName { get; set; } = null!;

    public string DistrictShort { get; set; } = null!;

    public int Division { get; set; }
}
