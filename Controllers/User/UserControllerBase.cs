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
    public partial class UserController(SocialWelfareDepartmentContext dbcontext, ILogger<UserController> logger, UserHelperFunctions _helper, EmailSender _emailSender, PdfService pdfService, IWebHostEnvironment webHostEnvironment) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<UserController> _logger = logger;
        protected readonly UserHelperFunctions helper = _helper;
        protected readonly EmailSender emailSender = _emailSender;

        protected readonly PdfService _pdfService = pdfService;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

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
           var details = GetUserDetails();
            return View(details);
        }

        public dynamic ServicesList()
        {
            return View();
        }

        public IActionResult GetServices()
        {
            var services = dbcontext.Services.Where(u => u.Active == true).ToList();
            var columns = new List<dynamic>{
                new {title="S.No."},
                new {title="Service Name"},
                new {title="Department"},
                new {title="Action"},
            };
            List<dynamic> Services = [];
            int index = 1;
            foreach (var item in services)
            {
                var button = new{
                    function = "OpenForm",
                    parameters = new[] { item.ServiceId },
                    buttonText = "View"
                };
                List<dynamic> data = [index, item.ServiceName, item.Department, JsonConvert.SerializeObject(button)];
                Services.Add(data);
                index++;
            }

            var obj = new
            {
                columns,
                data = Services.AsEnumerable().Skip(0).Take(10),
                recordsTotal = Services.Count,
                recordsFiltered = Services.AsEnumerable().Skip(0).Take(10).ToList().Count
            };

            return Json(new { status = true, obj });
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
                var preAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PresentAddressId)).ToList();
                var perAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PermanentAddressId)).ToList();
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
            return View();
        }

        public IActionResult GetApplicationStatus(int start=0, int length=10, string type = "")
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            List<Application> applications = [];

            bool Incomplete = type == "Incomplete";

            if (type == "ApplicationStatus")
                applications = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus != "Incomplete").ToList();
            else if (type == "Incomplete")
                applications = dbcontext.Applications.Where(u => u.CitizenId == userId && u.ApplicationStatus == "Incomplete").ToList();


            var columns = new List<dynamic>{
                new{title = "S.No."},
                new{title = "Reference Number"},
                new{title = "Applicant Name"},
                new{title = "Action"},
            };
            List<dynamic> Applications = [];
            int index = 1;
            foreach (var item in applications)
            {
                var firstButton = new{
                    function = "CreateTimeLine",
                    parameters = new[] { item.ApplicationId },
                    buttonText="View"
                };
                
                 var secondButton = new{
                    function = "EditForm",
                    parameters = new[] { item.ApplicationId },
                    buttonText="Edit Form"
                };

                List<dynamic> data =
                [
                    index,
                    item.ApplicationId,
                    item.ApplicantName,
                   !Incomplete?JsonConvert.SerializeObject(firstButton):JsonConvert.SerializeObject(secondButton)
                ];

                Applications.Add(data);
                index++;
            }
            var obj = new
            {
                columns,
                data = Applications.AsEnumerable().Skip(start).Take(length),
                recordsTotal = Applications.Count,
                recordsFiltered = Applications.AsEnumerable().Skip(start).Take(length).ToList().Count,
            };

            return Json(new { status = true, obj });
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
            var preAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PresentAddressId)).ToList();
            var perAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", PermanentAddressId)).ToList();
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
        public IActionResult Acknowledgement()
        {
            var details = FetchAcknowledgementDetails(); // Use the new helper method here
            return View(details);
        }

        public IActionResult GetApplications(string serviceId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int ServiceId = Convert.ToInt32(serviceId);
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

        [HttpGet]
        public IActionResult GetFile(string? filePath)
        {
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "resources", "dummyDocs", filePath!);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            var contentType = GetContentType(fullPath);

            return File(fileBytes, contentType, Path.GetFileName(fullPath));
        }

        private static string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                { ".txt", "text/plain" },
                { ".pdf", "application/pdf" },
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" }
            };

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.TryGetValue(ext, out string? value) ? value : "application/octet-stream";
        }
    }
}