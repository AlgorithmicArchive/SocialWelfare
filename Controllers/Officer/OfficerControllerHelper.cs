using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocialWelfare.Models.Entities;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Cert;
using Org.BouncyCastle.Security;
using iText.IO.Source;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        public dynamic PendingApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);

            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string districtCode = UserSpecificDetails?["DistrictCode"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;


            SqlParameter AccessLevelCode = new SqlParameter("@AccessLevelCode", DBNull.Value);

            switch (accessLevel)
            {
                case "Tehsil":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["TehsilCode"]?.ToString() ?? string.Empty);
                    break;
                case "District":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DistrictCode"]?.ToString() ?? string.Empty);
                    break;
                case "Division":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DivisionCode"]?.ToString() ?? string.Empty);
                    break;
            }

            var applicationList = dbcontext.Applications.FromSqlRaw(
                "EXEC GetApplicationsForOfficer @OfficerDesignation, @ActionTaken, @AccessLevel, @AccessLevelCode, @ServiceId",
                new SqlParameter("@OfficerDesignation", officerDesignation),
                new SqlParameter("@ActionTaken", "Pending"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).ToList();

            bool canSanction = false;
            bool canUpdate = false;
            List<Application> UpdateList = new List<Application>();
            List<Application> PoolList = new List<Application>();
            List<Application> PendingList = new List<Application>();
            JArray pool = new JArray();

            foreach (var application in applicationList)
            {
                _logger.LogInformation($"Application: {application.ApplicationId}");

                var updateRequest = JsonConvert.DeserializeObject<dynamic>(application.UpdateRequest!);
                if (updateRequest == null)
                {
                    _logger.LogError($"UpdateRequest is null for application {application.ApplicationId}");
                    continue;
                }

                var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == application.ServiceId);
                if (service == null)
                {
                    _logger.LogError($"Service not found for ServiceId {application.ServiceId}");
                    continue;
                }

                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service.WorkForceOfficers!);
                if (workForceOfficers == null)
                {
                    _logger.LogError($"WorkForceOfficers is null for service {service.ServiceId}");
                    continue;
                }

                var officer = workForceOfficers.FirstOrDefault(o => o["Designation"] == officerDesignation);
                if (officer == null)
                {
                    _logger.LogError($"No matching officer found in WorkForceOfficers for designation {officerDesignation}");
                    continue;
                }

                canSanction = officer["canSanction"];
                canUpdate = officer["canUpdate"];
                int requested = updateRequest["requested"];
                int updated = updateRequest["updated"];

                if (officerDesignation == "Director Finance" && canSanction)
                {
                    pool = JArray.Parse(officer["pool"].ToString());
                }
                else if (canSanction)
                {
                    var poolElement = officer["pool"]?[districtCode];
                    pool = poolElement != null ? JArray.Parse(poolElement.ToString()) : new JArray();
                    if (poolElement == null)
                    {
                        officer["pool"][districtCode] = pool;
                    }
                }

                if (requested == 1 && updated == 0)
                {
                    UpdateList.Add(application);
                }
                else if (pool.Count == 0)
                {
                    PendingList.Add(application);
                }
                else
                {
                    bool inPool = pool.Any(item => item.ToString() == application.ApplicationId);
                    if (inPool)
                    {
                        PoolList.Add(application);
                    }
                    else
                    {
                        PendingList.Add(application);
                    }
                }
            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.PendingList = PendingList;
            obj.UpdateList = UpdateList;
            obj.PoolList = PoolList;
            obj.canSanction = canSanction;
            obj.canUpdate = canUpdate;
            obj.Type = "Pending";
            obj.ServiceId = 1;

            return obj;
        }
        public dynamic SentApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;

            SqlParameter AccessLevelCode = new SqlParameter("@AccessLevelCode", DBNull.Value);

            switch (accessLevel)
            {
                case "Tehsil":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["TehsilCode"]?.ToString() ?? string.Empty);
                    break;
                case "District":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DistrictCode"]?.ToString() ?? string.Empty);
                    break;
                case "Division":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DivisionCode"]?.ToString() ?? string.Empty);
                    break;
            }

            var applicationList = dbcontext.Applications.FromSqlRaw(
                "EXEC GetApplicationsForOfficer @OfficerDesignation, @ActionTaken, @AccessLevel, @AccessLevelCode, @ServiceId",
                new SqlParameter("@OfficerDesignation", officerDesignation),
                new SqlParameter("@ActionTaken", "Forward,Return"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).ToList();

            var SentApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                bool? canPull = false;
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);
                foreach (var phase in phases!)
                {
                    if (phase["Officer"] == officerDesignation)
                        canPull = phase["CanPull"];
                }
                var data = new
                {
                    application.ApplicationId,
                    application.ApplicantName,
                    canPull
                };
                SentApplications.Add(data);
            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.SentApplications = SentApplications;
            obj.Type = "Sent";
            return obj;
        }
        public dynamic SanctionApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;

            SqlParameter AccessLevelCode = new SqlParameter("@AccessLevelCode", DBNull.Value);

            switch (accessLevel)
            {
                case "Tehsil":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["TehsilCode"]?.ToString() ?? string.Empty);
                    break;
                case "District":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DistrictCode"]?.ToString() ?? string.Empty);
                    break;
                case "Division":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DivisionCode"]?.ToString() ?? string.Empty);
                    break;
            }

            var applicationList = dbcontext.Applications.FromSqlRaw(
                "EXEC GetApplicationsForOfficer @OfficerDesignation, @ActionTaken, @AccessLevel, @AccessLevelCode, @ServiceId",
                new SqlParameter("@OfficerDesignation", officerDesignation),
                new SqlParameter("@ActionTaken", "Sanction"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).ToList();

            var SantionApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                var data = new
                {
                    application.ApplicationId,
                    application.ApplicantName,
                };
                SantionApplications.Add(data);
            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.MiscellaneousList = SantionApplications;
            obj.Type = "Sanction";
            return obj;
        }
        public dynamic RejectApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;

            SqlParameter AccessLevelCode = new SqlParameter("@AccessLevelCode", DBNull.Value);

            switch (accessLevel)
            {
                case "Tehsil":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["TehsilCode"]?.ToString() ?? string.Empty);
                    break;
                case "District":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DistrictCode"]?.ToString() ?? string.Empty);
                    break;
                case "Division":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DivisionCode"]?.ToString() ?? string.Empty);
                    break;
            }

            var applicationList = dbcontext.Applications.FromSqlRaw(
                "EXEC GetApplicationsForOfficer @OfficerDesignation, @ActionTaken, @AccessLevel, @AccessLevelCode, @ServiceId",
                new SqlParameter("@OfficerDesignation", officerDesignation),
                new SqlParameter("@ActionTaken", "Reject"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).ToList();

            var RejectApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                var data = new
                {
                    application.ApplicationId,
                    application.ApplicantName,
                };
                RejectApplications.Add(data);
            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.MiscellaneousList = RejectApplications;
            obj.Type = "Reject";
            return obj;
        }
        public bool IsMoreThanSpecifiedDays(string dateString, int value)
        {
            if (DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                DateTime currentDate = DateTime.Now;
                double daysDifference = (currentDate - parsedDate).TotalDays;
                return daysDifference > value;
            }
            else
            {
                throw new ArgumentException("Invalid date format.");
            }
        }
        public List<dynamic> GetCount(string type, Dictionary<string, string> conditions)
        {
            StringBuilder Condition1 = new StringBuilder();
            StringBuilder Condition2 = new StringBuilder();
            if (type == "Pending")
                Condition1.Append("AND a.ApplicationStatus='Initiated'");
            else if (type == "Sanction")
                Condition1.Append("AND a.ApplicationStatus='Sanctioned'");
            else if (type == "Reject")
                Condition1.Append("AND a.ApplicationStatus='Rejected'");
            else if (type == "PendingWithCitizen")
                Condition1.Append("AND a.ApplicationStatus='Initiated' AND JSON_VALUE(app.value, '$.ActionTaken')='ReturnToEdit'");

            int conditionCount = 0;
            int splitPoint = conditions != null ? conditions.Count / 2 : 0;

            if (conditions != null && conditions.Count != 0)
            {
                foreach (var condition in conditions)
                {
                    if (conditionCount < splitPoint)
                        Condition1.Append($" AND {condition.Key}='{condition.Value}'");
                    else
                        Condition2.Append($" AND {condition.Key}='{condition.Value}'");

                    conditionCount++;
                }

            }

            if (conditions != null && conditions.ContainsKey("JSON_VALUE(app.value, '$.Officer')") && type != "Total")
            {
                Condition2.Append($" AND JSON_VALUE(app.value, '$.ActionTaken') = '{type}'");
            }
            else if (type == "Total")
            {
                Condition2.Append($" AND JSON_VALUE(app.value, '$.ActionTaken') != ''");
            }

            _logger.LogInformation($"Condition1: {Condition1} Condition2: {Condition2}");
            var applications = dbcontext.Applications.FromSqlRaw("EXEC GetApplications @Condition1, @Condition2",
            new SqlParameter("@Condition1", Condition1.ToString()),
           new SqlParameter("@Condition2", Condition2.ToString())).ToList();

            var list = new List<dynamic>();

            foreach (var application in applications)
            {
                int districtCode = Convert.ToInt32(JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific)!["District"]);
                string AppliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)!.DistrictName;
                string AppliedService = dbcontext.Services.FirstOrDefault(s => s.ServiceId == application.ServiceId)!.ServiceName;
                string ApplicationWithOfficer = "";
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);
                foreach (var phase in phases!)
                {
                    if (phase["ActionTaken"] == "Pending" || phase["ActionTaken"] == "Sanction")
                    {
                        ApplicationWithOfficer = phase["Officer"];
                        break;
                    }
                }

                var obj = new
                {
                    ApplicationNo = application.ApplicationId,
                    application.ApplicantName,
                    application.ApplicationStatus,
                    AppliedDistrict,
                    AppliedService,
                    ApplicationWithOfficer
                };
                list.Add(obj);
            }


            return list;

        }
        public IActionResult GetFilteredCount(string? conditions)
        {
            var Conditions = JsonConvert.DeserializeObject<Dictionary<string, string>>(conditions!);
            var TotalCount = GetCount("Total", Conditions!);
            var PendingCount = GetCount("Pending", Conditions!);
            var RejectCount = GetCount("Reject", Conditions!);
            var SanctionCount = GetCount("Sanction", Conditions!);

            return Json(new { status = true, TotalCount, PendingCount, RejectCount, SanctionCount });
        }

        private static bool ValidateCertificate(byte[] certificateData, string password)
        {
            try
            {
                using (var cert = new X509Certificate2(certificateData, password))
                {
                    // Check if the certificate is expired
                    if (DateTime.Now > cert.NotAfter || DateTime.Now < cert.NotBefore)
                    {
                        return false;
                    }

                    // Check if the certificate is self-signed
                    if (cert.Subject == cert.Issuer)
                    {
                        // Perform any additional checks for self-signed certificates here
                        // For example, you can check the thumbprint or other properties

                        return true; // Assuming self-signed certificates are acceptable
                    }

                    // Check the certificate chain for non-self-signed certificates
                    using (var chain = new X509Chain())
                    {
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
                        chain.ChainPolicy.VerificationTime = DateTime.Now;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 30);

                        if (!chain.Build(cert))
                        {
                            return false;
                        }

                        // Ensure the certificate is issued by a trusted root CA
                        foreach (var chainElement in chain.ChainElements)
                        {
                            if (chainElement.Certificate.Thumbprint == cert.Thumbprint)
                            {
                                continue;
                            }

                            if (chainElement.Certificate.Subject == chainElement.Certificate.Issuer)
                            {
                                // This is the root certificate, ensure it's trusted
                                if (!IsTrustedRoot(chainElement.Certificate))
                                {
                                    return false;
                                }
                            }
                        }
                    }

                    // Additional validation checks can be added here
                    // For example, check the subject name, key usage, etc.

                    return true;
                }
            }
            catch (CryptographicException)
            {
                // Handle cryptographic exceptions (e.g., incorrect password, corrupted certificate)
                return false;
            }
            catch (Exception)
            {
                // Handle any other exceptions
                return false;
            }
        }

        private static bool IsTrustedRoot(X509Certificate2 rootCertificate)
        {
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
                chain.ChainPolicy.VerificationTime = DateTime.Now;
                chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 0, 30);

                if (chain.Build(rootCertificate))
                {
                    // Check if the root certificate is in the trusted root CA store
                    foreach (var chainElement in chain.ChainElements)
                    {
                        if (chainElement.Certificate.Subject == chainElement.Certificate.Issuer)
                        {
                            return chainElement.Certificate.Verify();
                        }
                    }
                }
            }

            return false;
        }

        private byte[] EncryptData(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(_configuration["EncryptionSettings:EncryptionKey"]!);
                aes.IV = Convert.FromBase64String(_configuration["EncryptionSettings:EncryptionIV"]!);

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                    return ms.ToArray();
                }
            }
        }

        private byte[] DecryptData(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(_configuration["EncryptionSettings:EncryptionKey"]!);
                aes.IV = Convert.FromBase64String(_configuration["EncryptionSettings:EncryptionIV"]!);

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(data))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new MemoryStream())
                {
                    cs.CopyTo(sr);
                    return sr.ToArray();
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> SignPdf(IFormFile pdfFile)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null || pdfFile == null)
            {
                return Json(new { status = false, response = "Invalid session or file." });
            }

            var userCertificate = dbcontext.Certificates.FirstOrDefault(c => c.OfficerId == userId);
            if (userCertificate == null)
            {
                return Json(new { status = false, response = "Certificate not found." });
            }

            var certificateData = DecryptData(userCertificate.EncryptedCertificateData);
            var password = Encoding.UTF8.GetString(DecryptData(userCertificate.EncryptedPassword));

            var signedPdfPath = Path.Combine(Path.GetTempPath(), "Signed_" + pdfFile.FileName);

            await using (var memoryStream = new MemoryStream())
            {
                await pdfFile.CopyToAsync(memoryStream);
                var pdfBytes = memoryStream.ToArray();

                await using (var pdfInputStream = new MemoryStream(pdfBytes))
                await using (var pdfOutputStream = new FileStream(signedPdfPath, FileMode.Create, FileAccess.Write))
                {
                    var reader = new PdfReader(pdfInputStream);
                    var writer = new PdfWriter(pdfOutputStream);
                    var pdfDoc = new PdfDocument(reader, writer);

                    var signer = new PdfSigner(reader, writer, new StampingProperties());

                    using (var cert = new X509Certificate2(certificateData, password))
                    {
                        // Convert .NET X509Certificate2 to BouncyCastle's X509Certificate
                        var bcCert = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(cert.RawData);

                        // Convert .NET RSA private key to BouncyCastle's RSA private key
                        var rsa = cert.GetRSAPrivateKey();
                        if (rsa == null)
                        {
                            return Json(new { status = false, response = "Private key not found." });
                        }

                        var rsaParameters = rsa.ExportParameters(true);
                        var bcPrivateKey = DotNetUtilities.GetRsaKeyPair(rsaParameters).Private;

                        var chain = new Org.BouncyCastle.X509.X509Certificate[] { bcCert };
                        var externalSignature = new PrivateKeySignature((iText.Commons.Bouncycastle.Crypto.IPrivateKey)bcPrivateKey, DigestAlgorithms.SHA256);

                        signer.SignDetached(new BouncyCastleDigest(), externalSignature, (iText.Commons.Bouncycastle.Cert.IX509Certificate[])chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
                    }
                }
            }

            return File(System.IO.File.ReadAllBytes(signedPdfPath), "application/pdf", "SignedDocument.pdf");
        }


    }
}