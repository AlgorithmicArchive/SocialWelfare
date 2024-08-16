using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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


        // [HttpPost]
        // public async Task<IActionResult> Action([FromForm] IFormCollection form)
        // {
        //     int? userId = HttpContext.Session.GetInt32("UserId");
        //     var officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
        //     string updateObject = "";

        //     var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(officer!.UserSpecificDetails);
        //     string officerDesignation = userSpecificDetails!.Designation;
        //     string districtCode = userSpecificDetails.DistrictCode;

        //     string applicationId = form["ApplicationId"].ToString();
        //     var applicationIdParam = new SqlParameter("@ApplicationId", applicationId);
        //     string action = form["Action"].ToString();
        //     string remarks = form["Remarks"].ToString();

        //     string nextOfficer = "";

        //     var application = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == applicationId);
        //     var workForceOfficers = JsonConvert.DeserializeObject<dynamic>(dbcontext.Services.FirstOrDefault(s => s.ServiceId == application!.ServiceId)!.WorkForceOfficers!);
        //     var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(s => s.PhaseId == application!.Phase);

        //     string email = application!.Email;
        //     // var phases = JsonConvert.DeserializeObject<List<dynamic>>(application.Phase);

        //     string file = await helper.GetFilePath(form.Files["ForwardFile"]);

        //     int currentOfficerIndex = 0;
        //     foreach (var wofficer in workForceOfficers!)
        //     {
        //         if (wofficer["Designation"] == officerDesignation)
        //             break;
        //         currentOfficerIndex++;
        //     }


        //     if (action == "Forward")
        //     {
        //         var newPhase = new CurrentPhase
        //         {
        //             ApplicationId = application.ApplicationId,
        //             ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
        //             Officer = workForceOfficers[currentOfficerIndex + 1]["Designation"],
        //             ActionTaken = "Pending",
        //             Remarks = "",
        //             CanPull = workForceOfficers[currentOfficerIndex + 1]["Designation"] != "Director Finance",
        //             Next = 0,
        //             Previous = currentPhase!.PhaseId,
        //         };

        //         dbcontext.CurrentPhases.Add(newPhase);
        //         await dbcontext.SaveChangesAsync();
        //         currentPhase.ActionTaken = action == "Update" ? "Forward" : action;
        //         currentPhase.Remarks = remarks;
        //         currentPhase.Next = newPhase.PhaseId;

        //     }
        //     else if (action == "Return")
        //     {
        //         var previousPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase!.Previous);
        //         previousPhase!.ActionTaken = "Pending";
        //         previousPhase.Next = 0;
        //         previousPhase.CanPull = false;
        //         dbcontext.CurrentPhases.Remove(currentPhase!);
        //         await dbcontext.SaveChangesAsync();
        //     }
        //     else if (action == "ReturnToEdit")
        //     {
        //         currentPhase!.ActionTaken = action;
        //     }
        //     else if (action == "Sanction")
        //     {
        //         currentPhase!.ActionTaken = action;
        //         currentPhase.CanPull = true;
        //     }

        //     dbcontext.SaveChanges();

        //     // for (int i = 0; i < phases!.Count; i++)
        //     // {
        //     //     if (phases[i].Officer == officerDesignation)
        //     //     {
        //     //         phases[i].HasApplication = false;
        //     //         phases[i].ActionTaken = action == "Update" ? "Forward" : action;
        //     //         phases[i].Remarks = remarks;
        //     //         phases[i].CanPull = action == "ReturnToEdit";

        //     //         if (action == "Forward")
        //     //         {
        //     //             phases[i].CanPull = phases[i + 1].Officer != "Director Finance";
        //     //             if (i + 1 < phases.Count)
        //     //             {
        //     //                 phases[i + 1].HasApplication = true;
        //     //                 phases[i + 1].ActionTaken = "Pending";
        //     //                 phases[i + 1].ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
        //     //                 nextOfficer = phases[i + 1].Officer;
        //     //             }
        //     //         }
        //     //         else if (action == "ReturnToEdit")
        //     //         {
        //     //             phases[i].ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
        //     //         }
        //     //         else if (action == "Update")
        //     //         {
        //     //             string Field = form["UpdateColumn"].ToString();
        //     //             string OldValue = "";
        //     //             string NewValue = "";
        //     //             string UpdateColumn = "";
        //     //             string UpdateColumnValue = "";
        //     //             string UpdateColumnFile = await helper.GetFilePath(form.Files["UpdateColumnFile"]);

        //     //             var entityType = dbcontext.Model.FindEntityType(typeof(Application));
        //     //             if (entityType!.GetProperties().Any(p => p.Name == Field))
        //     //             {
        //     //                 var propertyInfo = typeof(Application).GetProperty(Field);
        //     //                 OldValue = propertyInfo!.GetValue(application)?.ToString()!;
        //     //                 NewValue = form["UpdateColumnValue"].ToString();
        //     //                 UpdateColumn = Field;
        //     //                 UpdateColumnValue = NewValue;
        //     //             }
        //     //             else
        //     //             {
        //     //                 var ServiceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
        //     //                 foreach (var item in ServiceSpecific!)
        //     //                 {

        //     //                     if (item.Name == Field)
        //     //                     {
        //     //                         OldValue = item.Value.ToString();
        //     //                         NewValue = form["UpdateColumnValue"].ToString();
        //     //                         item.Value = NewValue;
        //     //                         UpdateColumn = "ServiceSpecific";
        //     //                         UpdateColumnValue = JsonConvert.SerializeObject(ServiceSpecific);
        //     //                     }
        //     //                 }
        //     //             }

        //     //             string desigShort = dbcontext.OfficersDesignations.FirstOrDefault(of => of.Designation == officerDesignation)!.DesignationShort;
        //     //             updateObject = JsonConvert.SerializeObject(new { Officer = desigShort, ColumnName = Field, OldValue, NewValue, File = UpdateColumnFile });

        //     //             phases[i].CanPull = true;
        //     //             if (i + 1 < phases.Count)
        //     //             {
        //     //                 phases[i + 1].HasApplication = true;
        //     //                 phases[i + 1].ActionTaken = "Pending";
        //     //                 phases[i + 1].ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
        //     //                 nextOfficer = phases[i + 1].Officer;
        //     //             }
        //     //             helper.UpdateApplication(UpdateColumn, UpdateColumnValue, applicationIdParam);
        //     //         }
        //     //         else if (action == "Return" && i - 1 >= 0)
        //     //         {
        //     //             phases[i - 1].HasApplication = true;
        //     //             phases[i - 1].ActionTaken = "Pending";
        //     //             nextOfficer = phases[i - 1].Officer;
        //     //         }
        //     //         else if (action == "Sanction")
        //     //         {
        //     //             phases[i].CanPull = true;
        //     //         }
        //     //     }
        //     // }

        //     string emailAction = action == "Update" ? "Forwarded" : action + "ed";
        //     await emailSender.SendEmail(
        //         email,
        //         "Acknowledgement",
        //         $"Your Application with Reference Number {applicationId} is {emailAction} by {officerDesignation}" +
        //         (nextOfficer != "" ? $" to {nextOfficer}" : "") +
        //         $" at {DateTime.Now:dd MMM yyyy hh:mm tt}"
        //     );

        //     // helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), applicationIdParam);
        //     helper.UpdateApplication("EditList", form["editList"].ToString(), applicationIdParam);
        //     helper.UpdateApplicationHistory(applicationId, officerDesignation, action, remarks, updateObject, file);

        //     if (action == "Sanction")
        //     {
        //         var newLetterUpdateDetails = JsonConvert.DeserializeObject<dynamic>(form["letterUpdateDetails"].ToString());

        //         if (newLetterUpdateDetails != null)
        //         {
        //             var letterUpdateDetails = dbcontext.UpdatedLetterDetails.FirstOrDefault(up => up.ApplicationId == applicationId);

        //             if (letterUpdateDetails != null)
        //             {
        //                 var existingLetterUpdateDetails = JsonConvert.DeserializeObject<List<dynamic>>(letterUpdateDetails!.UpdatedDetails.ToString());
        //                 existingLetterUpdateDetails!.Add(newLetterUpdateDetails);
        //                 letterUpdateDetails.UpdatedDetails = JsonConvert.SerializeObject(existingLetterUpdateDetails);
        //                 dbcontext.UpdatedLetterDetails.Update(letterUpdateDetails);
        //                 dbcontext.SaveChanges();
        //             }
        //             else
        //             {
        //                 letterUpdateDetails = new UpdatedLetterDetail
        //                 {
        //                     ApplicationId = applicationId,
        //                     UpdatedDetails = JsonConvert.SerializeObject(new List<dynamic> { newLetterUpdateDetails })
        //                 };

        //                 dbcontext.UpdatedLetterDetails.Add(letterUpdateDetails);
        //                 dbcontext.SaveChanges();  // Ensure to save the changes here as well
        //             }
        //         }

        //         Sanction(applicationId, officerDesignation);
        //     }

        //     if (action == "Sanction" || action == "Reject")
        //     {
        //         helper.UpdateApplication("ApplicationStatus", $"{action}ed", applicationIdParam);
        //     }

        //     return Json(new { status = true, url = "/Officer/Index", ApplicationId = applicationId });
        // }

        [HttpPost]
        public async Task<IActionResult> HandleAction([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var currentOfficer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(currentOfficer!.UserSpecificDetails);
            string officerDesignation = userSpecificDetails!.Designation;

            string applicationId = form["ApplicationId"].ToString();
            var applicationIdParam = new SqlParameter("@ApplicationId", applicationId);
            string remarks = form["Remarks"].ToString();
            string action = form["Action"].ToString();
            int serviceId = Convert.ToInt32(form["ServiceId"].ToString());
            var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == applicationId && cur.Officer == officerDesignation);

            switch (action)
            {
                case "Forward":
                    await HandleForward(form, currentPhase, officerDesignation, serviceId, remarks, applicationId);
                    break;

                case "Return":
                    HandleReturn(currentPhase, remarks);
                    break;

                case "ReturnToEdit":
                    HandleReturnToEdit(currentPhase, form, applicationIdParam, officerDesignation, remarks);
                    break;

                case "Sanction":
                    HandleSanction(form, currentPhase, applicationId, officerDesignation, remarks);
                    break;

                case "Update":
                    await HandleUpdate(form, currentPhase, officerDesignation, remarks, serviceId, applicationId);
                    break;

                case "Reject":
                    HandleReject(currentPhase, applicationIdParam, officerDesignation, remarks);
                    break;
            }

            dbcontext.SaveChanges();
            return Json(new { status = true });
        }

        private async Task HandleForward(IFormCollection form, CurrentPhase? currentPhase, string officerDesignation, int serviceId, string remarks, string applicationId, bool update = false)
        {
            string file = await helper.GetFilePath(form.Files["ForwardFile"]);
            var workForceOfficers = JsonConvert.DeserializeObject<List<dynamic>>(dbcontext.Services.FirstOrDefault(s => s.ServiceId == serviceId)!.WorkForceOfficers!);
            var currentOfficerIndex = workForceOfficers!.FindIndex(o => o["Designation"] == officerDesignation);
            string nextOfficer = (currentOfficerIndex >= 0 && currentOfficerIndex + 1 < workForceOfficers.Count)
                ? workForceOfficers[currentOfficerIndex + 1]["Designation"]
                : string.Empty;

            if (currentPhase != null)
            {
                if (currentPhase.Next == 0)
                {
                    var newPhase = new CurrentPhase
                    {
                        ApplicationId = applicationId,
                        ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
                        Officer = nextOfficer,
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

        private async Task HandleUpdate(IFormCollection form, CurrentPhase? currentPhase, string officerDesignation, string remarks, int serviceId, string applicationId)
        {
            await HandleForward(form, currentPhase, officerDesignation, serviceId, remarks, applicationId);

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