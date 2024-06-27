using System;
using System.Collections.Generic;

namespace SocialWelfare.Models.Entities;

public partial class Certificate
{
    public int Uuid { get; set; }

    public int? OfficerId { get; set; }

    public string CertificateName { get; set; } = null!;

    public byte[] EncryptedCertificateData { get; set; } = null!;

    public byte[] EncryptedPassword { get; set; } = null!;

    public DateTime RegisteredDate { get; set; }
}
