using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SocialWelfare.Controllers.User
{
    public partial class UserController:Controller
    {
        [HttpPost]
        public IActionResult SetServiceForm([FromForm] IFormCollection form)
        {
            int.TryParse(form["serviceId"].ToString(), out int serviceId);
            HttpContext.Session.SetInt32("serviceId", serviceId);
            return Json(new { status = true, url = "/User/ServiceForm" });
        }

        [HttpGet]
        public dynamic? GetUserDetails(){
            int? userId = HttpContext.Session.GetInt32("UserId");
            int initiated = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Initiated").ToList().Count;
            int incomplete = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Incomplete").ToList().Count;
            int sanctioned = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Sanctioned" || u.ApplicationStatus == "Dispatched").ToList().Count;
            var userDetails = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);



            var details = new
            {
                userDetails,
                initiated,
                incomplete,
                sanctioned
            };

            return details;
        }


        [HttpGet]
        public IActionResult GetDistricts()
        {
            var districts = dbcontext.Districts.ToList();
            return Json(new { status = true, districts });
        }

        [HttpGet]
        public IActionResult GetTehsils(string districtId)
        {
            int.TryParse(districtId, out int DistrictId);
            var tehsils = dbcontext.Tehsils.Where(u => u.DistrictId == DistrictId).ToList();
            return Json(new { status = true, tehsils });
        }

        [HttpGet]
        public IActionResult GetBlocks(string districtId)
        {
            int DistrictId = Convert.ToInt32(districtId);
            var blocks = dbcontext.Blocks.Where(u => u.DistrictId == DistrictId).ToList();
            return Json(new { status = true, blocks });
        }

        [HttpGet]
        public IActionResult GetPhases(string applicationId)
        {
            int? phaseId = Convert.ToInt32(dbcontext.Applications.FirstOrDefault(app => app.ApplicationId == applicationId)!.Phase);
            var phases = new List<dynamic>();

            _logger.LogInformation($"---------- PHASE ID: {phaseId}");
            // Traverse the linked list of phases
            while (phaseId != 0)
            {
                var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == phaseId);
                if (currentPhase == null)
                    break;

                phases.Add(new
                {
                    currentPhase.ReceivedOn,
                    currentPhase.Officer,
                    currentPhase.ActionTaken,
                    currentPhase.Remarks
                });

                if(currentPhase!.ActionTaken == "Pending") break;
                // Move to the next phase
                phaseId = currentPhase.Next;
            }

            return Json(new { phase = JsonConvert.SerializeObject(phases) });
        }

        [HttpGet]
        public IActionResult GetServiceContent(){
             int? serviceId = HttpContext.Session.GetInt32("serviceId");
            var service = dbcontext.Services.FirstOrDefault(ser=>ser.ServiceId == serviceId);
            if(service!=null){
                return Json(new{status = true,service.ServiceName, service.FormElement,service.ServiceId});
            }
            else  return Json(new{status=false,message="No Service Found"});
        }


        [HttpGet]
        public IActionResult GetAcknowledgement()
        {
            var details = FetchAcknowledgementDetails();
            return Json(details);
        }

        // Private helper method to get the acknowledgement details
        private Dictionary<string, string> FetchAcknowledgementDetails()
        {
            var ApplicationId = HttpContext.Session.GetString("ApplicationId");

            // Handle the case when ApplicationId is null or empty
            if (string.IsNullOrEmpty(ApplicationId))
            {
                return new Dictionary<string, string>(); // Return an empty dictionary
            }

            var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(ApplicationId!);
            int districtCode = Convert.ToInt32(serviceSpecific["District"]);
            string AppliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.DistrictName ?? "Unknown District";

            var details = new Dictionary<string, string>
            {
                ["REFERENCE NUMBER"] = userDetails.ApplicationId,
                ["APPLICANT NAME"] = userDetails.ApplicantName,
                ["PARENTAGE"] = userDetails.RelationName + $" ({userDetails.Relation.ToUpper()})",
                ["MOTHER NAME"] = serviceSpecific["MotherName"],
                ["APPLIED DISTRICT"] = AppliedDistrict.ToUpper(),
                ["BANK NAME"] = bankDetails["BankName"],
                ["ACCOUNT NUMBER"] = bankDetails["AccountNumber"],
                ["IFSC CODE"] = bankDetails["IfscCode"],
                ["DATE OF MARRIAGE"] = serviceSpecific["DateOfMarriage"],
                ["DATE OF SUBMISSION"] = userDetails.SubmissionDate!,
                ["PRESENT ADDRESS"] = $"{preAddressDetails.Address}, TEHSIL: {preAddressDetails.Tehsil}, DISTRICT: {preAddressDetails.District}, PIN CODE: {preAddressDetails.Pincode}",
                ["PERMANENT ADDRESS"] = $"{perAddressDetails.Address}, TEHSIL: {perAddressDetails.Tehsil}, DISTRICT: {perAddressDetails.District}, PIN CODE: {perAddressDetails.Pincode}"
            };

            return details;
        }

    }
}