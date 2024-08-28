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
            var recordsCount = dbcontext.RecordCounts.FirstOrDefault();


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
            int serviceId = Convert.ToInt32(form["serviceId"].ToString());
            string action = form["action"].ToString();
            var IdList = JsonConvert.DeserializeObject<string[]>(form["IdList"].ToString());
            var listType = form["listType"].ToString();
            var WorkForceOfficers = JsonConvert.DeserializeObject<dynamic>(dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId)!.WorkForceOfficers!);

            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = UserSpecificDetails!["AccessCode"].ToString();

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

            var arrayLists = dbcontext.ApplicationLists.FirstOrDefault(list =>
            list.ServiceId == serviceId &&
            list.Officer == officerDesignation &&
            list.AccessLevel == accessLevel &&
            list.AccessCode == Convert.ToInt32(accessCode));

            List<string> PoolList, ApprovalList;

            if (arrayLists == null)
            {
                // Create a new ApplicationList record if none exists
                arrayLists = new ApplicationList
                {
                    ServiceId = serviceId,
                    Officer = officerDesignation,
                    AccessLevel = accessLevel,
                    AccessCode = Convert.ToInt32(accessCode),
                    ApprovalList = JsonConvert.SerializeObject(new List<string>()),
                    PoolList = JsonConvert.SerializeObject(new List<string>()),
                };

                dbcontext.ApplicationLists.Add(arrayLists);
                dbcontext.SaveChanges(); // Save the new record to the database

                PoolList = new List<string>();
                ApprovalList = new List<string>();
            }
            else
            {
                PoolList = JsonConvert.DeserializeObject<List<string>>(arrayLists.PoolList) ?? new List<string>();
                ApprovalList = JsonConvert.DeserializeObject<List<string>>(arrayLists.ApprovalList) ?? new List<string>();
            }


            string actionTaken = "";

            foreach (var item in IdList)
            {
                if (listType == "Approve")
                {
                    if (action == "add" && !ApprovalList!.Contains(item)) { ApprovalList.Add(item); actionTaken = "Transfered to Appove List From Inbox"; }
                    else if (action == "remove" && ApprovalList!.Contains(item)) { ApprovalList.Remove(item); actionTaken = "Transfered to Inbox From Approve List"; }

                    arrayLists!.ApprovalList = JsonConvert.SerializeObject(ApprovalList);

                }
                else if (listType == "Pool")
                {
                    if (action == "add" && !PoolList!.Contains(item)) { PoolList.Add(item); actionTaken = "Transfered to Pool List From Approve List"; }
                    else if (action == "remove" && PoolList!.Contains(item)) { PoolList.Remove(item); actionTaken = "Transfered to Inbox From Pool List"; }

                    arrayLists!.PoolList = JsonConvert.SerializeObject(PoolList);
                }
                else if (listType == "ApproveToPool")
                {
                    if (ApprovalList!.Contains(item)) ApprovalList.Remove(item);
                    if (!PoolList!.Contains(item)) PoolList.Add(item);

                    arrayLists!.ApprovalList = JsonConvert.SerializeObject(ApprovalList);
                    arrayLists.PoolList = JsonConvert.SerializeObject(PoolList);
                    actionTaken = "Transfered to Pool List From Approve List";
                }
                else if (listType == "PoolToApprove")
                {
                    if (!ApprovalList!.Contains(item)) ApprovalList.Add(item);
                    if (PoolList!.Contains(item)) PoolList.Remove(item);

                    arrayLists!.ApprovalList = JsonConvert.SerializeObject(ApprovalList);
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

            string ftpHost = form["ftpHost"].ToString();
            string ftpUser = form["ftpUser"].ToString();
            string ftpPassword = form["ftpPassword"].ToString();
            int serviceId = Convert.ToInt32(form["serviceId"].ToString());

            var service = dbcontext.Services.FirstOrDefault(s => s.ServiceId == serviceId)!;

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "exports", service.BankDispatchFile);

            var ftpClient = new SftpClient(ftpHost, 22, ftpUser, ftpPassword);
            ftpClient.Connect();

            if (!ftpClient.IsConnected) return Json(new { status = false, message = "Unable to connect to the SFTP server." });

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                ftpClient.UploadFile(stream, Path.GetFileName(filePath));
            }
            ftpClient.Disconnect();

            service.BankDispatchFile = "";
            dbcontext.SaveChanges();

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