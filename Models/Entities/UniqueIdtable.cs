using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class UniqueIdtable
{
    public string DistrictNameShort { get; set; } = null!;

    public string MonthShort { get; set; } = null!;

    public long LastSequentialNumber { get; set; }
}
