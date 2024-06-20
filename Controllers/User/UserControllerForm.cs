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
            var BankDetailsParam = new SqlParameter("@BankDetails", "");
            var DocumentsParam = new SqlParameter("@Documents", "");
            var ApplicationStatusParam = new SqlParameter("@ApplicationStatus", "Incomplete");

            dbcontext.Database.ExecuteSqlRaw("EXEC InsertGneralApplicationDetails @ApplicationId,@CitizenId,@ServiceId,@ApplicantName,@ApplicantImage,@Email,@MobileNumber,@Relation,@RelationName,@DateOfBirth,@Category,@ServiceSpecific,@BankDetails,@Documents,@ApplicationStatus",
                ApplicationIdParam, CitizenIdParam, ServiceIdParam, ApplicantNameParam, ApplicantImageParam, EmailParam, MobileNumberParam, RelationParam, RelationNameParam, DateOfBirthParam, CateogryParam, ServiceSpecificParam, BankDetailsParam, DocumentsParam, ApplicationStatusParam);

            var updateRequest = new
            {
                column = "ServiceSpecific",
                formElement = new
                {
                    type = "date",
                    label = "Date Of Marriage",
                    name = "DateOfMarriage",
                    validationFunctions = new[] { "notEmpty", "isDateWithinRange" },
                    maxLength = "6",
                    minLength = "1",
                    isFormSpecific = true
                },
                newValue = "",
                requested = 0,
                updated = 0
            };

            helper.UpdateApplication("UpdateRequest", JsonConvert.SerializeObject(updateRequest), new SqlParameter("@ApplicationId", ApplicationId));

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

                _logger.LogInformation($"ApplicationID: {form["ApplicationId"]}, PresentAddressId: {presentAddressId}, PermanentAddressId: {permanentAddressId}");

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
                _logger.LogError(ex, "Error occurred while inserting address details.");
                return Json(new { status = false, message = ex.Message });
            }
        }
        public IActionResult InsertBankDetails([FromForm] IFormCollection form)
        {
            var ApplicationId = new SqlParameter("@ApplicationId", form["ApplicationId"].ToString());
            var bankDetails = form["bankDetails"].ToString();
            helper.UpdateApplication("BankDetails", bankDetails, ApplicationId);

            return Json(new { status = true, ApplicationId = form["ApplicationId"].ToString() });
        }
        public async Task<IActionResult> InsertDocuments([FromForm] IFormCollection form)
        {
            var applicationId = form["ApplicationId"].ToString();
            var ApplicationId = new SqlParameter("@ApplicationId", applicationId);
            var docs = new List<Document>();
            var addedLabels = new HashSet<string>();
            string[] labels = JsonConvert.DeserializeObject<string[]>(form["labels"].ToString()) ?? Array.Empty<string>();

            foreach (var label in labels)
            {
                if (addedLabels.Contains(label))
                {
                    continue;
                }

                string enclosure = form[$"{label}Enclosure"].ToString();
                string file = await helper.GetFilePath(form.Files[$"{label}File"]);
                var doc = new Document
                {
                    Label = label,
                    Enclosure = enclosure,
                    File = file
                };

                docs.Add(doc);
                addedLabels.Add(label);
            }

            var documents = JsonConvert.SerializeObject(docs);
            var workForceOfficers = JsonConvert.DeserializeObject<List<dynamic>>(form["workForceOfficers"].ToString()) ?? new List<dynamic>();

            var phases = workForceOfficers.Select((officer, index) => new
            {
                ReceivedOn = index == 0 ? DateTime.Now.ToString() : string.Empty,
                Officer = officer.Designation,
                HasApplication = index == 0,
                ActionTaken = index == 0 ? "Pending" : string.Empty,
                Remarks = string.Empty,
                CanPull = false
            }).ToList();

            // Update Phase, Documents, and ApplicationStatus in Applications Table
            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), ApplicationId);
            helper.UpdateApplication("Documents", documents, ApplicationId);
            helper.UpdateApplication("ApplicationStatus", "Initiated", ApplicationId);

            if (!form.ContainsKey("returnToEdit"))
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(applicationId);

                var details = new Dictionary<string, string>
                {
                    ["REFERENCE NUMBER"] = userDetails.ApplicationId,
                    ["APPLICANT NAME"] = userDetails.ApplicantName,
                    ["DATE OF MARRIAGE"] = serviceSpecific["DateOfMarriage"],
                    ["PRESENT ADDRESS"] = $"{preAddressDetails.Address}, TEHSIL: {preAddressDetails.Tehsil}, DISTRICT: {preAddressDetails.District}, PIN CODE: {preAddressDetails.Pincode}",
                    ["PERMANENT ADDRESS"] = $"{perAddressDetails.Address}, TEHSIL: {perAddressDetails.Tehsil}, DISTRICT: {perAddressDetails.District}, PIN CODE: {perAddressDetails.Pincode}"
                };

                _pdfService.CreateAcknowledgement(details, userDetails.ApplicationId);
            }

            if (form.ContainsKey("returnToEdit"))
            {
                helper.UpdateApplication("EditList", "[]", ApplicationId);
                helper.UpdateApplicationHistory(applicationId, "Citizen", $"Edited and returned to {workForceOfficers[0].Designation}", "NULL");
            }
            else
            {
                helper.UpdateApplicationHistory(applicationId, "Citizen", "Application Submitted.", "NULL");
            }

            string email = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == applicationId)!.Email;

            if (email != null)
            {
                await emailSender.SendEmail(email, "Acknowledgement", $"Your Application with Reference Number {applicationId} has been sent to {workForceOfficers[0].Designation} at {DateTime.Now:dd MMM yyyy hh:mm tt}");
            }

            return Json(new { status = true, ApplicationId = applicationId, complete = true });
        }

        public async Task<IActionResult> UpdateGeneralDetails([FromForm] IFormCollection form)
        {
            string ApplicationId = form["ApplicationId"].ToString();
            var parameters = new List<SqlParameter>();
            var applicationIdParameter = new SqlParameter("@ApplicationId", ApplicationId);
            parameters.Add(applicationIdParameter);

            _logger.LogInformation($"IMAGE: {form.Files["ApplicantImage"]!.FileName}");

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
            var bankDetails = form["bankDetails"].ToString();
            helper.UpdateApplication("BankDetails", bankDetails, ApplicationId);

            return Json(new { status = true, ApplicationId = form["ApplicationId"].ToString() });
        }
        public IActionResult UpdateEditList([FromForm] IFormCollection form)
        {

            _logger.LogInformation($"WorkForceOfficer: {form["workForceOficers"].ToString()}");


            var workForceOfficers = JsonConvert.DeserializeObject<List<dynamic>>(form["workForceOfficers"].ToString());

            List<object> Phases = [];

            for (var i = 0; i < workForceOfficers!.Count; i++)
            {
                var phase = new
                {
                    ReceivedOn = i == 0 ? DateTime.Now.ToString() : "",
                    Officer = workForceOfficers[i]["Designation"],
                    HasApplication = i == 0,
                    ActionTaken = "",
                    Remarks = "",
                    CanPull = false
                };
                Phases.Add(phase);
            }

            // Update Phase in Applications Table
            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(Phases.ToArray()), new SqlParameter("@ApplicationId", form["ApplicationId"].ToString()));
            // Update/Insert Documents in Applications Table


            helper.UpdateApplication("EditList", "[]", new SqlParameter("@ApplicationId", form["ApplicationId"].ToString()));


            return Json(new { status = true });
        }

    }
}