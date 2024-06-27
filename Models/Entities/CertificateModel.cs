public class CertificateModel
{
    public int Id { get; set; }
    public string? CertificateName { get; set; }
    public byte[]? EncryptedCertificateData { get; set; }
    public byte[]? EncryptedPassword { get; set; }
}
