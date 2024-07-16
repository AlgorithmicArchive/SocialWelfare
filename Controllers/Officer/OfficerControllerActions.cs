using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

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
            if (officer == null)
            {
                return Json(new { status = false, message = "Officer not found." });
            }

            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(officer.UserSpecificDetails);
            string officerDesignation = userSpecificDetails!.Designation;
            string districtCode = userSpecificDetails.DistrictCode;

            string applicationId = form["ApplicationId"].ToString();
            var applicationIdParam = new SqlParameter("@ApplicationId", applicationId);
            string action = form["Action"].ToString();
            string remarks = form["Remarks"].ToString();

            var application = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == applicationId);
            if (application == null)
            {
                return Json(new { status = false, message = "Application not found." });
            }

            string email = application.Email;
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
                    else if (action == "Update")
                    {
                        phases[i].CanPull = true;
                        if (i + 1 < phases.Count)
                        {
                            phases[i + 1].HasApplication = true;
                            phases[i + 1].ActionTaken = "Pending";
                            phases[i + 1].ReceivedOn = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
                            nextOfficer = phases[i + 1].Officer;
                        }
                        helper.UpdateApplication(form["UpdateColumn"].ToString(), form["UpdateColumnValue"].ToString(), applicationIdParam);
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
            helper.UpdateApplicationHistory(applicationId, officerDesignation, action, remarks);

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