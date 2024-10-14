using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class UniqueIdtable
{
    public int Uuid { get; set; }

    public string DistrictNameShort { get; set; } = null!;

    public string MonthShort { get; set; } = null!;

    public int LastSequentialNumber { get; set; }
}
