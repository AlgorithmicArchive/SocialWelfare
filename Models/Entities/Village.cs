using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Village
{
    public int Uuid { get; set; }

    public int? HalqaPanchayatId { get; set; }

    public int? TehsilId { get; set; }

    public string? VillageName { get; set; }
}
