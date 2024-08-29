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
        public List<dynamic> GetCount(string type, Dictionary<string, string> conditions, int? divisionCode = null)
        {
            var condition1 = new StringBuilder();
            var condition2 = new StringBuilder();
            var conditionsList = conditions?.ToList() ?? [];

            switch (type)
            {
                case "Total":
                    condition1.Append(" AND JSON_VALUE(app.value, '$.ActionTaken') != ''");
                    break;
                case "Pending":
                    condition1.Append("AND a.ApplicationStatus='Initiated'  AND JSON_VALUE(app.value, '$.ActionTaken') = 'Pending'");
                    break;
                case "Sanction":
                    condition1.Append("AND a.ApplicationStatus='Sanctioned'  AND JSON_VALUE(app.value, '$.ActionTaken') = 'Sanction'");
                    break;
                case "Reject":
                    condition1.Append("AND a.ApplicationStatus='Rejected'  AND JSON_VALUE(app.value, '$.ActionTaken') = 'Reject'");
                    break;
                case "Forward":
                    condition1.Append(" AND JSON_VALUE(app.value, '$.ActionTaken') = 'Forward'");
                    break;
                case "PendingWithCitizen":
                    condition1.Append("AND a.ApplicationStatus='Initiated' AND JSON_VALUE(app.value, '$.ActionTaken')='ReturnToEdit'");
                    break;
            }

            int splitPoint = conditionsList.Count / 2;

            for (int i = 0; i < conditionsList.Count; i++)
            {
                var condition = conditionsList[i];
                if (i < splitPoint)
                    condition1.Append($" AND {condition.Key}='{condition.Value}'");
                else
                    condition2.Append($" AND {condition.Key}='{condition.Value}'");
            }



            var applications = dbcontext.Applications.FromSqlRaw("EXEC GetApplications @Condition1, @Condition2",
                new SqlParameter("@Condition1", condition1.ToString()),
                new SqlParameter("@Condition2", condition2.ToString())).ToList();

            var list = new List<dynamic>();

            foreach (var application in applications)
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
                int districtCode = Convert.ToInt32(serviceSpecific!["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.DistrictName!;
                string appliedService = dbcontext.Services.FirstOrDefault(s => s.ServiceId == application.ServiceId)?.ServiceName!;
                string applicationCurrentlyWith = "";
                string receivedOn = "";

                var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(curr => curr.ApplicationId == application.ApplicationId && (curr.ActionTaken == "Pending" || curr.ActionTaken == "Sanction" || curr.ActionTaken == "ReturnToEdit"));
                applicationCurrentlyWith = currentPhase!.ActionTaken != "ReturnToEdit" ? currentPhase.Officer : "Citizen";
                receivedOn = currentPhase.ReceivedOn;

                if (divisionCode == null || dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.Division == divisionCode)
                {
                    var obj = new
                    {
                        ApplicationNo = application.ApplicationId,
                        application.ApplicantName,
                        application.ApplicationStatus,
                        AppliedDistrict = appliedDistrict,
                        AppliedService = appliedService,
                        ApplicationCurrentlyWith = applicationCurrentlyWith,
                        ReceivedOn = receivedOn,
                        SubmissionDate = application.SubmissionDate!.ToString().Split('T')[0]
                    };
                    list.Add(obj);
                }
            }

            return list;
        }
        public IActionResult GetFilteredCount(string? conditions)
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            int? divisionCode = null;
            var user = dbcontext.Users.FirstOrDefault(u => u.UserId == UserId);
            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(user!.UserSpecificDetails);
            if (userSpecificDetails!["DivisionCode"] != null)
                divisionCode = Convert.ToInt32(userSpecificDetails["DivisionCode"]);

            var Conditions = JsonConvert.DeserializeObject<Dictionary<string, string>>(conditions!);
            var TotalCount = GetCount("Total", Conditions!.Count != 0 ? Conditions : null!, divisionCode);
            var PendingCount = GetCount("Pending", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            var RejectCount = GetCount("Reject", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            var ForwardCount = GetCount("Forward", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            var SanctionCount = GetCount("Sanction", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            var PendingWithCitizenCount = GetCount("PendingWithCitizen", Conditions.Count != 0 ? Conditions : null!, divisionCode);

            return Json(new { status = true, TotalCount, PendingCount, RejectCount, ForwardCount, SanctionCount, PendingWithCitizenCount });
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
    }
}