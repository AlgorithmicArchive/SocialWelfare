using System.Text;
using ClosedXML.Excel;
using Encryption;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendEmails;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Officer
{
    [Authorize(Roles = "Officer")]
    public partial class OfficerController(
        SocialWelfareDepartmentContext dbcontext,
        ILogger<OfficerController> logger,
        UserHelperFunctions helper,
        EmailSender emailSender,
        PdfService pdfService,
        IWebHostEnvironment webHostEnvironment,
        IHubContext<ProgressHub> hubContext,
        IEncryptionService encryptionService
            ) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<OfficerController> _logger = logger;
        protected readonly EmailSender emailSender = emailSender;
        protected readonly UserHelperFunctions helper = helper;
        protected readonly PdfService _pdfService = pdfService;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly IHubContext<ProgressHub> hubContext = hubContext;

        protected readonly IEncryptionService encryptionService = encryptionService;

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            ViewData["UserType"] = "Officer";
            ViewData["UserName"] = Officer!.Username;
        }
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            var Services = dbcontext.Services.Where(s => s.Active == true).ToList();
            var ServiceList = new List<dynamic>();
            foreach (var service in Services)
            {
                var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(service.WorkForceOfficers!);
                foreach (var officer in WorkForceOfficers!)
                {
                    if (officer["Designation"] == officerDesignation)
                    {
                        ServiceList.Add(new { service.ServiceId, service.ServiceName });
                    }
                }
            }


            return View(ServiceList);
        }

        public async Task<IActionResult> GetApplicationsList(string ServiceId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);


            if (Officer == null)
            {
                return Json(new { status = false, message = "Officer not found." });
            }

            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer.UserSpecificDetails);
            int serviceId = Convert.ToInt32(ServiceId);
            string officerDesignation = UserSpecificDetails?["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails?["AccessLevel"]?.ToString() ?? string.Empty;
            int accessCode = Convert.ToInt32(UserSpecificDetails?["AccessCode"]?.ToString());


            var counts = await dbcontext.RecordCounts.FirstOrDefaultAsync(rc => rc.ServiceId == serviceId && rc.Officer == officerDesignation && rc.AccessCode == accessCode);
            if (counts == null)
            {
                dbcontext.RecordCounts.Add(new RecordCount
                {
                    ServiceId = serviceId,
                    Officer = officerDesignation,
                    AccessCode = accessCode,
                    Pending = 1
                });
            }

            await dbcontext.SaveChangesAsync();


            var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(
                dbcontext.Services.FirstOrDefault(service => service.ServiceId == serviceId)?.WorkForceOfficers ?? "[]"
            );
            var officersList = ((IEnumerable<dynamic>)WorkForceOfficers!).ToList();

            bool canSanction = officersList.Any(officer => officer["Designation"] == officerDesignation && (bool)officer["canSanction"]);
            bool canForward = officersList.Any(officer => officer["Designation"] == officerDesignation && (bool)officer["canForward"]);

            var countList = new
            {
                Pending = counts?.Pending ?? 0,
                Forward = counts?.Forward ?? 0,
                Sanction = counts?.Sanction ?? 0,
                Reject = counts?.Reject ?? 0,
                Return = counts?.Return ?? 0,
                CanSanction = canSanction,
                CanForward = canForward,
                ServiceId = serviceId
            };

            return Json(new { status = true, countList });
        }
        public IActionResult Applications(string? type, int start = 0, int length = 1, int serviceId = 0)
        {

            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            dynamic? ApplicationList = null;


            if (type!.ToString() == "Pending")
                ApplicationList = PendingApplications(Officer!, start, length, type, serviceId);
            else if (type.ToString() == "Pool")
                ApplicationList = PoolApplications(Officer!, start, length, type, serviceId);
            else if (type.ToString() == "Approve")
                ApplicationList = ApproveApplications(Officer!, start, length, type, serviceId);
            else if (type!.ToString() == "Sent")
                ApplicationList = SentApplications(Officer!, start, length, type, serviceId);
            else if (type.ToString() == "Sanction")
                ApplicationList = SanctionApplications(Officer!, start, length, type, serviceId);
            else if (type.ToString() == "Reject")
                ApplicationList = RejectApplications(Officer!, start, length, type, serviceId);

            return Json(new { status = true, ApplicationList });
        }

        public IActionResult UserDetails(string? ApplicationId)
        {

            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];

            var generalDetails = dbcontext.Applications.Where(u => u.ApplicationId == ApplicationId).ToList()[0];
            bool canOfficerTakeAction = true;

            CurrentPhase currentPhase, nextPhase, previousPhase;


            currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == generalDetails.ApplicationId && cur.Officer == officerDesignation)!;



            if (currentPhase.Next != 0)
            {
                nextPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase.Next)!;
                nextPhase.CanPull = false;

            }


            if (currentPhase.Previous != 0)
            {
                previousPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase.Previous)!;
                previousPhase.CanPull = false;
            }

            dbcontext.SaveChanges();

            if (IsMoreThanSpecifiedDays(currentPhase.ReceivedOn.ToString(), 15)) canOfficerTakeAction = false;
            if (IsMoreThanSpecifiedDays(generalDetails.SubmissionDate!.ToString(), 45)) canOfficerTakeAction = false;


            var preAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PresentAddressId)).ToList()[0];
            var perAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PermanentAddressId)).ToList()[0];
            var serviceContent = dbcontext.Services.FirstOrDefault(u => u.ServiceId == generalDetails.ServiceId);

            var applicationHistory = JsonConvert.DeserializeObject<dynamic>(dbcontext.ApplicationsHistories.FirstOrDefault(his => his.ApplicationId == ApplicationId)!.History);
            List<dynamic> histories = [];
            foreach (var history in applicationHistory!)
            {
                bool isTransfered = history["ActionTaken"].ToString().Contains("Transfered");
                if (!isTransfered) histories.Add(history);
            }
            string updateObject = "";
            foreach (var item in applicationHistory!)
            {
                if (item["ActionTaken"] == "Update")
                {
                    updateObject = JsonConvert.SerializeObject(item["UpdateObject"]);
                }
            }

            var ApplicationDetails = new
            {
                currentOfficer = officerDesignation,
                serviceContent,
                generalDetails,
                preAddressDetails,
                perAddressDetails,
                canOfficerTakeAction,
                previousActions = histories,
                updateObject,
            };
            return View(ApplicationDetails);
        }

        public IActionResult SendBankFile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            var Services = dbcontext.Services.Where(s => s.Active == true).ToList();
            var ServiceList = new List<dynamic>();
            var Districts = dbcontext.Districts.ToList();
            foreach (var service in Services)
            {
                var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(service.WorkForceOfficers!);
                foreach (var officer in WorkForceOfficers!)
                {
                    if (officer["Designation"] == officerDesignation)
                    {
                        ServiceList.Add(new { service.ServiceId, service.ServiceName });
                    }
                }
            }


            return View(new { ServiceList, Districts });
        }

        public IActionResult GetResponseFile()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            var Services = dbcontext.Services.Where(s => s.Active == true).ToList();
            var ServiceList = new List<dynamic>();
            var Districts = dbcontext.Districts.ToList();
            foreach (var service in Services)
            {
                var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(service.WorkForceOfficers!);
                foreach (var officer in WorkForceOfficers!)
                {
                    if (officer["Designation"] == officerDesignation)
                    {
                        ServiceList.Add(new { service.ServiceId, service.ServiceName });
                    }
                }
            }


            return View(new { ServiceList, Districts });
        }
        public IActionResult Reports()
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            string Officer = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)!.UserSpecificDetails)!["Designation"];
            var officerDetails = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)!.UserSpecificDetails);
            string designation = officerDetails!["Designation"];
            int districtCode = Convert.ToInt32(officerDetails["AccessCode"]);

            var AllDistrictCount = GetCount();
            var countList = GetCount(designation, districtCode);
            return View(new { countList, AllDistrictCount, districtCode, designation });
        }

        public IActionResult UpdateRequests()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];

            var list = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationUpdateRequestForOfficer @OfficerDesignation,@District", new SqlParameter("@OfficerDesignation", officerDesignation), new SqlParameter("@District", districtCode.ToString())).ToList();

            return View(list);
        }
        [HttpPost]
        public IActionResult UpdateRequests([FromForm] IFormCollection form)
        {
            string? ApplicationId = form["ApplicationId"].ToString();
            var updateRequest = JsonConvert.DeserializeObject<dynamic>(form["updateRequest"].ToString());
            var application = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId);
            string column = updateRequest!["column"].ToString();
            string newValue = "";
            var isFormSpecific = updateRequest!["formElement"]["isFormSpecific"];
            if (isFormSpecific == "True")
            {
                string name = updateRequest["formElement"]["name"];
                string value = updateRequest["newValue"];
                var serviceSpecific = JObject.Parse(application!.ServiceSpecific);
                serviceSpecific[name] = value;
                newValue = JsonConvert.SerializeObject(serviceSpecific);
            }
            else
            {
                newValue = updateRequest["newValue"];
            }

            helper.UpdateApplication(column, newValue, new SqlParameter("@ApplicationId", ApplicationId));
            updateRequest["updated"] = 1;
            helper.UpdateApplication("UpdateRequest", JsonConvert.SerializeObject(updateRequest), new SqlParameter("@ApplicationId", ApplicationId));

            return Json(new { status = true });
        }
        [HttpGet]
        public IActionResult RegisterDSC()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var alreadyCertificate = dbcontext.Certificates.FirstOrDefault(cer => cer.OfficerId == userId);
            return View(alreadyCertificate);
        }

    }
}