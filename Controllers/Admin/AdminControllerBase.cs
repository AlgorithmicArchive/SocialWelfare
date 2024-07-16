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
            string AdminDesignation = JsonConvert.DeserializeObject<dynamic>(Admin!.UserSpecificDetails)!["Designation"];
            ViewData["AdminType"] = AdminDesignation;
            ViewData["UserName"] = Admin!.Username;

        }

        public IActionResult Dashboard()
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            int ServiceCount = dbcontext.Services.ToList().Count;
            int OfficerCount = dbcontext.Users.Where(u => u.UserType == "Officer").ToList().Count;
            int CitizenCount = dbcontext.Users.Where(u => u.UserType == "Citizen").ToList().Count;
            int ApplicationCount = dbcontext.Applications.ToList().Count;
            var conditions = new Dictionary<string, string>();

            string districtCode = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)!.UserSpecificDetails)!["DistrictCode"];

            int? divisionCode = null;

            var user = dbcontext.Users.FirstOrDefault(u => u.UserId == UserId);
            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(user!.UserSpecificDetails);
            if (userSpecificDetails!["DivisionCode"] != null)
                divisionCode = Convert.ToInt32(userSpecificDetails["DivisionCode"]);


            var TotalCount = GetCount("Total", conditions.Count != 0 ? conditions : null!, divisionCode);
            var PendingCount = GetCount("Pending", conditions.Count != 0 ? conditions : null!, divisionCode);
            var RejectCount = GetCount("Reject", conditions.Count != 0 ? conditions : null!, divisionCode);
            var ForwardCount = GetCount("Forward", conditions.Count != 0 ? conditions : null!, divisionCode);
            var SanctionCount = GetCount("Sanction", conditions.Count != 0 ? conditions : null!, divisionCode);
            var PendingWithCitizenCount = GetCount("PendingWithCitizen", conditions.Count != 0 ? conditions : null!, divisionCode);

            var AllDistrictCount = new
            {
                Sanctioned = GetCount("Sanction", null!, null),
                Pending = GetCount("Pending", null!, null),
                PendingWithCitizen = GetCount("PendingWithCitizen", null!, null),
                Rejected = GetCount("Reject", null!, null),

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
                ForwardCount,
                SanctionCount,
                PendingWithCitizenCount,
                AllDistrictCount,
                divisionCode
            };
            return View(countList);
        }

        [HttpGet("Admin/Reports/History")]
        public IActionResult History()
        {
            return View();
        }
        [HttpGet("Admin/Reports/Individual")]
        public IActionResult Individual()
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