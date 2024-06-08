using Microsoft.AspNetCore.Mvc;
using SocialWelfare.Models.Entities;

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
    }
}