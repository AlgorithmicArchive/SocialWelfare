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
    public partial class OfficerController(SocialWelfareDepartmentContext dbcontext, ILogger<OfficerController> logger, UserHelperFunctions _helper, EmailSender _emailSender, PdfService pdfService, IWebHostEnvironment webHostEnvironment) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<OfficerController> _logger = logger;
        protected readonly EmailSender emailSender = _emailSender;
        protected readonly UserHelperFunctions helper = _helper;
        protected readonly PdfService _pdfService = pdfService;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;


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
            bool canSanction = true;
            bool canForward = true;
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);

            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails["AccessLevel"]?.ToString() ?? string.Empty;

            SqlParameter AccessLevelCode = new("@AccessLevelCode", DBNull.Value);
            switch (accessLevel)
            {
                case "Tehsil":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails["TehsilCode"]?.ToString() ?? string.Empty);
                    break;
                case "District":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails["DistrictCode"]?.ToString() ?? string.Empty);
                    break;
                case "Division":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails["DivisionCode"]?.ToString() ?? string.Empty);
                    break;
            }

            int PendingCount = 0, ForwardCount = 0, SanctionCount = 0, RejectCount = 0, ReturnCount = 0;

            var applications = dbcontext.Applications.FromSqlRaw(
                "EXEC GetApplicationCountForOfficer @OfficerDesignation, @AccessLevel, @AccessLevelCode",
                new SqlParameter("@OfficerDesignation", officerDesignation),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode
            ).ToList();

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

            var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(dbcontext.Services.FirstOrDefault(service => service.ServiceId == 1)!.WorkForceOfficers!);
            foreach (var officer in WorkForceOfficers!)
            {
                if (officer["Designation"] == officerDesignation)
                {
                    canSanction = officer["canSanction"];
                    canForward = officer["canForward"];
                }
            }
            var countList = new
            {
                Pending = PendingCount,
                Forward = ForwardCount,
                Sanction = SanctionCount,
                Reject = RejectCount,
                Return = ReturnCount,
                CanSanction = canSanction,
                CanForward = canForward
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


            return Json(new { status = true, ApplicationList });
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
            bool canOfficerTakeAction = true;
            var previousActions = new List<dynamic>();

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
                    if (IsMoreThanSpecifiedDays(currentItem["ReceivedOn"].ToString(), 15)) canOfficerTakeAction = false;

                    break;
                }
                else
                {
                    var obj = new
                    {
                        Officer = phases[i]["Officer"],
                        ActionTaken = phases[i]["ActionTaken"],
                        Remarks = phases[i]["Remarks"]
                    };
                    previousActions.Add(obj);
                }
            }

            if (IsMoreThanSpecifiedDays(generalDetails.SubmissionDate!.ToString(), 45)) canOfficerTakeAction = false;


            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), new SqlParameter("@ApplicationId", ApplicationId));


            var preAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PresentAddressId)).ToList()[0];
            var perAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PermanentAddressId)).ToList()[0];
            var serviceContent = dbcontext.Services.FirstOrDefault(u => u.ServiceId == generalDetails.ServiceId);

            var applicationHistory = JsonConvert.DeserializeObject<dynamic>(dbcontext.ApplicationsHistories.FirstOrDefault(his => his.ApplicationId == ApplicationId)!.History);
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
                previousActions,
                updateObject,
            };
            return View(ApplicationDetails);
        }
        public IActionResult Reports()
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            string Officer = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)!.UserSpecificDetails)!["Designation"];
            int ServiceCount = dbcontext.Services.ToList().Count;
            int OfficerCount = dbcontext.Users.Where(u => u.UserType == "Officer").ToList().Count;
            int CitizenCount = dbcontext.Users.Where(u => u.UserType == "Citizen").ToList().Count;
            int ApplicationCount = dbcontext.Applications.ToList().Count;
            var conditions = new Dictionary<string, string>();

            var officerDetails = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)!.UserSpecificDetails);
            string districtCode = officerDetails!["DistrictCode"];
            string designation = officerDetails["Designation"];
            conditions.Add("JSON_VALUE(a.ServiceSpecific, '$.District')", districtCode);
            conditions.Add("JSON_VALUE(app.value, '$.Officer')", designation);

            var TotalCount = GetCount("Total", conditions.Count != 0 ? conditions : null!);
            var PendingCount = GetCount("Pending", conditions.Count != 0 ? conditions : null!);
            var RejectCount = GetCount("Reject", conditions.Count != 0 ? conditions : null!);
            var SanctionCount = GetCount("Sanction", conditions.Count != 0 ? conditions : null!);
            var PendingWithCitizenCount = GetCount("PendingWithCitizen", conditions.Count != 0 ? conditions : null!);

            var AllDistrictCount = new
            {
                Sanctioned = GetCount("Sanction", null!),
                Pending = GetCount("Pending", null!),
                PendingWithCitizen = GetCount("PendingWithCitizen", null!),
                Rejected = GetCount("Reject", null!),
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
                districtCode,
                Officer,
            };
            return View(countList);
        }
        public IActionResult PullApplication([FromForm] IFormCollection form)
        {
            string? ApplicationId = form["ApplicationId"].ToString();
            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];
            string otherOfficer = "";

            var generalDetails = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId);
            var phases = JsonConvert.DeserializeObject<List<dynamic>>(generalDetails!.Phase);

            for (var i = 0; i < phases!.Count; i++)
            {
                if (phases[i]["Officer"] == officerDesignation)
                {
                    if (phases[i]["ActionTaken"] == "Forward")
                    {
                        otherOfficer = phases[i + 1]["Officer"];
                        phases[i + 1]["HasApplication"] = false;
                        phases[i + 1]["ActionTaken"] = "";
                    }
                    else if (phases[i]["ActionTaken"] == "Return")
                    {
                        otherOfficer = phases[i - 1]["Officer"];
                        phases[i - 1]["HasApplication"] = false;
                        phases[i + 1]["ActionTaken"] = "Forward";
                    }
                    else if (phases[i]["ActionTaken"] == "Sanction")
                    {
                        helper.UpdateApplication("ApplicationStatus", "Initiated", new SqlParameter("@ApplicationId", ApplicationId));
                        string sourceFile = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "SanctionLetter.pdf");
                        string destinationFile = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "BAK" + DateTime.Now.ToString("dd MMM yyyy hh:mm tt") + "SanctionLetter.pdf");
                        if (System.IO.File.Exists(sourceFile))
                            System.IO.File.Move(sourceFile, destinationFile);
                    }
                    phases[i]["ActionTaken"] = "Pending";
                    phases[i]["HasApplication"] = true;
                    phases[i]["Remarks"] = "";
                    phases[i]["CanPull"] = false;
                    break;
                }
            }

            helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), new SqlParameter("@ApplicationId", ApplicationId));
            helper.UpdateApplicationHistory(ApplicationId, officerDesignation, "Pulled Back From " + otherOfficer == "" ? "Sanctioned Phase" : otherOfficer, "");
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
            string poolAction = form["poolAction"].ToString();
            var poolList = JsonConvert.DeserializeObject<string[]>(form["poolIdList"].ToString());
            var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId)!.WorkForceOfficers!);


            foreach (var item in poolList!)
            {
                var generalDetails = dbcontext.Applications.Where(u => u.ApplicationId == item).ToList()[0];
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
                helper.UpdateApplication("Phase", JsonConvert.SerializeObject(phases), new SqlParameter("@ApplicationId", item));
            }


            foreach (var officer in WorkForceOfficers!)
            {
                if (officer["Designation"] == officerDesignation)
                {
                    JArray pool;
                    if (poolList!.Length > 0)
                    {
                        if (officer["Designation"] == "Director Finance")
                            pool = JArray.Parse(officer["pool"].ToString());
                        else
                            pool = JArray.Parse(officer["pool"][districtCode.ToString()].ToString());
                        foreach (string item in poolList!)
                        {
                            bool inPool = pool.Any(u => u.ToString() == item);
                            if (!inPool && poolAction == "add")
                            {
                                pool.Add(item);
                            }
                            else if (inPool && poolAction == "remove")
                            {
                                var itemsToRemove = pool.Where(u => u.ToString() == item).ToList();
                                foreach (var itemToRemove in itemsToRemove)
                                {
                                    pool.Remove(itemToRemove);
                                }
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