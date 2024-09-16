using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class ApplicationPerDistrict
{
    public int Uuid { get; set; }

    public int DistrictId { get; set; }

    public string FinancialYear { get; set; } = null!;

    public int CountValue { get; set; }

    public virtual District District { get; set; } = null!;
}
