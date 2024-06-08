using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class District
{
    public int Uuid { get; set; }

    public int? DistrictId { get; set; }

    public string? DistrictName { get; set; }

    public string? DistrictShort { get; set; }
}
