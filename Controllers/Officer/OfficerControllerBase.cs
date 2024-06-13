using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendEmails;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Officer
{
    [Authorize(Roles = "Officer")]
    public partial class OfficerController(SocialWelfareDepartmentContext dbcontext, ILogger<OfficerController> logger, UserHelperFunctions _helper, EmailSender _emailSender, PdfService pdfService) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<OfficerController> _logger = logger;
        protected readonly EmailSender emailSender = _emailSender;
        protected readonly UserHelperFunctions helper = _helper;
        protected readonly PdfService _pdfService = pdfService;

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

            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];
            int PendingCount = 0, ForwardCount = 0, SanctionCount = 0, RejectCount = 0, ReturnCount = 0;

            var applications = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationCountForOfficer @OfficerDesignation, @District", new SqlParameter("@OfficerDesignation", officerDesignation), new SqlParameter("@District", districtCode)).ToList();

            foreach (var application in applications)
            {
                var Phases = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(application.Phase);
                foreach (var phase in Phases!)
                {
                    if (officerDesignation == phase["Officer"])
                    {
                        switch (phase["ActionTaken"])
                        {
                            case "Pending":
                                PendingCount++;
                                break;
                            case "Forward":
                                ForwardCount++;
                                break;
                            case "Sanction":
                                SanctionCount++;
                                break;
                            case "Reject":
                                RejectCount++;
                                break;
                            case "Return":
                            case "ReturnToEdit":
                                ReturnCount++;
                                break;
                        }
                    }
                }
            }

            var countList = new
            {
                Pending = PendingCount,
                Forward = ForwardCount,
                Sanction = SanctionCount,
                Reject = RejectCount,
                Return = ReturnCount
            };

            return View(countList);
        }

        public IActionResult Applications(string? type)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            dynamic? ApplicationList = null;

            if (type!.ToString() == "Pending")
                ApplicationList = PendingApplications(Officer!);
            else if (type!.ToString() == "Sent")
                ApplicationList = SentApplications(Officer!);
            else if (type.ToString() == "Sanction")
                ApplicationList = SanctionApplications(Officer!);
            else if (type.ToString() == "Reject")
                ApplicationList = RejectApplications(Officer!);


            return View(ApplicationList);
        }
        [HttpPost]
        public IActionResult GetWorkForceOfficers([FromForm] IFormCollection form)
        {
            var ServiceId = Convert.ToInt32(form["ServiceId"].ToString());
            var Designation = HttpContext.Session.GetString("Designation");
            var WorkForceOfficers = dbcontext.Services.FirstOrDefault(u => u.ServiceId == ServiceId)!.WorkForceOfficers;

            var Officer = JsonConvert.DeserializeObject<List<dynamic>>(WorkForceOfficers!);
            var currentOfficer = "";
            foreach (var item in Officer!)
            {
                if (item["Designation"] == Designation)
                {
                    currentOfficer = JsonConvert.SerializeObject(item);
                    break;
                }
            }

            return Json(new { status = true, currentOfficer });
        }
        public IActionResult UserDetails(string? ApplicationId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];

            var generalDetails = dbcontext.Applications.Where(u => u.ApplicationId == ApplicationId).ToList()[0];
            var phases = JsonConvert.DeserializeObject<List<dynamic>>(generalDetails.Phase);


            for (int i = 0; i < phases!.Count; i++)
            {
                var currentItem = phases[i];
                var previousItem = i > 0 ? phases[i - 1] : null;
                var nextItem = i < phases.Count - 1 ? phases[i + 1] : null;

                if (currentItem["Officer"] == officerDesignation)
                {
                    if (previousItem != null && previousItem!["CanPull"] != null)
                    {
                        previousItem!["CanPull"] = false;
                    }

                    if (nextItem != null && nextItem!["CanPull"] != null)
                    {
                        nextItem!["CanPull"] = false;
                    }
                }
            }

            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), new SqlParameter("@ApplicationId", ApplicationId));


            var preAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PresentAddressId)).ToList()[0];
            var perAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PermanentAddressId)).ToList()[0];
            var serviceContent = dbcontext.Services.FirstOrDefault(u => u.ServiceId == generalDetails.ServiceId);


            var ApplicationDetails = new
            {
                currentOfficer = officerDesignation,
                serviceContent,
                generalDetails,
                preAddressDetails,
                perAddressDetails,
            };
            return View(ApplicationDetails);
        }
        public IActionResult PullApplication([FromForm] IFormCollection form)
        {
            string? ApplicationId = form["ApplicationId"].ToString();
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];
            _logger.LogInformation($"Application ID:{ApplicationId}");

            var generalDetails = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId);
            var phases = JsonConvert.DeserializeObject<List<dynamic>>(generalDetails!.Phase);

            _logger.LogInformation($"PHASES: {generalDetails.Phase}");
            for (var i = 0; i < phases!.Count; i++)
            {
                if (phases[i]["Officer"] == officerDesignation)
                {
                    _logger.LogInformation($"PHASE[i][ActionTaken] :{phases[i]["ActionTaken"]}");
                    if (phases[i]["ActionTaken"] == "Forward")
                    {
                        phases[i + 1]["HasApplication"] = false;
                        phases[i + 1]["ActionTaken"] = "";
                    }
                    else if (phases[i]["ActionTaken"] == "Return")
                    {
                        phases[i - 1]["HasApplication"] = false;
                        phases[i + 1]["ActionTaken"] = "Forward";
                    }
                    phases[i]["ActionTaken"] = "Pending";
                    phases[i]["HasApplication"] = true;
                    phases[i]["Remarks"] = "";
                    phases[i]["CanPull"] = false;
                    _logger.LogInformation($"PHASE[i][ActionTaken] :{phases[i]["ActionTaken"]}");
                    break;
                }
            }

            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), new SqlParameter("@ApplicationId", ApplicationId));


            return Json(new { status = true, PullApplication = "YES" });
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
        [HttpPost]
        public IActionResult UpdatePool([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];
            int serviceId = Convert.ToInt32(form["serviceId"].ToString());

            var poolList = JsonConvert.DeserializeObject<string[]>(form["poolIdList"].ToString());
            var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId)!.WorkForceOfficers!);

            foreach (var officer in WorkForceOfficers!)
            {
                if (officer["Designation"] == officerDesignation)
                {
                    JArray pool;
                    _logger.LogInformation($"POOL LIST LENGHT: {poolList!.Length}");
                    if (poolList!.Length > 0)
                    {
                        if (officer["Designation"] == "Director Finance")
                            pool = JArray.Parse(officer["pool"].ToString());
                        else
                            pool = JArray.Parse(officer["pool"][districtCode.ToString()].ToString());
                        foreach (string item in poolList!)
                        {
                            bool inPool = pool.Any(u => u.ToString() == item);
                            if (!inPool)
                            {
                                pool.Add(item);
                            }
                        }
                    }
                    else
                    {
                        pool = new JArray();
                    }
                    if (officer["Designation"] == "Director Finance")
                        officer["pool"] = pool;
                    else officer["pool"][districtCode.ToString()] = pool;

                }
            }
            var serviceIdParam = new SqlParameter("@ServiceId", serviceId);
            var WorkForceOfficersParam = new SqlParameter("@WorkForceOfficer", JsonConvert.SerializeObject(WorkForceOfficers));

            dbcontext.Database.ExecuteSqlRaw("EXEC UpdateWorkForceOfficer @ServiceId,@WorkForceOfficer", serviceIdParam, WorkForceOfficersParam);


            return Json(new { status = true });
        }
    }
}