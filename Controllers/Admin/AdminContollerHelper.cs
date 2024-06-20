using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Admin
{
    public partial class AdminController
    {
        public List<Application> GetCount(string type, Dictionary<string, string> conditions, int? divisionCode)
        {
            StringBuilder Condition1 = new StringBuilder();
            StringBuilder Condition2 = new StringBuilder();

            if (type == "Pending")
                Condition1.Append("AND application.ApplicationStatus='Initiated'");
            else if (type == "Sanction")
                Condition1.Append("AND application.ApplicationStatus='Sanctioned'");
            else if (type == "Reject")
                Condition1.Append("AND application.ApplicationStatus='Rejected'");
            else if (type == "PendingWithCitizen")
                Condition1.Append("AND Application.ApplicationStatus='Initiated' AND JSON_VALUE(app.value, '$.ActionTaken')='ReturnToEdit'");

            int conditionCount = 0;
            int splitPoint = conditions != null ? conditions.Count / 2 : 0;

            if (conditions != null && conditions.Count != 0)
            {
                foreach (var condition in conditions)
                {
                    if (conditionCount < splitPoint)
                        Condition1.Append($" AND {condition.Key}='{condition.Value}'");
                    else
                        Condition2.Append($" AND {condition.Key}='{condition.Value}'");

                    conditionCount++;
                }

            }

            if (conditions != null && conditions.ContainsKey("JSON_VALUE(app.value, '$.Officer')") && type != "Total")
            {
                Condition2.Append($" AND JSON_VALUE(app.value, '$.ActionTaken') = '{type}'");
            }
            else if (type == "Total")
            {
                Condition2.Append($" AND JSON_VALUE(app.value, '$.ActionTaken') != ''");
            }

            var applications = dbcontext.Applications.FromSqlRaw("EXEC GetApplications @Condition1, @Condition2",
            new SqlParameter("@Condition1", Condition1.ToString()),
           new SqlParameter("@Condition2", Condition2.ToString())).ToList();

            _logger.LogInformation($"HELPER DIVISION CODE: {divisionCode}");

            if (divisionCode == null) return applications;
            else
            {

                var filteredApplications = new List<Application>();
                foreach (var application in applications)
                {
                    var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
                    int district = Convert.ToInt32(serviceSpecific!["District"]);
                    int division = dbcontext.Districts.FirstOrDefault(d => d.Uuid == district)!.Division;
                    _logger.LogInformation($"DIVISION FROM DATABASE: {division}");
                    if (division == divisionCode) filteredApplications.Add(application);
                }
                return filteredApplications;

            }

        }

        public IActionResult GetFilteredCount(string? conditions)
        {
            var Conditions = JsonConvert.DeserializeObject<Dictionary<string, string>>(conditions!);
            var TotalCount = GetCount("Total", Conditions!, null);
            var PendingCount = GetCount("Pending", Conditions!, null);
            var RejectCount = GetCount("Reject", Conditions!, null);
            var SanctionCount = GetCount("Sanction", Conditions!, null);

            return Json(new { status = true, TotalCount, PendingCount, RejectCount, SanctionCount });
        }

        [HttpGet]
        public IActionResult GetDistricts(string? division)
        {
            var districts = dbcontext.Districts.AsQueryable();

            if (!string.IsNullOrEmpty(division) && division != "null")
            {
                int divisionCode;
                if (int.TryParse(division, out divisionCode))
                {
                    districts = districts.Where(d => d.Division == divisionCode);
                }
                else
                {
                    return Json(new { status = false, message = "Invalid division code" });
                }
            }

            var districtList = districts.ToList();
            return Json(new { status = true, districts = districtList });
        }



        [HttpGet]
        public IActionResult GetDesignations()
        {
            var designations = dbcontext.OfficersDesignations.ToList();
            return Json(new { status = true, designations });
        }

        [HttpGet]
        public IActionResult GetServices()
        {
            var services = dbcontext.Services.ToList();
            return Json(new { status = true, services });
        }

        [HttpGet]
        public IActionResult GetTeshilForDistrict(string districtId)
        {
            int DistrictId = Convert.ToInt32(districtId);
            var tehsils = dbcontext.Tehsils.Where(u => u.DistrictId == DistrictId).ToList();
            return Json(new { status = true, tehsils });
        }

        [HttpGet]
        public IActionResult GetBlockForDistrict(string districtId)
        {
            int DistrictId = Convert.ToInt32(districtId);
            var blocks = dbcontext.Blocks.Where(u => u.DistrictId == DistrictId).ToList();
            return Json(new { status = true, blocks });
        }

    }
}