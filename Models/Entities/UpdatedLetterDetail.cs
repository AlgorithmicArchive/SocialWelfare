using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class UpdatedLetterDetail
{
    public string ApplicationId { get; set; } = null!;

    public string UpdatedDetails { get; set; } = null!;

    public virtual Application Application { get; set; } = null!;
}
