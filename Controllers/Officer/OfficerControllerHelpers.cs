using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        public bool IsMoreThanSpecifiedDays(string dateString, int value)
        {
            if (DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                DateTime currentDate = DateTime.Now;
                double daysDifference = (currentDate - parsedDate).TotalDays;
                return daysDifference > value;
            }
            else
            {
                throw new ArgumentException("Invalid date format.");
            }
        }
        public dynamic GetCount(string officer = "", int accessCode = -1, int serviceId = -1)
        {
            bool isOfficer = officer != "", isService = serviceId != -1, isDistrict = accessCode != -1;
            var recordsCount = dbcontext.RecordCounts
            .Where(count => !isOfficer || count.Officer == officer)
            .Where(count => !isDistrict || count.AccessCode == accessCode)
            .Where(count => !isService || count.ServiceId == serviceId)
            .GroupBy(rc => 1)
            .Select(g => new
            {
                TotalPending = g.Sum(rc => rc.Pending),
                TotalPendingWithCitizen = g.Sum(rc => rc.PendingWithCitizen),
                TotalForward = g.Sum(rc => rc.Forward),
                TotalSanction = g.Sum(rc => rc.Sanction),
                TotalReturn = g.Sum(rc => rc.Return),
                TotalReject = g.Sum(rc => rc.Reject),
                TotalSumPerRow = g.Sum(rc => rc.Pending + rc.PendingWithCitizen + rc.Forward + rc.Sanction + rc.Return + rc.Reject)
            })
            .FirstOrDefault();


            var obj = new
            {
                PendingCount = recordsCount?.TotalPending ?? 0,  // Use null-coalescing to handle if no results
                PendingWithCitizenCount = recordsCount?.TotalPendingWithCitizen ?? 0,
                ForwardCount = recordsCount?.TotalForward ?? 0,
                SanctionCount = recordsCount?.TotalSanction ?? 0,
                ReturnCount = recordsCount?.TotalReturn ?? 0,
                RejectCount = recordsCount?.TotalReject ?? 0,
                TotalCount = recordsCount?.TotalSumPerRow ?? 0
            };
            return obj;
        }
        public IActionResult GetFilteredCount(int serviceId)
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            var user = dbcontext.Users.FirstOrDefault(u => u.UserId == UserId);
            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(user!.UserSpecificDetails);
            string designation = userSpecificDetails!["Designation"];
            int accessCode = Convert.ToInt32(userSpecificDetails!["AccessCode"]);
            var countList = GetCount(designation, accessCode, serviceId);


            return Json(new { status = true, countList });
        }


        [HttpGet]
        public dynamic? GetApplicationDetails(string? ApplicationId)
        {

            _logger.LogInformation($"Application ID: {ApplicationId}");
            // Fetch the logged-in user ID
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return null;

            // Retrieve the officer's details
            var Officer = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            if (Officer == null)
                return null;

            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];

            // Fetch the general details of the application
            var generalDetails = dbcontext.Applications.FirstOrDefault(u => u.ApplicationId == ApplicationId);
            if (generalDetails == null)
                return null;

            bool canOfficerTakeAction = true;

            // Fetch the current phase, next phase, and previous phase details
            var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == generalDetails.ApplicationId && cur.Officer == officerDesignation);

            CurrentPhase? nextPhase = null, previousPhase = null;
            if (currentPhase != null)
            {
                if (currentPhase.Next != 0)
                {
                    nextPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase.Next);
                    if (nextPhase != null)
                        nextPhase.CanPull = false;
                }

                if (currentPhase.Previous != 0)
                {
                    previousPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.PhaseId == currentPhase.Previous);
                    if (previousPhase != null)
                        previousPhase.CanPull = false;
                }

                dbcontext.SaveChanges();

                // Check if officer can take action based on days elapsed
                if (IsMoreThanSpecifiedDays(currentPhase.ReceivedOn!.ToString(), 15))
                    canOfficerTakeAction = false;
                if (IsMoreThanSpecifiedDays(generalDetails.SubmissionDate!.ToString(), 45))
                    canOfficerTakeAction = false;
            }

            // Fetch address details using stored procedures
            var preAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PresentAddressId)).ToList().FirstOrDefault();
            var perAddressDetails = dbcontext.Set<AddressJoin>().FromSqlRaw("EXEC GetAddressDetails @AddressId", new SqlParameter("@AddressId", generalDetails!.PermanentAddressId)).ToList().FirstOrDefault();

            var serviceContent = dbcontext.Services.FirstOrDefault(u => u.ServiceId == generalDetails.ServiceId);

            // Fetch application history
            var applicationHistory = JsonConvert.DeserializeObject<dynamic>(dbcontext.ApplicationsHistories.FirstOrDefault(his => his.ApplicationId == ApplicationId)!.History);

            // Filter history to include only relevant actions
            List<dynamic> histories = new List<dynamic>();
            foreach (var history in applicationHistory!)
            {
                bool isTransfered = history["ActionTaken"].ToString().Contains("Transfered");
                if (!isTransfered) histories.Add(history);
            }

            // Find update object from history
            string updateObject = "";
            foreach (var item in applicationHistory!)
            {
                if (item["ActionTaken"] == "Update")
                {
                    updateObject = JsonConvert.SerializeObject(item["UpdateObject"]);
                }
            }

            // Prepare the consolidated details to return
            var applicationDetails = new
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

            return applicationDetails;
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
                arrayLists = new Models.Entities.ApplicationList
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
        [HttpGet]
        public IActionResult DownloadAllData(string? type,string? serviceId, string? activeButtons)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int ServiceId = Convert.ToInt32(serviceId);
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
                        ApplicationList = PendingApplications(Officer, 0, 0, type, ServiceId, true);
                        _logger.LogInformation($"Application List: {ApplicationList}");
                        break;
                    case "Sent":
                        ApplicationList = SentApplications(Officer, 0, 0, type, ServiceId, true);
                        break;
                    case "Sanction":
                        ApplicationList = SanctionApplications(Officer, 0, 0, type, ServiceId, true);
                        break;
                    case "Reject":
                        ApplicationList = RejectApplications(Officer, 0, 0, type, ServiceId, true);
                        break;
                    default:
                        _logger.LogError("Invalid application type: {Type}", type);
                        return BadRequest("Invalid application type.");
                }
                
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Applications");
                var currentRow = 1;

                int currentColumn = 1;
                for (int i = 0; i < ApplicationList.columns.Count; i++)
                {
                    string header = ApplicationList.columns[i].title.ToString();

                    // Check if the header contains HTML, for example, the "input" tag
                    if ((ActiveButtons.Count == 0  && !header.Contains("<input")) || ActiveButtons.Contains(header))
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
                        if ((ActiveButtons.Count == 0 && !ApplicationList.columns[i].title.ToString().Contains("<input")) || ActiveButtons.Contains(ApplicationList.columns[i].title.ToString()))
                        {
                            if(cellValue.Contains("{\"function\":")) {
                                var obj = JsonConvert.DeserializeObject<dynamic>(cellValue);
                                cellValue = obj!.buttonText;
                            }
                            worksheet.Cell(currentRow, currentColumn).Value = cellValue;
                            currentColumn++;
                        }
                    }
                }


                var fileName = DateTime.Now.ToString("dd MMM yyyy hh:mm tt") + "_Applications.xlsx";
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "exports", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
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
 
        public IActionResult GeServiceList(){
            
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

            return Json(new{ServiceList});
        }
        
        [HttpGet]
        public IActionResult ServePdf(string fileName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName);

            _logger.LogInformation($"------------File Path: {filePath}----------------");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            // Force the file to be displayed inline
            Response.Headers.Append("Content-Disposition", "inline; filename=" + fileName);

            return File(fileBytes, "application/pdf");
        }

    }
}