using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Tehsil
{
    public int Uuid { get; set; }

    public int? DistrictId { get; set; }

    public int? TehsilId { get; set; }

    public string? TehsilName { get; set; }
}
