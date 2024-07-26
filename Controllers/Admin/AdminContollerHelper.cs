using System.Dynamic;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Admin
{
    public partial class AdminController
    {
        public List<dynamic> GetCount(string type, Dictionary<string, string> conditions, int? divisionCode)
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
                string applicationWithOfficer = "";
                string receivedOn = "";
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);

                foreach (var phase in phases!)
                {
                    if (phase["ActionTaken"] == "Pending" || phase["ActionTaken"] == "Sanction")
                    {
                        applicationWithOfficer = phase["Officer"];
                        receivedOn = phase["ReceivedOn"];
                        break;
                    }
                    else if (phase["ActionTaken"] == "ReturnToEdit")
                    {
                        applicationWithOfficer = "Citizen";
                        receivedOn = phase["ReceivedOn"];
                        break;
                    }
                }

                if (divisionCode == null || dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.Division == divisionCode)
                {
                    var obj = new
                    {
                        ApplicationNo = application.ApplicationId,
                        application.ApplicantName,
                        application.ApplicationStatus,
                        AppliedDistrict = appliedDistrict,
                        AppliedService = appliedService,
                        ApplicationWithOfficer = applicationWithOfficer,
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
        [HttpGet]
        public IActionResult GetDistricts(string? division)
        {
            var districts = dbcontext.Districts.AsQueryable();

            if (!string.IsNullOrEmpty(division) && division != "null")
            {
                int divisionCode;
                if (int.TryParse(division, out divisionCode))
                {
                    districts = districts.Where(d => d.Division == divisionCode);
                }
                else
                {
                    return Json(new { status = false, message = "Invalid division code" });
                }
            }

            var districtList = districts.ToList();
            return Json(new { status = true, districts = districtList });
        }
        [HttpGet]
        public IActionResult GetDesignations()
        {
            var designations = dbcontext.OfficersDesignations.Where(des => !des.Designation.Contains("Admin")).ToList();
            return Json(new { status = true, designations });
        }
        [HttpGet]
        public IActionResult GetServices()
        {
            var services = dbcontext.Services.ToList();
            return Json(new { status = true, services });
        }
        [HttpGet]
        public IActionResult GetTeshilForDistrict(string districtId)
        {
            int DistrictId = Convert.ToInt32(districtId);
            var tehsils = dbcontext.Tehsils.Where(u => u.DistrictId == DistrictId).ToList();
            return Json(new { status = true, tehsils });
        }
        [HttpGet]
        public IActionResult GetBlockForDistrict(string districtId)
        {
            int DistrictId = Convert.ToInt32(districtId);
            var blocks = dbcontext.Blocks.Where(u => u.DistrictId == DistrictId).ToList();
            return Json(new { status = true, blocks });
        }
        public List<string> GetDateList(string StartDate, string EndDate)
        {
            List<string> dateList = [];
            DateTime startDate = DateTime.ParseExact(StartDate, "dd MMM yyyy", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact(EndDate, "dd MMM yyyy", CultureInfo.InvariantCulture);
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateList.Add(date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
            }
            return dateList;
        }
        public string GetFormatedDate(string date)
        {
            DateTime checkDate = DateTime.ParseExact(date, "dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture);
            string formatDate = checkDate.ToString("dd MMM yyyy");
            return formatDate;
        }
        public bool CheckIfInDivision(string Division, string District)
        {
            int division = Convert.ToInt32(Division);
            int district = Convert.ToInt32(District);

            int ApplicationDivision = dbcontext.Districts.FirstOrDefault(d => d.Uuid == district)!.Division;

            if (division == ApplicationDivision)
                return true;

            return false;
        }
        public List<dynamic> GetFormatedObject(List<Application> applications)
        {
            var list = new List<dynamic>();

            foreach (var application in applications)
            {
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
                int districtCode = Convert.ToInt32(serviceSpecific!["District"]);
                string AppliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.Uuid == districtCode)?.DistrictName!;
                string AppliedService = dbcontext.Services.FirstOrDefault(s => s.ServiceId == application.ServiceId)?.ServiceName!;
                string ApplicationWithOfficer = "";
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);

                foreach (var phase in phases!)
                {
                    if (phase["ActionTaken"] == "Pending" || phase["ActionTaken"] == "Sanction")
                    {
                        ApplicationWithOfficer = phase["Officer"];
                        break;
                    }
                }

                var obj = new
                {
                    ApplicationNo = application.ApplicationId,
                    application.ApplicantName,
                    application.ApplicationStatus,
                    AppliedDistrict,
                    AppliedService,
                    ApplicationWithOfficer
                };
                list.Add(obj);
            }

            return list;
        }
        public dynamic GetFormattedApplication(Application application, string actionTaken, string actionTaker, string dateTime)
        {
            dynamic obj = new ExpandoObject();
            var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
            int districtCode = Convert.ToInt32(serviceSpecific!["District"]);
            string AppliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.Uuid == districtCode)?.DistrictName!;
            string AppliedService = dbcontext.Services.FirstOrDefault(s => s.ServiceId == application.ServiceId)?.ServiceName!;
            string ApplicationWithOfficer = "";
            var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);

            foreach (var phase in phases!)
            {
                if (phase["ActionTaken"] == "Pending" || phase["ActionTaken"] == "Sanction")
                {
                    ApplicationWithOfficer = phase["Officer"];
                    break;
                }
            }

            obj.ApplicationNo = application.ApplicationId;
            obj.ApplicantName = application.ApplicantName;
            obj.DateTime = dateTime;
            obj.PreviouslyWith = actionTaker;
            obj.PreviousStatus = actionTaken != "Sanction" ? "Pending" : "Sanction";
            obj.AppliedDistrict = AppliedDistrict;
            obj.AppliedService = AppliedService;
            obj.CurrentlyApplicationWithOfficer = ApplicationWithOfficer;
            obj.CurrentApplicationStatus = application.ApplicationStatus;

            return obj;
        }
        [HttpGet]
        public IActionResult GetHistories(string StartDate, string EndDate, string Status)
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            string Division = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)?.UserSpecificDetails!)!.DivisionCode;

            List<string> dateList = GetDateList(StartDate, EndDate);
            var Histories = dbcontext.ApplicationsHistories.ToList();

            var filteredHistories = new List<dynamic>();

            foreach (var history in Histories)
            {
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(history.History);
                foreach (var item in jsonObject!)
                {
                    string actionTaken = (string)item.ActionTaken;
                    string actionTaker = (string)item.ActionTaker;
                    string dateTime = (string)item.DateTime;
                    string formattedDate = GetFormatedDate((string)item.DateTime);
                    if (dateList.Contains(formattedDate))
                    {
                        if (Status == "Pending" && actionTaken != "Sanction")
                        {
                            var application = dbcontext.Applications.FirstOrDefault(app => app.ApplicationId == history.ApplicationId);
                            dynamic formattedApplication = GetFormattedApplication(application!, actionTaken, actionTaker, dateTime);
                            filteredHistories.Add(formattedApplication);
                        }
                        else if (Status == "Sanctioned" && actionTaken == "Sanction")
                        {
                            var application = dbcontext.Applications.FirstOrDefault(app => app.ApplicationId == history.ApplicationId);
                            dynamic formattedApplication = GetFormattedApplication(application!, actionTaken, actionTaker, dateTime);
                            filteredHistories.Add(formattedApplication);
                        }
                    }
                }
            }


            var resultApplications = new List<dynamic>();

            foreach (var application in filteredHistories)
            {
                string applicationId = application.ApplicationNo;
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(dbcontext.Applications.FirstOrDefault(app => app.ApplicationId == applicationId)!.ServiceSpecific);
                string district = serviceSpecific!["District"];
                if (Division != null && CheckIfInDivision(Division, district))
                {
                    resultApplications.Add(application);
                }
                else if (Division == null)
                {
                    resultApplications.Add(application);
                }
            }

            return Json(new { status = true, applications = resultApplications });
        }
        [HttpGet]
        public IActionResult GetHistory(string applicationId)
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            var Division = JsonConvert.DeserializeObject<dynamic>(dbcontext.Users.FirstOrDefault(u => u.UserId == UserId)?.UserSpecificDetails!)!.DivisionCode;
            if (Division != null)
            {
                var application = dbcontext.Applications.FirstOrDefault(a => a.ApplicationId == applicationId);
                int applicationDistrict = JsonConvert.DeserializeObject<dynamic>(application!.ServiceSpecific)!.District;
                int applicationDivision = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == applicationDistrict)!.Division;

                if (Division == applicationDivision)
                {
                    var result = dbcontext.ApplicationsHistories.FirstOrDefault(a => a.ApplicationId == applicationId);
                    return Json(new { status = true, result });
                }
                else
                {
                    return Json(new { status = false, response = "NO RECORD FOUND." });
                }
            }
            else
            {
                var result = dbcontext.ApplicationsHistories.FirstOrDefault(a => a.ApplicationId == applicationId);
                return Json(new { status = true, result });
            }
        }

    }
}