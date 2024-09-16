using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class District
{
    public int DistrictId { get; set; }

    public string DistrictName { get; set; } = null!;

    public string DistrictShort { get; set; } = null!;

    public int Division { get; set; }

    public virtual ICollection<ApplicationPerDistrict> ApplicationPerDistricts { get; set; } = new List<ApplicationPerDistrict>();

    public virtual ICollection<BankFile> BankFiles { get; set; } = new List<BankFile>();

    public virtual ICollection<Block> Blocks { get; set; } = new List<Block>();

    public virtual ICollection<Tehsil> Tehsils { get; set; } = new List<Tehsil>();
}
