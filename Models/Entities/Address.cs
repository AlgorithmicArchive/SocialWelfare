using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Address
{
    public int AddressId { get; set; }

    public int DistrictId { get; set; }

    public int TehsilId { get; set; }

    public int BlockId { get; set; }

    public int HalqaPanchayatId { get; set; }

    public int VillageId { get; set; }

    public int WardId { get; set; }

    public int PincodeId { get; set; }

    public string AddressDetails { get; set; } = null!;
}
