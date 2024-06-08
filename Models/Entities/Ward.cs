using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Ward
{
    public int Uuid { get; set; }

    public int? VillageId { get; set; }

    public string? WardName { get; set; }
}
