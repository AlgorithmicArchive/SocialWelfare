using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SendEmails;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.User
{
    [Authorize(Roles = "Citizen")]
    public partial class UserController : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext;
        protected readonly ILogger<UserController> _logger;
        protected readonly UserHelperFunctions helper;
        protected readonly EmailSender emailSender;

        protected readonly PdfService _pdfService;

        public UserController(SocialWelfareDepartmentContext dbcontext, ILogger<UserController> logger, UserHelperFunctions _helper, EmailSender _emailSender, PdfService pdfService)
        {
            this.dbcontext = dbcontext;
            _logger = logger;
            helper = _helper;
            emailSender = _emailSender;
            _pdfService = pdfService;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Citizen = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            ViewData["UserType"] = "Citizen";
            ViewData["UserName"] = Citizen!.Username;

        }

        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int initiated = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Initiated").ToList().Count;
            int incomplete = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Incomplete").ToList().Count;
            int sanctioned = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Sanctioned").ToList().Count;
            var userDetails = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);



            var details = new
            {
                userDetails,
                initiated,
                incomplete,
                sanctioned
            };

            return View(details);
        }

        public IActionResult ServicesList()
        {
            var services = dbcontext.Services.Where(u => u.Active == true).ToList();
            return View(services);
        }

        public IActionResult ServiceForm(string? ApplicationId, bool? returnToEdit)
        {
            object? ApplicationDetails = null;
            if (ApplicationId == null)
            {
                int? serviceId = HttpContext.Session.GetInt32("serviceId");
                var serviceContent = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
                ApplicationDetails = new
                {
                    serviceContent
                };
            }
            else
            {
                var generalDetails = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId);
                var PresentAddressId = generalDetails!.PresentAddressId ?? "";
                var PermanentAddressId = generalDetails.PermanentAddressId ?? "";
                var preAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PresentAddressId)).ToList();
                var perAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PermanentAddressId)).ToList();
                var serviceContent = dbcontext.Services.FirstOrDefault(u => u.ServiceId == generalDetails.ServiceId);

                if (returnToEdit != null && (bool)returnToEdit)
                {
                    ApplicationDetails = new
                    {
                        returnToEdit,
                        serviceContent,
                        generalDetails,
                        preAddressDetails,
                        perAddressDetails
                    };
                }
                else
                {
                    ApplicationDetails = new
                    {
                        serviceContent,
                        generalDetails,
                        preAddressDetails,
                        perAddressDetails
                    };
                }
            }
            return View(ApplicationDetails);
        }

        public IActionResult ApplicationStatus()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var applications = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus != "Incomplete").ToList();

            return View(applications);
        }

        public IActionResult IncompleteApplications()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var applications = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Incomplete").ToList();

            return View(applications);
        }

        public IActionResult EditForm(string ApplicationId)
        {
            var generalDetails = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId);
            var PresentAddressId = generalDetails!.PresentAddressId ?? "";
            var PermanentAddressId = generalDetails.PermanentAddressId ?? "";
            var preAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PresentAddressId)).ToList();
            var perAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PermanentAddressId)).ToList();
            var serviceContent = dbcontext.Services.FirstOrDefault(u => u.ServiceId == generalDetails.ServiceId);
            var ApplicationDetails = new
            {
                serviceContent,
                generalDetails,
                preAddressDetails,
                perAddressDetails,
            };
            return View(ApplicationDetails);
        }

        public IActionResult UpdateRequest([FromForm] IFormCollection form)
        {
            var ApplicationId = new SqlParameter("@ApplicationId", form["ApplicationId"].ToString());
            var updateRequest = form["updateRequest"].ToString();
            helper.UpdateApplication("UpdateRequest", updateRequest, ApplicationId);
            return Json(new { status = true });
        }

        [HttpGet]
        public IActionResult Acknowledgement(string? RefNo)
        {
            var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(RefNo!);

            var details = new Dictionary<string, string>
            {
                ["REFERENCE NUMBER"] = userDetails.ApplicationId,
                ["APPLICANT NAME"] = userDetails.ApplicantName,
                ["DATE OF MARRIAGE"] = serviceSpecific["DateOfMarriage"],
                ["PRESENT ADDRESS"] = preAddressDetails.Address + ",TEHSIL:" + preAddressDetails.Tehsil + ",DISTRICT:" + preAddressDetails.District + ",PIN CODE:" + preAddressDetails.Pincode,
                ["PERMANENT ADDRESS"] = perAddressDetails.Address + ",TEHSIL:" + perAddressDetails.Tehsil + ",DISTRICT:" + perAddressDetails.District + ",PIN CODE:" + perAddressDetails.Pincode,
            };
            return View(details);
        }

        public IActionResult GetApplications(string serviceId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int ServiceId = Convert.ToInt32(serviceId);
            _logger.LogInformation($"SERVICE ID: {ServiceId}");
            var applications = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ServiceId == ServiceId).ToList();

            var Ids = new List<string>();

            foreach (var application in applications)
            {
                Ids.Add(application.ApplicationId);
            }

            return Json(new { status = true, Ids });
        }
        public IActionResult GetServiceNames()
        {
            var services = dbcontext.Services.ToList();

            var ServiceList = new List<dynamic>();

            foreach (var service in services)
            {
                var obj = new
                {
                    service.ServiceId,
                    service.ServiceName
                };
                ServiceList.Add(obj);
            }

            return Json(new { status = true, ServiceList });
        }

        public IActionResult Feedback()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Feedback([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? message = form["message"].ToString();

            var UserId = new SqlParameter("@UserId", userId);
            var Message = new SqlParameter("@Message", message);
            SqlParameter ServiceRelated;
            string serviceValue = form["service"].ToString();

            if (!string.IsNullOrEmpty(serviceValue))
            {
                var obj = new
                {
                    ServiceId = Convert.ToInt32(serviceValue),
                    ApplicationId = form["ApplicationId"].ToString()
                };
                ServiceRelated = new SqlParameter("@ServiceRelated", JsonConvert.SerializeObject(obj));
            }
            else
            {
                ServiceRelated = new SqlParameter("@ServiceRelated", "{}");
            }

            dbcontext.Database.ExecuteSqlRaw("EXEC SubmitFeedback @UserId,@Message,@ServiceRelated", UserId, Message, ServiceRelated);

            return RedirectToAction("Index");
        }

    }
}