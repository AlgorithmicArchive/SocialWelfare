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
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];

            string? ApplicationId = form["ApplicationId"].ToString();

            string? Action = form["Action"].ToString();
            string? Remarks = form["Remarks"].ToString();

            string email = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId)!.Email;


            var phases = JsonConvert.DeserializeObject<List<dynamic>>(dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId)!.Phase);

            string? nextOfficer = "";

            for (var i = 0; i < phases!.Count; i++)
            {
                if (phases[i]["Officer"] == officerDesignation)
                {
                    if (Action == "Forward")
                    {
                        phases[i]["HasApplication"] = false;
                        phases[i]["ActionTaken"] = Action;
                        phases[i]["Remarks"] = Remarks;
                        phases[i]["CanPull"] = true;
                        phases[i + 1]["HasApplication"] = true;
                        phases[i + 1]["ActionTaken"] = "Pending";
                        phases[i + 1]["ReceivedOn"] = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
                        nextOfficer = phases[i + 1]["Officer"];
                    }
                    else if (Action == "Return")
                    {
                        phases[i]["HasApplication"] = false;
                        phases[i]["ActionTaken"] = Action;
                        phases[i]["Remarks"] = Remarks;
                        phases[i]["CanPull"] = false;
                        phases[i - 1]["HasApplication"] = true;
                        phases[i - 1]["ActionTaken"] = "Pending";
                        nextOfficer = phases[i - 1]["Officer"];

                    }
                    else if (Action == "Reject" || Action == "Sanction" || Action == "ReturnToEdit")
                    {
                        phases[i]["HasApplication"] = false;
                        phases[i]["ActionTaken"] = Action;
                        phases[i]["Remarks"] = Remarks;
                        phases[i]["CanPull"] = Action == "ReturnToEdit";
                    }
                }
            }


            await emailSender.SendEmail(
                email,
                "Acknowledgement",
                "Your Application with Reference Number " + ApplicationId + " is " + Action + " by " + officerDesignation +
                (nextOfficer != "" ? " to " + nextOfficer : "") +
                " at " + DateTime.Now.ToString("dd MMM yyyy hh:mm tt")
            );



            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), new SqlParameter("@ApplicationId", ApplicationId));

            helper.UpdateApplication("EditList", form["editList"].ToString(), new SqlParameter("@ApplicationId", ApplicationId));

            if (Action == "Sanction")
                Sanction(ApplicationId, officerDesignation);
            if (Action == "Sanction" || Action == "Reject")
                helper.UpdateApplication("ApplicationStatus", Action + "ed", new SqlParameter("@ApplicationId", ApplicationId));

            return Json(new { status = true, url = "/Officer/Index", ApplicationId });
        }
    }
}