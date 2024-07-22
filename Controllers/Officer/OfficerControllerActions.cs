using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {

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
            _pdfService.CreateSanctionPdf(sanctionObject, Officer, ApplicationId);
        }


        [HttpPost]
        public async Task<IActionResult> Action([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            string updateObject = "";

            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(officer!.UserSpecificDetails);
            string officerDesignation = userSpecificDetails!.Designation;
            string districtCode = userSpecificDetails.DistrictCode;

            string applicationId = form["ApplicationId"].ToString();
            var applicationIdParam = new SqlParameter("@ApplicationId", applicationId);
            string action = form["Action"].ToString();
            string remarks = form["Remarks"].ToString();

            var application = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == applicationId);
            string email = application!.Email;
            var phases = JsonConvert.DeserializeObject<List<dynamic>>(application.Phase);
            string? nextOfficer = null;


            for (int i = 0; i < phases!.Count; i++)
            {
                if (phases[i].Officer == officerDesignation)
                {
                    phases[i].HasApplication = false;
                    phases[i].ActionTaken = action == "Update" ? "Forward" : action;
                    phases[i].Remarks = remarks;
                    phases[i].CanPull = action == "ReturnToEdit";

                    if (action == "Forward")
                    {
                        phases[i].CanPull = true;
                        if (i + 1 < phases.Count)
                        {
                            phases[i + 1].HasApplication = true;
                            phases[i + 1].ActionTaken = "Pending";
                            phases[i + 1].ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
                            nextOfficer = phases[i + 1].Officer;
                        }
                    }
                    else if (action == "ReturnToEdit")
                    {
                        phases[i].ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
                    }
                    else if (action == "Update")
                    {
                        string Field = form["UpdateColumn"].ToString();
                        string OldValue = "";
                        string NewValue = "";
                        string UpdateColumn = "";
                        string UpdateColumnValue = "";
                        _logger.LogInformation("----------------------------UPDATE ACTION ----------------------------");

                        var entityType = dbcontext.Model.FindEntityType(typeof(Application));
                        if (entityType!.GetProperties().Any(p => p.Name == Field))
                        {
                            _logger.LogInformation("NOT SERVICE SPECIFIC");

                            var propertyInfo = typeof(Application).GetProperty(Field);
                            OldValue = propertyInfo!.GetValue(application)?.ToString()!;
                            NewValue = form["UpdateColumnValue"].ToString();
                            UpdateColumn = Field;
                            UpdateColumnValue = NewValue;
                        }
                        else
                        {
                            _logger.LogInformation("SERVICE SPECIFIC");

                            var ServiceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
                            foreach (var item in ServiceSpecific!)
                            {

                                if (item.Name == Field)
                                {
                                    OldValue = item.Value.ToString();
                                    NewValue = form["UpdateColumnValue"].ToString();
                                    item.Value = NewValue;
                                    UpdateColumn = "ServiceSpecific";
                                    UpdateColumnValue = JsonConvert.SerializeObject(ServiceSpecific);
                                }
                            }
                        }

                        updateObject = JsonConvert.SerializeObject(new { Officer = phases[i]["Officer"], ColumnName = Field, OldValue, NewValue });

                        phases[i].CanPull = true;
                        if (i + 1 < phases.Count)
                        {
                            phases[i + 1].HasApplication = true;
                            phases[i + 1].ActionTaken = "Pending";
                            phases[i + 1].ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
                            nextOfficer = phases[i + 1].Officer;
                        }
                        helper.UpdateApplication(UpdateColumn, UpdateColumnValue, applicationIdParam);
                    }
                    else if (action == "Return" && i - 1 >= 0)
                    {
                        phases[i - 1].HasApplication = true;
                        phases[i - 1].ActionTaken = "Pending";
                        nextOfficer = phases[i - 1].Officer;
                    }
                }
            }

            string emailAction = action == "Update" ? "Forwarded" : action + "ed";
            await emailSender.SendEmail(
                email,
                "Acknowledgement",
                $"Your Application with Reference Number {applicationId} is {emailAction} by {officerDesignation}" +
                (nextOfficer != null ? $" to {nextOfficer}" : "") +
                $" at {DateTime.Now:dd MMM yyyy hh:mm tt}"
            );

            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), applicationIdParam);
            helper.UpdateApplication("EditList", form["editList"].ToString(), applicationIdParam);
            helper.UpdateApplicationHistory(applicationId, officerDesignation, action, remarks, updateObject);

            if (action == "Sanction")
            {
                Sanction(applicationId, officerDesignation);
            }

            if (action == "Sanction" || action == "Reject")
            {
                helper.UpdateApplication("ApplicationStatus", $"{action}ed", applicationIdParam);
            }

            return Json(new { status = true, url = "/Officer/Index", ApplicationId = applicationId });
        }

    }
}