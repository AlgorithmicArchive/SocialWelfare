using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace SocialWelfare.Controllers.User
{
    public partial class UserController
    {
        [HttpPost]
        public IActionResult SetServiceForm([FromForm] IFormCollection form)
        {
            int.TryParse(form["serviceId"].ToString(), out int serviceId);
            HttpContext.Session.SetInt32("serviceId", serviceId);
            return Json(new { status = true, url = "/User/ServiceForm" });
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
            int.TryParse(districtId, out int DistrictId);
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

                // Move to the next phase
                phaseId = currentPhase.Next;
            }

            return Json(new { phase = JsonConvert.SerializeObject(phases) });
        }

    }
}