using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public partial class AdminController(SocialWelfareDepartmentContext dbcontext, ILogger<AdminController> logger) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<AdminController> _logger = logger;


        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Admin = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            ViewData["UserType"] = "Admin";
            ViewData["UserName"] = Admin!.Username;

        }

        public IActionResult Dashboard()
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            int ServiceCount = dbcontext.Services.ToList().Count;
            int OfficerCount = dbcontext.Officers.ToList().Count;
            int CitizenCount = dbcontext.Citizens.ToList().Count;
            int ApplicationCount = dbcontext.Applications.ToList().Count;
            var conditions = new Dictionary<string, string>();
            string districtCode = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)!.UserSpecificDetails)!["DistrictCode"];

            if (districtCode != null)
                conditions.Add("specific.District", districtCode);


            int TotalCount = GetCount("Total", conditions.Count != 0 ? conditions : null!);
            int PendingCount = GetCount("Pending", conditions.Count != 0 ? conditions : null!);
            int RejectCount = GetCount("Reject", conditions.Count != 0 ? conditions : null!);
            int SanctionCount = GetCount("Sanction", conditions.Count != 0 ? conditions : null!);
            int PendingWithCitizenCount = GetCount("PendingWithCitizen", conditions.Count != 0 ? conditions : null!);

            var AllDistrictCount = new
            {
                Pending = GetCount("Pending", null!),
                Rejected = GetCount("Reject", null!),
                Sanctioned = GetCount("Sanction", null!),
            };


            var countList = new
            {
                ServiceCount,
                OfficerCount,
                CitizenCount,
                ApplicationCount,
                TotalCount,
                PendingCount,
                RejectCount,
                SanctionCount,
                PendingWithCitizenCount,
                AllDistrictCount,
                districtCode
            };
            return View(countList);
        }

        public IActionResult Reports()
        {
            return View();
        }

        [HttpGet("Admin/Services/Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet("Admin/Services/Modify")]
        public IActionResult Modify()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateService([FromForm] IFormCollection form)
        {

            var service = new Service
            {
                ServiceName = form["serviceName"].ToString(),
                Department = form["departmentName"].ToString(),
            };

            dbcontext.Add(service);
            dbcontext.SaveChanges();

            return Json(new { status = true, serviceId = service.ServiceId });
        }

        [HttpPost]
        public IActionResult UpdateService([FromForm] IFormCollection form)
        {
            int serviceId = Convert.ToInt32(form["serviceId"].ToString());
            var service = dbcontext.Services.Find(serviceId);

            if (form.ContainsKey("formElements") && !string.IsNullOrEmpty(form["formElements"]))
            {
                service!.FormElement = form["formElements"].ToString();
            }
            if (form.ContainsKey("workForceOfficers") && !string.IsNullOrEmpty(form["workForceOfficers"]))
            {
                service!.WorkForceOfficers = form["workForceOfficers"].ToString();
            }

            dbcontext.SaveChanges();
            return Json(new { status = true });
        }
    }
}