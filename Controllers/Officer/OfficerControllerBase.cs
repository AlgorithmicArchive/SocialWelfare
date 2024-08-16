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
using Renci.SshNet;
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

        public IActionResult GetApplicationsList(string ServiceId)
        {

            int? userId = HttpContext.Session.GetInt32("UserId");
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            bool canSanction = true;
            bool canForward = true;
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            int serviceId = Convert.ToInt32(ServiceId);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = "";
            SqlParameter AccessLevelCode = new("@AccessLevelCode", DBNull.Value);
            switch (accessLevel)
            {
                case "Tehsil":
                    accessCode = UserSpecificDetails["TehsilCode"]?.ToString()!;
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails["TehsilCode"]?.ToString() ?? string.Empty);
                    break;
                case "District":
                    accessCode = UserSpecificDetails["DistrictCode"]?.ToString()!;
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails["DistrictCode"]?.ToString() ?? string.Empty);
                    break;
                case "Division":
                    accessCode = UserSpecificDetails["DivisionCode"]?.ToString()!;
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails["DivisionCode"]?.ToString() ?? string.Empty);
                    break;
            }


            int PendingCount = dbcontext.CurrentPhases
            .Join(dbcontext.Applications,
                cp => cp.ApplicationId,
                a => a.ApplicationId,
                (cp, a) => new { CurrentPhase = cp, Application = a })
            .Where(x => x.Application.ServiceId == serviceId && x.CurrentPhase.ActionTaken == "Pending" && x.CurrentPhase.Officer == officerDesignation)
            .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
            .Where(x =>
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                return locationCode == accessCode;
            })
            .Select(x => x.CurrentPhase)
            .ToList().Count;

            int ForwardCount = dbcontext.CurrentPhases
           .Join(dbcontext.Applications,
               cp => cp.ApplicationId,
               a => a.ApplicationId,
               (cp, a) => new { CurrentPhase = cp, Application = a })
           .Where(x => x.Application.ServiceId == serviceId && x.CurrentPhase.ActionTaken == "Forward" && x.CurrentPhase.Officer == officerDesignation)
           .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
            .Where(x =>
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                return locationCode == accessCode;
            })
            .Select(x => x.CurrentPhase)
            .ToList().Count;

            int SanctionCount = dbcontext.CurrentPhases
           .Join(dbcontext.Applications,
               cp => cp.ApplicationId,
               a => a.ApplicationId,
               (cp, a) => new { CurrentPhase = cp, Application = a })
           .Where(x => x.Application.ServiceId == serviceId && x.CurrentPhase.ActionTaken == "Sanction" && x.CurrentPhase.Officer == officerDesignation)
           .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
            .Where(x =>
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                return locationCode == accessCode;
            })
            .Select(x => x.CurrentPhase)
            .ToList().Count;

            int RejectCount = dbcontext.CurrentPhases
            .Join(dbcontext.Applications,
                cp => cp.ApplicationId,
                a => a.ApplicationId,
                (cp, a) => new { CurrentPhase = cp, Application = a })
            .Where(x => x.Application.ServiceId == serviceId && x.CurrentPhase.ActionTaken == "Reject" && x.CurrentPhase.Officer == officerDesignation)
           .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
            .Where(x =>
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                return locationCode == accessCode;
            })
            .Select(x => x.CurrentPhase)
            .ToList().Count;

            int ReturnCount = dbcontext.CurrentPhases
           .Join(dbcontext.Applications,
               cp => cp.ApplicationId,
               a => a.ApplicationId,
               (cp, a) => new { CurrentPhase = cp, Application = a })
           .Where(x => x.Application.ServiceId == serviceId && x.CurrentPhase.ActionTaken == "Return" && x.CurrentPhase.Officer == officerDesignation)
           .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
            .Where(x =>
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                return locationCode == accessCode;
            })
            .Select(x => x.CurrentPhase)
            .ToList().Count;



            var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(dbcontext.Services.FirstOrDefault(service => service.ServiceId == serviceId)!.WorkForceOfficers!);



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
                CanForward = canForward,
                serviceId
            };


            return Json(new { status = true, countList });
        }
        public IActionResult Applications(string? type, int start = 0, int length = 1, int serviceId = 0)
        {

            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            dynamic? ApplicationList = null;

            if (type!.ToString() == "Pending" || type.ToString() == "Approve" || type.ToString() == "Pool")
                ApplicationList = PendingApplications(Officer!, start, length, type, serviceId);
            else if (type!.ToString() == "Sent")
                ApplicationList = SentApplications(Officer!, start, length, type, serviceId);
            else if (type.ToString() == "Sanction")
                ApplicationList = SanctionApplications(Officer!, start, length, type, serviceId);
            else if (type.ToString() == "Reject")
                ApplicationList = RejectApplications(Officer!, start, length, type, serviceId);

            return Json(new { status = true, ApplicationList });
        }

        [HttpGet]
        public IActionResult DownloadAllData(string? type, string? activeButtons)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;

            try
            {
                List<string> ActiveButtons = [];

                if (!string.IsNullOrEmpty(activeButtons))
                {
                    ActiveButtons = JsonConvert.DeserializeObject<List<string>>(Uri.UnescapeDataString(activeButtons!))!;
                }

                dynamic? ApplicationList;
                switch (type)
                {
                    case "Pending":
                    case "Approve":
                    case "Pool":
                        ApplicationList = PendingApplications(Officer, 0, 0, type, 1, true);
                        break;
                    case "Sent":
                        ApplicationList = SentApplications(Officer, 0, 0, type, 1, true);
                        break;
                    case "Sanction":
                        ApplicationList = SanctionApplications(Officer, 0, 0, type, 1, true);
                        break;
                    case "Reject":
                        ApplicationList = RejectApplications(Officer, 0, 0, type, 1, true);
                        break;
                    default:
                        _logger.LogError("Invalid application type: {Type}", type);
                        return BadRequest("Invalid application type.");
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Applications");
                var currentRow = 1;

                _logger.LogInformation($"-------------ACTIVE BUTOONS: {activeButtons}-------------------");

                int currentColumn = 1;
                for (int i = 0; i < ApplicationList.columns.Count; i++)
                {
                    string header = ApplicationList.columns[i].title.ToString();

                    if (ActiveButtons.Count == 0 || ActiveButtons.Contains(header))
                    {
                        worksheet.Cell(currentRow, currentColumn).Value = header;
                        currentColumn++;
                    }
                }

                // Adding Data
                foreach (var application in ApplicationList.data)
                {
                    currentRow++;
                    currentColumn = 1; // Reset column for each row
                    for (int i = 0; i < application.Count; i++)
                    {
                        string cellValue = application[i]?.ToString()!;
                        if (ActiveButtons.Count == 0 || ActiveButtons.Contains(ApplicationList.columns[i].title.ToString()))
                        {
                            worksheet.Cell(currentRow, currentColumn).Value = cellValue;
                            currentColumn++;
                        }
                    }
                }

                var fileName = DateTime.Now.ToString("dd MMM yyyy hh:mm tt") + "_Applications.xlsx";
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "exports", fileName);

                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                // Save the workbook
                workbook.SaveAs(filePath);

                return Json(new { filePath = "/exports/" + fileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while generating the Excel file.");
                return StatusCode(500, "Internal server error");
            }
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
            bool canOfficerTakeAction = true;

            CurrentPhase currentPhase, nextPhase, previousPhase;


            currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == generalDetails.ApplicationId && cur.Officer == officerDesignation)!;

            _logger.LogInformation($"=========NEXT:{currentPhase.Next}  PREVIOUS:{currentPhase.Previous}-=================================");


            if (currentPhase.Next != 0)
            {
                nextPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase.Next)!;
                nextPhase.CanPull = false;

            }


            if (currentPhase.Previous != 0)
            {
                previousPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase.Previous)!;
                _logger.LogInformation($"===============CAN PULL:{previousPhase.CanPull}==============================");
                previousPhase.CanPull = false;
                _logger.LogInformation($"===============CAN PULL:{previousPhase.CanPull}==============================");

            }

            dbcontext.SaveChanges();

            if (IsMoreThanSpecifiedDays(currentPhase.ReceivedOn.ToString(), 15)) canOfficerTakeAction = false;
            if (IsMoreThanSpecifiedDays(generalDetails.SubmissionDate!.ToString(), 45)) canOfficerTakeAction = false;


            var preAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PresentAddressId)).ToList()[0];
            var perAddressDetails = dbcontext.AddressJoins.FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PermanentAddressId)).ToList()[0];
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
            var generalDetails = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId);
            var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == ApplicationId && cur.Officer == officerDesignation);
            CurrentPhase? otherPhase = null;
            string ActionTaken = currentPhase!.ActionTaken;


            currentPhase.ActionTaken = "Pending";
            currentPhase.Remarks = "";
            currentPhase.CanPull = false;
            if (ActionTaken != "Sanction")
            {
                otherPhase = dbcontext.CurrentPhases.FirstOrDefault(curr => curr.ApplicationId == ApplicationId && curr.PhaseId == (ActionTaken == "Forward" ? currentPhase.Next : currentPhase.Previous))!;
                otherPhase!.ActionTaken = ActionTaken == "Forward" ? "" : "Forward";
            }

            dbcontext.SaveChanges();

            if (ActionTaken == "Sanction")
            {
                helper.UpdateApplication("ApplicationStatus", "Initiated", new SqlParameter("@ApplicationId", ApplicationId));
                string sourceFile = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "SanctionLetter.pdf");
                string destinationFile = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "BAK" + DateTime.Now.ToString("dd MMM yyyy hh:mm tt") + "SanctionLetter.pdf");
                if (System.IO.File.Exists(sourceFile))
                    System.IO.File.Move(sourceFile, destinationFile);
            }

            helper.UpdateApplicationHistory(ApplicationId, officerDesignation, "Pulled Back From " + otherPhase == null ? "Sanction Phase" : otherPhase!.Officer, "");
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
            string action = form["action"].ToString();
            var IdList = JsonConvert.DeserializeObject<string[]>(form["IdList"].ToString());
            var listType = form["listType"].ToString();
            var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId)!.WorkForceOfficers!);

            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = "0";

            SqlParameter AccessLevelCode = new SqlParameter("@AccessLevelCode", DBNull.Value);

            switch (accessLevel)
            {
                case "Tehsil":
                    accessCode = UserSpecificDetails!["TehsilCode"].ToString();
                    break;
                case "District":
                    accessCode = UserSpecificDetails!["DistrictCode"].ToString();
                    break;
                case "Division":
                    accessCode = UserSpecificDetails!["DivisionCode"].ToString();
                    break;
            }

            foreach (var item in IdList!)
            {
                var generalDetails = dbcontext.Applications.Where(u => u.ApplicationId == item).ToList()[0];

                var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(curr => curr.ApplicationId == item && curr.Officer == officerDesignation);
                if (currentPhase!.Next != 0)
                {
                    var previousPhase = dbcontext.CurrentPhases.FirstOrDefault(curr => curr.PhaseId == currentPhase.Next);
                    previousPhase!.CanPull = false;
                }
                if (currentPhase!.Previous != 0)
                {
                    var nextPhase = dbcontext.CurrentPhases.FirstOrDefault(curr => curr.PhaseId == currentPhase.Previous);
                    nextPhase!.CanPull = false;
                }

            }
            dbcontext.SaveChanges();

            var arrayLists = dbcontext.ApplicationLists.FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));

            var PoolList = JsonConvert.DeserializeObject<List<string>>(arrayLists!.PoolList);
            var ApprovalList = JsonConvert.DeserializeObject<List<string>>(arrayLists!.ApprovalList);
            string actionTaken = "";

            foreach (var item in IdList)
            {
                if (listType == "Approve")
                {
                    if (action == "add" && !ApprovalList!.Contains(item)) { ApprovalList.Add(item); actionTaken = "Transfered to Appove List From Inbox"; }
                    else if (action == "remove" && ApprovalList!.Contains(item)) { ApprovalList.Remove(item); actionTaken = "Transfered to Inbox From Approve List"; }

                    arrayLists.ApprovalList = JsonConvert.SerializeObject(ApprovalList);

                }
                else if (listType == "Pool")
                {
                    if (action == "add" && !PoolList!.Contains(item)) { PoolList.Add(item); actionTaken = "Transfered to Pool List From Approve List"; }
                    else if (action == "remove" && PoolList!.Contains(item)) { PoolList.Remove(item); actionTaken = "Transfered to Inbox From Pool List"; }

                    arrayLists.PoolList = JsonConvert.SerializeObject(PoolList);
                }
                else if (listType == "ApproveToPool")
                {
                    if (ApprovalList!.Contains(item)) ApprovalList.Remove(item);
                    if (!PoolList!.Contains(item)) PoolList.Add(item);

                    arrayLists.ApprovalList = JsonConvert.SerializeObject(ApprovalList);
                    arrayLists.PoolList = JsonConvert.SerializeObject(PoolList);
                    actionTaken = "Transfered to Pool List From Approve List";
                }
                else if (listType == "PoolToApprove")
                {
                    if (!ApprovalList!.Contains(item)) ApprovalList.Add(item);
                    if (PoolList!.Contains(item)) PoolList.Remove(item);

                    arrayLists.ApprovalList = JsonConvert.SerializeObject(ApprovalList);
                    arrayLists.PoolList = JsonConvert.SerializeObject(PoolList);
                    actionTaken = "Transfered to Approve List From Pool List";

                }

                helper.UpdateApplicationHistory(item, officerDesignation, actionTaken, "NULL");

            }



            dbcontext.Entry(arrayLists).State = EntityState.Modified;
            dbcontext.SaveChanges();


            return Json(new { status = true });
        }

        [HttpPost]
        public IActionResult UploadCsv([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            SqlParameter AccessLevelCode = new SqlParameter("@AccessLevelCode", DBNull.Value);

            switch (accessLevel)
            {
                case "Tehsil":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["TehsilCode"]?.ToString() ?? string.Empty);
                    break;
                case "District":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DistrictCode"]?.ToString() ?? string.Empty);
                    break;
                case "Division":
                    AccessLevelCode = new SqlParameter("@AccessLevelCode", UserSpecificDetails!["DivisionCode"]?.ToString() ?? string.Empty);
                    break;
            }

            var applicationList = dbcontext.Applications.FromSqlRaw(
                "EXEC GetApplicationsForOfficer @OfficerDesignation, @ActionTaken, @AccessLevel, @AccessLevelCode, @ServiceId",
                new SqlParameter("@OfficerDesignation", officerDesignation),
                new SqlParameter("@ActionTaken", "Sanction"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).ToList();

            string ftpHost = form["ftpHost"].ToString();
            string ftpUser = form["ftpUser"].ToString();
            string ftpPassword = form["ftpPassword"].ToString();

            var builder = new StringBuilder();
            foreach (var application in applicationList)
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                var obj = new
                {
                    referenceNumber = application.ApplicationId,
                    appliedDistrict,
                    submissionDate = application.SubmissionDate!,
                    applicantName = application.ApplicantName,
                    parentage = application.RelationName + $" ({application.Relation.ToUpper()})",
                    accountNumber = bankDetails["AccountNumber"],
                    amount = "500000",
                    dateOfMarriage = serviceSpecific!["DateOfMarriage"],
                };

                builder.AppendLine($"{obj.referenceNumber},{obj.appliedDistrict},{obj.submissionDate},{obj.applicantName},{obj.accountNumber},{obj.amount},{obj.dateOfMarriage}");
                application.ApplicationStatus = "Dispatched";
                helper.UpdateApplicationHistory(application.ApplicationId, officerDesignation, "Dispatched To Bank", "NULL");
            }

            dbcontext.SaveChanges();

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "exports", DateTime.Now.ToString("dd_MMM_yyyy_hh:mm_tt") + "_MAS.csv");
            System.IO.File.WriteAllText(filePath, builder.ToString());


            var ftpClient = new SftpClient(ftpHost, 22, ftpUser, ftpPassword);
            ftpClient.Connect();

            if (!ftpClient.IsConnected) return Json(new { status = false, message = "Unable to connect to the SFTP server." });

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                ftpClient.UploadFile(stream, Path.GetFileName(filePath));
            }
            ftpClient.Disconnect();

            return Json(new { status = true, message = "File Uploaded Successfully." });
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