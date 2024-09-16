using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Village
{
    public int Uuid { get; set; }

    public int HalqaPanchayatId { get; set; }

    public int TehsilId { get; set; }

    public string VillageName { get; set; } = null!;

    public virtual HalqaPanchayat HalqaPanchayat { get; set; } = null!;

    public virtual Tehsil Tehsil { get; set; } = null!;

    public virtual ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
