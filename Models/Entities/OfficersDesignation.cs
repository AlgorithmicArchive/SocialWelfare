using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class OfficersDesignation
{
    public int Uuid { get; set; }

    public string Designation { get; set; } = null!;

    public string DesignationShort { get; set; } = null!;

    public string AccessLevel { get; set; } = null!;
}
