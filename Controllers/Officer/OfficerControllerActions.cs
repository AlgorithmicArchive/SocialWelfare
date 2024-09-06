using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {

        public static string ConvertCamelCaseToUppercaseWithSpaces(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder();

            foreach (char c in input)
            {
                if (char.IsUpper(c) && result.Length > 0)
                {
                    result.Append(' ');
                }
                result.Append(c);
            }

            return result.ToString().ToUpper();
        }
        public void Sanction(string ApplicationId, string Officer)
        {
            var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(ApplicationId);

            var sanctionObject = new Dictionary<string, string>
            {
                ["NAME OF APPLICANT"] = userDetails.ApplicantName.ToUpper(),
                ["DATE OF BIRTH"] = userDetails.DateOfBirth.ToString(),
                ["FATHER/GUARDIAN NAME"] = userDetails.RelationName.ToUpper(),
                ["MOTHER NAME"] = serviceSpecific!["MotherName"],
                ["MOBILE/EMAIL"] = userDetails.MobileNumber.ToUpper() + "/" + userDetails.Email.ToUpper(),
                ["DATE OF MARRIAGE"] = serviceSpecific["DateOfMarriage"],
                ["BANK NAME/ BRANCH NAME"] = bankDetails!["BankName"] + "/" + bankDetails["BranchName"],
                ["IFSC CODE/ ACCOUNT NUMBER"] = bankDetails["IfscCode"] + "/" + bankDetails["AccountNumber"],
                ["AMOUNT SANCTIONED"] = "50000",
                ["PRESENT ADDRESS"] = preAddressDetails.Address!.ToUpper() + ", TEHSIL: " + preAddressDetails.Tehsil + ", DISTRICT: " + preAddressDetails.District + ", PIN CODE: " + preAddressDetails.Pincode,
                ["PERMANENT ADDRESS"] = perAddressDetails.Address!.ToUpper() + ", TEHSIL: " + perAddressDetails.Tehsil + ", DISTRICT: " + perAddressDetails.District + ", PIN CODE: " + perAddressDetails.Pincode,
            };

            var letterUpdateDetails = dbcontext.UpdatedLetterDetails.FirstOrDefault(up => up.ApplicationId == ApplicationId);

            if (letterUpdateDetails != null)
            {
                var existingLetterUpdateDetails = JsonConvert.DeserializeObject<List<dynamic>>(letterUpdateDetails!.UpdatedDetails.ToString());

                if (existingLetterUpdateDetails != null && existingLetterUpdateDetails.Count != 0)
                {
                    var lastElement = existingLetterUpdateDetails.LastOrDefault();
                    if (lastElement != null && lastElement is JObject)
                    {
                        foreach (var property in (JObject)lastElement!)
                        {
                            var key = ConvertCamelCaseToUppercaseWithSpaces(property.Key.ToString());
                            var value = property.Value;
                            if (value != null && value["NewValue"] != null)
                            {
                                var newValue = value["NewValue"]!.ToString();
                                if (!string.IsNullOrEmpty(newValue))
                                    sanctionObject[key] = newValue;
                            }
                        }
                    }
                }
            }

            _pdfService.CreateSanctionPdf(sanctionObject, Officer, ApplicationId);
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

        private async Task UpdateRelatedRecordsCount(string officer, int serviceId, int forwardCount = 0, int returnCount = 0)
        {
            var recordsCount = await dbcontext.RecordCounts.FirstOrDefaultAsync(rc => rc.ServiceId == serviceId && rc.Officer == officer);
            _logger.LogInformation($"--------------------FORWARD: {forwardCount} RETURN :{returnCount}----------------------------------");

            if (recordsCount != null)
            {
                recordsCount.Forward += forwardCount;
                recordsCount.Return += returnCount;
                recordsCount.Pending++;
            }
        }


        [HttpPost]
        public async Task<IActionResult> HandleAction([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var currentOfficer = await dbcontext.Users.FindAsync(userId);
            string applicationId = form["ApplicationId"].ToString();
            var applicationIdParam = new SqlParameter("@ApplicationId", applicationId);
            string remarks = form["Remarks"].ToString();
            string action = form["Action"].ToString();
            int serviceId = Convert.ToInt32(form["ServiceId"].ToString());

            if (currentOfficer == null) return NotFound();

            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(currentOfficer.UserSpecificDetails);
            string officerDesignation = userSpecificDetails!.Designation;
            var workForceOfficers = JsonConvert.DeserializeObject<List<dynamic>>(dbcontext.Services.FirstOrDefault(s => s.ServiceId == serviceId)!.WorkForceOfficers!);
            var currentOfficerIndex = workForceOfficers!.FindIndex(o => o["Designation"] == officerDesignation);
            string nextOfficer = (currentOfficerIndex >= 0 && currentOfficerIndex + 1 < workForceOfficers.Count)
                ? workForceOfficers[currentOfficerIndex + 1]["Designation"]
                : string.Empty;

            string accessLevel = userSpecificDetails.AccessLevel?.ToString() ?? string.Empty;
            int accessCode = Convert.ToInt32(userSpecificDetails.AccessCode.ToString());

            // Fetch current, next, and previous phases in a single query
            var currentPhase = await dbcontext.CurrentPhases
      .FirstOrDefaultAsync(cur => cur.ApplicationId == applicationId && cur.Officer == officerDesignation);

            var nextPhase = await dbcontext.CurrentPhases
                .FirstOrDefaultAsync(n => n.PhaseId == currentPhase!.Next);

            var previousPhase = await dbcontext.CurrentPhases
                .FirstOrDefaultAsync(p => p.PhaseId == currentPhase!.Previous);




            var recordsCount = await dbcontext.RecordCounts
                .FirstOrDefaultAsync(rc => rc.ServiceId == serviceId && rc.Officer == officerDesignation && rc.AccessCode == accessCode);

            var nextRecordCount = await dbcontext.RecordCounts.FirstOrDefaultAsync(rc => rc.ServiceId == serviceId && rc.Officer == nextOfficer && (rc.AccessCode == accessCode || rc.AccessCode == 0));

            _logger.LogInformation($"--------------NEXT PHASE: {nextPhase == null} NEXTRECORDCoUNT:{nextRecordCount == null}--------------------");

            switch (action)
            {
                case "Forward":
                    UpdateRecordCounts(recordsCount!, pendingCount: -1, forwardCount: 1);
                    if (nextPhase == null && nextRecordCount == null)
                    {
                        dbcontext.RecordCounts.Add(new RecordCount
                        {
                            ServiceId = serviceId,
                            Officer = nextOfficer,
                            AccessCode = nextOfficer == "Director Finance" ? 0 : accessCode,
                            Pending = 1
                        });
                    }
                    else if (nextPhase == null && nextRecordCount != null)
                    {
                        await UpdateRelatedRecordsCount(nextOfficer, serviceId);
                    }
                    else if (nextPhase!.ActionTaken.Trim().Equals("Return", StringComparison.OrdinalIgnoreCase))
                    {
                        await UpdateRelatedRecordsCount(nextPhase!.Officer, serviceId, returnCount: -1);
                    }
                    else
                    {
                        await UpdateRelatedRecordsCount(nextPhase!.Officer, serviceId);
                    }
                    await HandleForward(form, currentPhase, officerDesignation, serviceId, remarks, applicationId, accessCode);
                    break;

                case "Return":
                    UpdateRecordCounts(recordsCount!, pendingCount: -1, returnCount: 1);

                    _logger.LogInformation($"-----------------ACTION TAKEN IN RETURN: {previousPhase!.ActionTaken}---------------");

                    if (previousPhase!.ActionTaken.Trim().Equals("Forward", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"-----------------ACTION TAKEN: {previousPhase!.ActionTaken}");
                        await UpdateRelatedRecordsCount(previousPhase.Officer, serviceId, forwardCount: -1);
                    }
                    HandleReturn(currentPhase, remarks);
                    break;

                case "ReturnToEdit":
                    UpdateRecordCounts(recordsCount!, pendingCount: -1, pendingWithCitizenCount: 1);
                    HandleReturnToEdit(currentPhase, form, applicationIdParam, officerDesignation, remarks);
                    break;

                case "Sanction":
                    UpdateRecordCounts(recordsCount!, pendingCount: -1, sanctionCount: 1);
                    HandleSanction(form, currentPhase, applicationId, officerDesignation, remarks);
                    break;

                case "Update":
                    await HandleUpdate(form, currentPhase, officerDesignation, remarks, serviceId, applicationId, accessCode);
                    break;

                case "Reject":
                    HandleReject(currentPhase, applicationIdParam, officerDesignation, remarks);
                    UpdateRecordCounts(recordsCount!, -1, rejectCount: 1);
                    break;
            }

            // await dbcontext.SaveChangesAsync();
            return Json(new { status = true, applicationId });
        }



        private async Task HandleForward(IFormCollection form, CurrentPhase? currentPhase, string officerDesignation, int serviceId, string remarks, string applicationId, int accessCode, bool update = false)
        {
            string file = await helper.GetFilePath(form.Files["ForwardFile"]);
            var workForceOfficers = JsonConvert.DeserializeObject<List<dynamic>>(dbcontext.Services.FirstOrDefault(s => s.ServiceId == serviceId)?.WorkForceOfficers ?? "[]");
            var currentOfficerIndex = workForceOfficers?.FindIndex(o => o["Designation"] == officerDesignation) ?? -1;

            string nextOfficer = (currentOfficerIndex >= 0 && currentOfficerIndex + 1 < workForceOfficers!.Count)
                ? workForceOfficers[currentOfficerIndex + 1]["Designation"].ToString()
                : string.Empty;

            var nextOfficerDetails = dbcontext.Users.AsEnumerable().FirstOrDefault(u =>
            {
                var userSpecificDetail = JsonConvert.DeserializeObject<dynamic>(u.UserSpecificDetails);
                return userSpecificDetail!["Designation"] == nextOfficer &&
                       (userSpecificDetail["AccessCode"] == accessCode || userSpecificDetail["AccessCode"] == 0);
            });

            var userSpecificDetail = JsonConvert.DeserializeObject<dynamic>(nextOfficerDetails!.UserSpecificDetails);
            int AccessCode = Convert.ToInt32(userSpecificDetail!["AccessCode"]);
            if (currentPhase != null)
            {
                if (currentPhase.Next == 0)
                {
                    var newPhase = new CurrentPhase
                    {
                        ApplicationId = applicationId,
                        ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
                        OfficerId = nextOfficerDetails!.UserId,
                        Officer = nextOfficer,
                        AccessCode = AccessCode,
                        ActionTaken = "Pending",
                        Remarks = string.Empty,
                        File = string.Empty,
                        CanPull = false,
                        Previous = currentPhase.PhaseId,
                        Next = 0
                    };

                    dbcontext.CurrentPhases.Add(newPhase);
                    await dbcontext.SaveChangesAsync();
                    currentPhase.Next = newPhase.PhaseId;
                }
                else
                {
                    var nextPhase = dbcontext.CurrentPhases.FirstOrDefault(curr => curr.PhaseId == currentPhase.Next);
                    if (nextPhase != null)
                    {
                        nextPhase.ActionTaken = "Pending";
                        nextPhase.Remarks = "";
                    }
                }

                currentPhase.ActionTaken = update ? "Update" : "Forward";
                currentPhase.Remarks = remarks;
                currentPhase.File = file;
                currentPhase.CanPull = nextOfficer != "Director Finance";

                await dbcontext.SaveChangesAsync();
            }


            if (!update)
                helper.UpdateApplicationHistory(applicationId, officerDesignation, "Forward", remarks, string.Empty, file);

        }

        private void HandleReturn(CurrentPhase? currentPhase, string remarks)
        {
            var previousPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase!.Previous);

            currentPhase!.ActionTaken = "Return";
            currentPhase.Remarks = remarks;
            currentPhase.File = "";
            currentPhase.CanPull = true;

            previousPhase!.ActionTaken = "Pending";
            previousPhase.Remarks = "";
            previousPhase.CanPull = false;

            helper.UpdateApplicationHistory(currentPhase.ApplicationId, currentPhase.Officer, "Return", remarks, "", "");
        }

        private void HandleReturnToEdit(CurrentPhase? currentPhase, IFormCollection form, SqlParameter applicationIdParam, string officerDesignation, string remarks)
        {
            if (currentPhase != null)
            {
                currentPhase.ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
                currentPhase.ActionTaken = "ReturnToEdit";
                currentPhase.Remarks = remarks;
                currentPhase.File = string.Empty;
                currentPhase.CanPull = false;
            }

            helper.UpdateApplication("EditList", form["editList"].ToString(), applicationIdParam);
            helper.UpdateApplicationHistory(currentPhase!.ApplicationId, officerDesignation, "ReturnToEdit", remarks, string.Empty, string.Empty);
        }

        private void HandleSanction(IFormCollection form, CurrentPhase? currentPhase, string applicationId, string officerDesignation, string remarks)
        {
            if (currentPhase != null)
            {
                currentPhase.ActionTaken = "Sanction";
                currentPhase.Remarks = remarks;
                currentPhase.File = string.Empty;
                currentPhase.CanPull = true;
            }

            var newLetterUpdateDetails = JsonConvert.DeserializeObject<dynamic>(form["letterUpdateDetails"].ToString());
            if (newLetterUpdateDetails != null)
            {
                var letterUpdateDetails = dbcontext.UpdatedLetterDetails.FirstOrDefault(up => up.ApplicationId == applicationId);
                List<dynamic> existingLetterUpdateDetails = letterUpdateDetails != null
                    ? JsonConvert.DeserializeObject<List<dynamic>>(letterUpdateDetails!.UpdatedDetails.ToString())!
                    : new List<dynamic>();

                existingLetterUpdateDetails.Add(newLetterUpdateDetails);

                if (letterUpdateDetails != null)
                {
                    letterUpdateDetails.UpdatedDetails = JsonConvert.SerializeObject(existingLetterUpdateDetails);
                    dbcontext.UpdatedLetterDetails.Update(letterUpdateDetails);
                }
                else
                {
                    dbcontext.UpdatedLetterDetails.Add(new UpdatedLetterDetail
                    {
                        ApplicationId = applicationId,
                        UpdatedDetails = JsonConvert.SerializeObject(existingLetterUpdateDetails)
                    });
                }
            }

            Sanction(applicationId, officerDesignation);
            helper.UpdateApplication("ApplicationStatus", "Sanctioned", new SqlParameter("@ApplicationId", applicationId));
            helper.UpdateApplicationHistory(applicationId, officerDesignation, "Sanction", remarks, string.Empty, string.Empty);
        }

        private async Task HandleUpdate(IFormCollection form, CurrentPhase? currentPhase, string officerDesignation, string remarks, int serviceId, string applicationId, int accessCode)
        {
            await HandleForward(form, currentPhase, officerDesignation, serviceId, remarks, applicationId, accessCode);

            string field = form["UpdateColumn"].ToString();
            string oldValue = "";
            string newValue = "";
            string updateColumn = "";
            string updateColumnValue = "";
            string updateColumnFile = await helper.GetFilePath(form.Files["UpdateColumnFile"]);

            var application = dbcontext.Applications.FirstOrDefault(app => app.ApplicationId == applicationId);
            var entityType = dbcontext.Model.FindEntityType(typeof(Application));
            if (entityType!.GetProperties().Any(p => p.Name == field))
            {
                var propertyInfo = typeof(Application).GetProperty(field);
                oldValue = propertyInfo!.GetValue(application)?.ToString()!;
                newValue = form["UpdateColumnValue"].ToString();
                updateColumn = field;
                updateColumnValue = newValue;
            }
            else
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(application!.ServiceSpecific);
                foreach (var item in serviceSpecific!)
                {
                    if (item.Name == field)
                    {
                        oldValue = item.Value.ToString();
                        newValue = form["UpdateColumnValue"].ToString();
                        item.Value = newValue;
                        updateColumn = "ServiceSpecific";
                        updateColumnValue = JsonConvert.SerializeObject(serviceSpecific);
                    }
                }
            }

            string desigShort = dbcontext.OfficersDesignations.FirstOrDefault(of => of.Designation == officerDesignation)!.DesignationShort;
            var updateObject = JsonConvert.SerializeObject(new { Officer = desigShort, ColumnName = field, OldValue = oldValue, NewValue = newValue, File = updateColumnFile });
            helper.UpdateApplicationHistory(applicationId, officerDesignation, "Update", remarks, updateObject, updateColumnFile);
        }

        private void HandleReject(CurrentPhase? currentPhase, SqlParameter applicationIdParam, string officerDesignation, string remarks)
        {
            if (currentPhase != null)
            {
                currentPhase.ActionTaken = "Reject";
                currentPhase.Remarks = remarks;
                currentPhase.File = string.Empty;
                currentPhase.CanPull = false;
            }

            helper.UpdateApplication("ApplicationStatus", "Rejected", applicationIdParam);
            helper.UpdateApplicationHistory(currentPhase!.ApplicationId, officerDesignation, "Reject", remarks, string.Empty, string.Empty);
        }

    }
}