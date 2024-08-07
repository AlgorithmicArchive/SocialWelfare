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

            string file = await helper.GetFilePath(form.Files["ForwardFile"]);



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
                        phases[i].CanPull = phases[i + 1].Officer != "Director Finance";
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
                        string UpdateColumnFile = await helper.GetFilePath(form.Files["UpdateColumnFile"]);

                        var entityType = dbcontext.Model.FindEntityType(typeof(Application));
                        if (entityType!.GetProperties().Any(p => p.Name == Field))
                        {
                            var propertyInfo = typeof(Application).GetProperty(Field);
                            OldValue = propertyInfo!.GetValue(application)?.ToString()!;
                            NewValue = form["UpdateColumnValue"].ToString();
                            UpdateColumn = Field;
                            UpdateColumnValue = NewValue;
                        }
                        else
                        {
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

                        string desigShort = dbcontext.OfficersDesignations.FirstOrDefault(of => of.Designation == officerDesignation)!.DesignationShort;
                        updateObject = JsonConvert.SerializeObject(new { Officer = desigShort, ColumnName = Field, OldValue, NewValue, File = UpdateColumnFile });

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
                    else if (action == "Sanction")
                    {
                        phases[i].CanPull = true;
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
            helper.UpdateApplicationHistory(applicationId, officerDesignation, action, remarks, updateObject, file);

            if (action == "Sanction")
            {
                var newLetterUpdateDetails = JsonConvert.DeserializeObject<dynamic>(form["letterUpdateDetails"].ToString());

                if (newLetterUpdateDetails != null)
                {
                    var letterUpdateDetails = dbcontext.UpdatedLetterDetails.FirstOrDefault(up => up.ApplicationId == applicationId);

                    if (letterUpdateDetails != null)
                    {
                        var existingLetterUpdateDetails = JsonConvert.DeserializeObject<List<dynamic>>(letterUpdateDetails!.UpdatedDetails.ToString());
                        existingLetterUpdateDetails!.Add(newLetterUpdateDetails);
                        letterUpdateDetails.UpdatedDetails = JsonConvert.SerializeObject(existingLetterUpdateDetails);
                        dbcontext.UpdatedLetterDetails.Update(letterUpdateDetails);
                        dbcontext.SaveChanges();
                    }
                    else
                    {
                        letterUpdateDetails = new UpdatedLetterDetail
                        {
                            ApplicationId = applicationId,
                            UpdatedDetails = JsonConvert.SerializeObject(new List<dynamic> { newLetterUpdateDetails })
                        };

                        dbcontext.UpdatedLetterDetails.Add(letterUpdateDetails);
                        dbcontext.SaveChanges();  // Ensure to save the changes here as well
                    }
                }

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