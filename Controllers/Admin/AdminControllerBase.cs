using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public partial class AdminController(SocialWelfareDepartmentContext dbcontext, ILogger<AdminController> logger, UserHelperFunctions helper) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<AdminController> _logger = logger;
        protected readonly UserHelperFunctions helper = helper;


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
            var user = dbcontext.Users.FirstOrDefault(u => u.UserId == UserId);
            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(user!.UserSpecificDetails);
            int accessCode = Convert.ToInt32(userSpecificDetails!["AccessCode"]);


            var countList = GetCount();


            var AllDistrictCount = GetCount();



            return View(new { countList, AllDistrictCount });
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