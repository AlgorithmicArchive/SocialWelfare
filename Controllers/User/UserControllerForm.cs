using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.User
{
    public partial class UserController
    {

        public class Document
        {
            public string? Label { get; set; }
            public string? Enclosure { get; set; }
            public string? File { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> InsertGeneralDetails([FromForm] IFormCollection form)
        {
            var ServiceSpecific = JsonConvert.DeserializeObject<Dictionary<string, string>>(form["ServiceSpecific"].ToString());

            int.TryParse(ServiceSpecific!["District"], out int districtId);
            int.TryParse(form["ServiceId"].ToString(), out int serviceId);
            string ApplicationId = helper.GenerateApplicationId(districtId, dbcontext, _logger);
            IFormFile? photographFile = form.Files["ApplicantImage"];
            string? photographPath = await helper.GetFilePath(photographFile);
            int? userId = HttpContext.Session.GetInt32("UserId");


            var ApplicationIdParam = new SqlParameter("@ApplicationId", ApplicationId);
            var ServiceIdParam = new SqlParameter("@ServiceId", serviceId);
            var ApplicantNameParam = new SqlParameter("@ApplicantName", form["ApplicantName"].ToString());
            var ApplicantImageParam = new SqlParameter("@ApplicantImage", photographPath);
            var RelationParam = new SqlParameter("@Relation", form["Relation"].ToString());
            var RelationNameParam = new SqlParameter("@RelationName", form["RelationName"].ToString());
            var DateOfBirthParam = new SqlParameter("@DateOfBirth", form["DateOfBirth"].ToString());
            var CateogryParam = new SqlParameter("@Category", form["Category"].ToString());
            var ServiceSpecificParam = new SqlParameter("@ServiceSpecific", form["ServiceSpecific"].ToString());
            var CitizenIdParam = new SqlParameter("@CitizenId", userId);
            var EmailParam = new SqlParameter("@Email", form["Email"].ToString());
            var MobileNumberParam = new SqlParameter("@MobileNumber", form["MobileNumber"].ToString());
            var BankDetailsParam = new SqlParameter("@BankDetails", "{}");
            var DocumentsParam = new SqlParameter("@Documents", "[]");
            var ApplicationStatusParam = new SqlParameter("@ApplicationStatus", "Incomplete");

            dbcontext.Database.ExecuteSqlRaw("EXEC InsertGeneralApplicationDetails @ApplicationId,@CitizenId,@ServiceId,@ApplicantName,@ApplicantImage,@Email,@MobileNumber,@Relation,@RelationName,@DateOfBirth,@Category,@ServiceSpecific,@BankDetails,@Documents,@ApplicationStatus",
                ApplicationIdParam, CitizenIdParam, ServiceIdParam, ApplicantNameParam, ApplicantImageParam, EmailParam, MobileNumberParam, RelationParam, RelationNameParam, DateOfBirthParam, CateogryParam, ServiceSpecificParam, BankDetailsParam, DocumentsParam, ApplicationStatusParam);


            return Json(new
            {
                status = true,
                ApplicationId
            });
        }
        public IActionResult InsertAddressDetails([FromForm] IFormCollection form)
        {
            try
            {
                var applicationId = new SqlParameter("@ApplicationId", form["ApplicationId"].ToString());
                string? sameAsPresent = form["SameAsPresent"];

                var presentAddressParams = helper.GetAddressParameters(form, "Present");
                var permanentAddressParams = helper.GetAddressParameters(form, "Permanent");

                List<Address>? presentAddress = null;
                List<Address>? permanentAddress = null;
                int? presentAddressId = null;
                int? permanentAddressId = null;

                if (presentAddressParams != null)
                {
                    presentAddress = dbcontext.Addresses
                        .FromSqlRaw("EXEC CheckAndInsertAddress @DistrictId,@TehsilId,@BlockId,@HalqaPanchayatName,@VillageName,@WardName,@Pincode,@AddressDetails", presentAddressParams)
                        .ToList();

                    if (presentAddress != null && presentAddress.Count > 0)
                    {
                        presentAddressId = presentAddress[0].AddressId;
                        helper.UpdateApplication("PresentAddressId", presentAddressId.ToString()!, applicationId);

                        if (string.IsNullOrEmpty(sameAsPresent))
                        {
                            permanentAddress = dbcontext.Addresses
                                .FromSqlRaw("EXEC CheckAndInsertAddress @DistrictId,@TehsilId,@BlockId,@HalqaPanchayatName,@VillageName,@WardName,@Pincode,@AddressDetails", permanentAddressParams!)
                                .ToList();

                            if (permanentAddress != null && permanentAddress.Count > 0)
                            {
                                permanentAddressId = permanentAddress[0].AddressId;
                                helper.UpdateApplication("PermanentAddressId", permanentAddressId.ToString()!, applicationId);
                            }
                        }
                        else
                        {
                            permanentAddressId = presentAddressId;
                            helper.UpdateApplication("PermanentAddressId", presentAddressId.ToString()!, applicationId);
                        }
                    }
                }

                return Json(new
                {
                    status = true,
                    ApplicationId = form["ApplicationId"].ToString(),
                    PresentAddressId = presentAddressId,
                    PermanentAddressId = permanentAddressId
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        public IActionResult InsertBankDetails([FromForm] IFormCollection form)
        {
            var ApplicationId = new SqlParameter("@ApplicationId", form["ApplicationId"].ToString());

            var bankDetails = new
            {
                BankName = form["BankName"].ToString(),
                BranchName = form["BranchName"].ToString(),
                AccountNumber = form["AccountNumber"].ToString(),
                IfscCode = form["IfscCode"].ToString(),
            };


            helper.UpdateApplication("BankDetails", JsonConvert.SerializeObject(bankDetails), ApplicationId);

            return Json(new { status = true, ApplicationId = form["ApplicationId"].ToString() });
        }
        public async Task<IActionResult> InsertDocuments([FromForm] IFormCollection form)
        {
            _logger.LogInformation("------------- INSERT DOCUMENT-------------------");
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN-------------------");
            
            var applicationId = form["ApplicationId"].ToString();
            if (string.IsNullOrEmpty(applicationId))
            {
                return Json(new{message="ApplicationId is missing"});
            }
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 1-------------------");

            if (!int.TryParse(form["ServiceId"].ToString(), out int serviceId))
            {
                return Json(new { message = "ServiceId is missing or invalid" });
            }
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 2-------------------");

            if (!int.TryParse(form["AccessCode"].ToString(), out int accessCode))
            {
                return Json(new { message = "AccessCode is missing or invalid" });
            }
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 3-------------------");

            var labels = JsonConvert.DeserializeObject<string[]>(form["labels"].ToString()) ?? [];
            var docs = new List<Document>();
            var addedLabels = new HashSet<string>();
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 4-------------------");

            foreach (var label in labels)
            {
                if (addedLabels.Add(label))
                {
                    var file = form.Files[$"{label}File"];
                    if (file == null)
                    {
                        return Json(new{message=$"{label}File is missing"});
                    }

                    var doc = new Document
                    {
                        Label = label,
                        Enclosure = form[$"{label}Enclosure"].ToString(),
                        File = await helper.GetFilePath(file)
                    };
                    docs.Add(doc);
                }
            }
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 5-------------------");

            var documents = JsonConvert.SerializeObject(docs);
            var service = dbcontext.Services.FirstOrDefault(service=>service.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<List<dynamic>>(service!.WorkForceOfficers!) ?? [];
            
            if (workForceOfficers.Count == 0)
            {
                return Json(new{message="No workforce officers available"});
            }
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 6-------------------");

            string officer = workForceOfficers[0]!.Designation;
            _logger.LogInformation($"-----------OFFICER: {officer} ACCESSCODE: {accessCode}-------------------------");
            var newPhase = new CurrentPhase
            {
                ApplicationId = applicationId,
                ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
                Officer = workForceOfficers[0].Designation,
                AccessCode = accessCode,
                ActionTaken = "Pending",
                Remarks = "",
                CanPull = false,
            };
            dbcontext.CurrentPhases.Add(newPhase);
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 8-------------------");


            var recordCount = await dbcontext.RecordCounts
                .FirstOrDefaultAsync(rc => rc.ServiceId == serviceId && rc.Officer == officer && rc.AccessCode == accessCode);
          
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 9-------------------");

            if (recordCount != null)
            {
                recordCount.Pending++;
            }

            else
            {
                dbcontext.RecordCounts.Add(new RecordCount
                {
                    ServiceId = serviceId,
                    Officer = officer,
                    AccessCode = accessCode,
                    Pending = 1
                });
            }

            await dbcontext.SaveChangesAsync();
            _logger.LogInformation("------------- INSERT DOCUMENT DOWN 10-------------------");

            // Consolidate UpdateApplication calls
            var updates = new Dictionary<string, string>
            {
                { "Phase", JsonConvert.SerializeObject(newPhase.PhaseId) },
                { "Documents", documents },
                { "ApplicationStatus", "Initiated" },
                { "SubmissionDate", DateTime.Now.ToString("dd MMM yyyy hh:mm tt") }
            };

            foreach (var update in updates)
            {
                helper.UpdateApplication(update.Key, update.Value, new SqlParameter("@ApplicationId", applicationId));
            }

            if (!form.ContainsKey("returnToEdit"))
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(applicationId);
                int districtCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.DistrictName.ToUpper()!;

                var details = new Dictionary<string, string>
                {
                    ["REFERENCE NUMBER"] = userDetails.ApplicationId,
                    ["APPLICANT NAME"] = userDetails.ApplicantName,
                    ["PARENTAGE"] = $"{userDetails.RelationName} ({userDetails.Relation.ToUpper()})",
                    ["MOTHER NAME"] = serviceSpecific["MotherName"],
                    ["APPLIED DISTRICT"] = appliedDistrict,
                    ["BANK NAME"] = bankDetails["BankName"],
                    ["ACCOUNT NUMBER"] = bankDetails["AccountNumber"],
                    ["IFSC CODE"] = bankDetails["IfscCode"],
                    ["DATE OF MARRIAGE"] = serviceSpecific["DateOfMarriage"],
                    ["DATE OF SUBMISSION"] = userDetails.SubmissionDate,
                    ["PRESENT ADDRESS"] = $"{preAddressDetails.Address}, TEHSIL: {preAddressDetails.Tehsil}, DISTRICT: {preAddressDetails.District}, PIN CODE: {preAddressDetails.Pincode}",
                    ["PERMANENT ADDRESS"] = $"{perAddressDetails.Address}, TEHSIL: {perAddressDetails.Tehsil}, DISTRICT: {perAddressDetails.District}, PIN CODE: {perAddressDetails.Pincode}"
                };
                helper.UpdateApplicationHistory(applicationId, "Citizen", "Application Submitted.", "NULL");
                _pdfService.CreateAcknowledgement(details, userDetails.ApplicationId);
            }
            else
            {
                helper.UpdateApplication("EditList", "[]", new SqlParameter("@ApplicationId", applicationId));
                helper.UpdateApplicationHistory(applicationId, "Citizen", $"Edited and returned to {officer}", "NULL");
            }

            var email = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == applicationId)?.Email;

            if (!string.IsNullOrWhiteSpace(email))
            {
                await emailSender.SendEmail(email, "Acknowledgement", $"Your Application with Reference Number {applicationId} has been sent to {officer} at {DateTime.Now:dd MMM yyyy hh:mm tt}");
            }
            HttpContext.Session.SetString("ApplicationId",applicationId);
            return Json(new { status = true, ApplicationId = applicationId, complete = true });
        }
        public async Task<IActionResult> UpdateGeneralDetails([FromForm] IFormCollection form)
        {

            string ApplicationId = form["ApplicationId"].ToString();
            var parameters = new List<SqlParameter>();
            var applicationIdParameter = new SqlParameter("@ApplicationId", ApplicationId);
            parameters.Add(applicationIdParameter);
            foreach (var key in form.Keys)
            {
                SqlParameter parameter;
                if (key == "ApplicationId")
                    continue;

                parameter = new SqlParameter($"@{key}", form[key].ToString());
                parameters.Add(parameter);
            }

            foreach (var file in form.Files)
            {
                string path = await helper.GetFilePath(file);
                var parameter = new SqlParameter($"@{file.Name}", path);
                parameters.Add(parameter);
            }
            var sqlParams = string.Join(", ", parameters.Select(p => p.ParameterName + "=" + p.ParameterName));
            var sqlQuery = $"EXEC UpdateApplicationColumns {sqlParams}";
            // Execute SQL command
            dbcontext.Database.ExecuteSqlRaw(sqlQuery, parameters.ToArray());
            return Json(new { status = true, ApplicationId });
        }
        public IActionResult UpdateAddressDetails([FromForm] IFormCollection form)
        {
            string ApplicationId = form["ApplicationId"].ToString();
            var presentAddressParams = new List<SqlParameter>();
            var permanentAddressParams = new List<SqlParameter>();

            presentAddressParams.Add(new SqlParameter("@AddressId", Convert.ToInt32(form["PresentAddressId"])));
            permanentAddressParams.Add(new SqlParameter("@AddressId", Convert.ToInt32(form["PermanentAddressId"])));

            foreach (var key in form.Keys)
            {
                // Skip keys that are not relevant to address update
                if (key == "ApplicationId" || key == "PresentAddressId" || key == "PermanentAddressId")
                    continue;

                // Create SqlParameter for present address
                if (!key.StartsWith("Permanent"))
                {
                    presentAddressParams.Add(new SqlParameter($"@{key.Replace("Present", "")}", form[key].ToString()));
                }
                // Create SqlParameter for permanent address
                else
                {
                    permanentAddressParams.Add(new SqlParameter($"@{key.Replace("Permanent", "")}", form[key].ToString()));
                }
            }

            // Execute SQL command for present address update
            var presentSqlParams = string.Join(", ", presentAddressParams.Select(p => p.ParameterName + "=" + p.ParameterName));
            var presentSqlQuery = $"EXEC CheckAndUpdateAddress {presentSqlParams}";
            dbcontext.Database.ExecuteSqlRaw(presentSqlQuery, presentAddressParams.ToArray());

            // Execute SQL command for permanent address update if different
            if (form["PresentAddressId"].ToString() != form["PermanentAddressId"].ToString())
            {
                var permSqlParams = string.Join(", ", permanentAddressParams.Select(p => p.ParameterName + "=" + p.ParameterName));
                var permSqlQuery = $"EXEC CheckAndUpdateAddress {permSqlParams}";
                dbcontext.Database.ExecuteSqlRaw(permSqlQuery, permanentAddressParams.ToArray());
            }

            return Json(new { status = true, ApplicationId });
        }
        public IActionResult UpdateBankDetails([FromForm] IFormCollection form)
        {
            var ApplicationId = new SqlParameter("@ApplicationId", form["ApplicationId"].ToString());

            var bankDetails = new
            {
                BankName = form["BankName"].ToString(),
                BranchName = form["BranchName"].ToString(),
                AccountNumber = form["AccountNumber"].ToString(),
                IfscCode = form["IfscCode"].ToString(),
            };

            helper.UpdateApplication("BankDetails", JsonConvert.SerializeObject(bankDetails), ApplicationId);

            return Json(new { status = true, ApplicationId = form["ApplicationId"].ToString() });
        }
        public IActionResult UpdateEditList([FromForm] IFormCollection form)
        {

            var workForceOfficers = JsonConvert.DeserializeObject<List<dynamic>>(form["workForceOfficers"].ToString());
            int serviceId = Convert.ToInt32(form["serviceId"].ToString());
            int accessCode = Convert.ToInt32(form["District"]);
            string officerDesignation = workForceOfficers![0].Designation;

            var recordsCount = dbcontext.RecordCounts
              .FirstOrDefault(rc => rc.ServiceId == serviceId && rc.Officer == officerDesignation && rc.AccessCode == accessCode);

            UpdateRecordCounts(recordsCount!, pendingCount: 1, pendingWithCitizenCount: -1);

           string Officer = workForceOfficers[0].Designation;

            var CurrentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == form["ApplicationId"] && cur.Officer == Officer);
            CurrentPhase!.ReceivedOn =DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
            CurrentPhase.ActionTaken = "Pending";
            CurrentPhase.Remarks = string.Empty;
            CurrentPhase.CanPull = false;

            dbcontext.SaveChanges();

            helper.UpdateApplication("EditList", "[]", new SqlParameter("@ApplicationId", form["ApplicationId"].ToString()));
            helper.UpdateApplicationHistory(form["ApplicationId"].ToString(), "Citizen", "Edited and returned to " + workForceOfficers[0].Designation, "NULL");


            return Json(new { status = true });
        }


        private static void UpdateRecordCounts(RecordCount recordsCount, int pendingCount = 0, int pendingWithCitizenCount = 0, int forwardCount = 0, int returnCount = 0, int sanctionCount = 0, int rejectCount = 0)
        {
            if (recordsCount == null) return;
            recordsCount.Pending += pendingCount;
            recordsCount.PendingWithCitizen += pendingWithCitizenCount;
            recordsCount.Forward += forwardCount;
            recordsCount.Return += returnCount;
            recordsCount.Sanction += sanctionCount;
            recordsCount.Reject += rejectCount;
        }

        public IActionResult IncompleteApplication(string ApplicationId, string DistrictId)
        {
            var ApplicationIdParam = new SqlParameter("@ApplicationId", ApplicationId);
            int? serviceId = dbcontext.Applications.FirstOrDefault(app => app.ApplicationId == ApplicationId)!.ServiceId;
            int accessCode = Convert.ToInt32(DistrictId);
            var workForceOfficer = JsonConvert.DeserializeObject<List<dynamic>>(dbcontext.Services.FirstOrDefault(s => s.ServiceId == serviceId)!.WorkForceOfficers!);
            string officerDesignation = workForceOfficer![0]["Designation"];
            var recordsCount = dbcontext.RecordCounts
              .FirstOrDefault(rc => rc.ServiceId == serviceId && rc.Officer == officerDesignation && rc.AccessCode == accessCode);
            UpdateRecordCounts(recordsCount!, pendingCount: 1);
            helper.UpdateApplication("ApplicationStatus", "Initiated", ApplicationIdParam);
            helper.UpdateApplicationHistory(ApplicationId, "Citizen", "Application Submitted.", "NULL");
            return Json(new { status = true });
        }

    }
}