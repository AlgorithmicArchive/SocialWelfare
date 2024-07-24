using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class UpdatedLetterDetail
{
    public string ApplicationId { get; set; } = null!;

    public string UpdatedDetails { get; set; } = null!;
}
