using Microsoft.AspNetCore.Mvc;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using iText.Bouncycastle.Crypto;
using iText.Commons.Bouncycastle.Cert;
using iText.Bouncycastle.X509;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using iText.Forms.Form.Element;
using iText.Forms.Fields.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using System.Text;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> UploadDSC([FromForm] IFormCollection form)

        {
            var file = form.Files["dscFile"];
            string password = form["password"].ToString();
            if (file == null || file.Length == 0 || Path.GetExtension(file.FileName).ToLower() != ".pfx")
            {
                return Json(new { status = false, message = "Invalid file type or no file uploaded." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var certificateBytes = await ReadStreamToByteArray(stream);

                using var pfx = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.Exportable);
                // if (pfx.Issuer == pfx.Subject)
                // {
                //     return Json(new { status = false, message = "Self-signed certificates are not allowed." });
                // }

                // Check if the certificate is expired
                if (DateTime.UtcNow > pfx.NotAfter)
                {
                    return Json(new { status = false, message = "The certificate has expired." });
                }

                // Check if the certificate is not yet valid
                if (DateTime.UtcNow < pfx.NotBefore)
                {
                    return Json(new { status = false, message = "The certificate is not yet valid." });
                }

                byte[] encryptionKey = encryptionService.GenerateKey();
                byte[] encryptionIV = encryptionService.GenerateIV();
                byte[] encryptedCertificate = encryptionService.EncryptData(certificateBytes, encryptionKey, encryptionIV);
                byte[] encryptedPassword = encryptionService.EncryptData(Encoding.UTF8.GetBytes(password), encryptionKey, encryptionIV);


                SaveDSCToDatabase(encryptedCertificate, encryptedPassword, encryptionKey, encryptionIV);

                return Json(new { status = true, message = "Certificate Registered Properly." });
            }
            catch (CryptographicException ex)
            {
                return BadRequest($"Cryptographic error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing .pfx file: {ex.Message}");
            }
        }
        private static async Task<byte[]> ReadStreamToByteArray(Stream stream)
        {
            using MemoryStream ms = new();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        public void SaveDSCToDatabase(byte[] encryptedCertificate, byte[] encryptedPassword, byte[] encryptionKey, byte[] encryptionIV)
        {
            int userId = (int)HttpContext.Session.GetInt32("UserId")!;

            byte[] kek = Convert.FromBase64String(Environment.GetEnvironmentVariable("KEY_ENCRYPTION_KEY")!);
            byte[] encryptedKey = encryptionService.EncryptData(encryptionKey, kek, encryptionIV);


            var certificateDetails = new Models.Entities.Certificate
            {
                OfficerId = userId,
                EncryptedCertificateData = encryptedCertificate,
                EncryptedPassword = encryptedPassword,
                EncryptionKey = encryptedKey,
                EncryptionIv = encryptionIV
            };

            dbcontext.Certificates.Add(certificateDetails);
            dbcontext.SaveChanges();

        }
        public void Sign(string src, string dest, Org.BouncyCastle.X509.X509Certificate[] chain, ICipherParameters pk,
                    string digestAlgorithm, PdfSigner.CryptoStandard subfilter, string reason, string location,
                    ICollection<ICrlClient>? crlList, IOcspClient? ocspClient, ITSAClient? tsaClient, int estimatedSize)
        {
            using PdfReader reader = new(src);
            using FileStream fs = new(dest, FileMode.Open);

            PdfSigner signer = new(reader, fs, new StampingProperties());

            // Set the name of the signature field
            signer.SetFieldName("SignatureFieldName");

            // Set the position and page number for the signature
            signer.SetPageRect(new iText.Kernel.Geom.Rectangle(380, 20, 200, 100));  // Rectangle(x, y, width, height)
            signer.SetPageNumber(1);



            // Set reason and location for the signature
            signer.SetReason("Digital Signing");
            signer.SetLocation("Social Welfare Department");

            SignedAppearanceText appearanceText = new();
            appearanceText.SetReasonLine("Reason: " + signer.GetReason());
            appearanceText.SetLocationLine("Department: " + signer.GetLocation());

            // Initialize the appearance object
            SignatureFieldAppearance appearance = new("app");

            // Set up the appearance content
            appearance.SetContent(appearanceText);

            // Set font, color, and size for the appearance text
            appearance.SetFontSize(10);
            appearance.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
            appearance.SetFontColor(new DeviceRgb(0, 0, 0)); // Black color



            // Apply the appearance to the signer
            signer.SetSignatureAppearance(appearance);





            IExternalSignature pks = new PrivateKeySignature(new PrivateKeyBC(pk), digestAlgorithm);
            IX509Certificate[] certificateWrappers = new IX509Certificate[chain.Length];
            for (int i = 0; i < certificateWrappers.Length; ++i)
            {
                certificateWrappers[i] = new X509CertificateBC(chain[i]);
            }

            // Sign the document
            signer.SignDetached(pks, certificateWrappers, crlList, ocspClient, tsaClient, estimatedSize, subfilter);
        }
        public IActionResult SignPdf(string ApplicationId)
        {

            int userId = (int)HttpContext.Session.GetInt32("UserId")!;
            string inputPdfPath = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "SanctionLetter.pdf");

            var certificate = dbcontext.Certificates.FirstOrDefault(cer => cer.OfficerId == userId);

            byte[] kek = Convert.FromBase64String(Environment.GetEnvironmentVariable("KEY_ENCRYPTION_KEY")!);
            byte[] encryptionKey = encryptionService.DecryptData(certificate!.EncryptionKey, kek, certificate!.EncryptionIv);
            byte[] encryptionIV = certificate.EncryptionIv;

            byte[] certificateBytes = encryptionService.DecryptData(certificate.EncryptedCertificateData, encryptionKey, encryptionIV);
            byte[] certificatePasswordBytes = encryptionService.DecryptData(certificate.EncryptedPassword, encryptionKey, encryptionIV);
            string decryptedPassword = Encoding.UTF8.GetString(certificatePasswordBytes);


            using (var pfxStream = new MemoryStream(certificateBytes))
            {
                Pkcs12Store pkcs12 = new Pkcs12StoreBuilder().Build();
                pkcs12.Load(pfxStream, decryptedPassword.ToCharArray());
                string? alias = null;
                foreach (var a in pkcs12.Aliases)
                {
                    alias = (string)a;
                    if (pkcs12.IsKeyEntry(alias))
                        break;
                }

                ICipherParameters pk = pkcs12.GetKey(alias).Key;
                X509CertificateEntry[] ce = pkcs12.GetCertificateChain(alias);
                Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
                for (int k = 0; k < ce.Length; ++k)
                {
                    chain[k] = ce[k].Certificate;
                }

                Sign(inputPdfPath, inputPdfPath, chain, pk, DigestAlgorithms.SHA256, PdfSigner.CryptoStandard.CMS, "Digital Signing", "JAMMU", null, null, null, 0);

            }

            return Json(new { status = true });
        }


    }
}