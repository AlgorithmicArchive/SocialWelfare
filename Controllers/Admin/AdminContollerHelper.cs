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
                TotalSumPerRow = g.Sum(rc => rc.Pending + (accessCode != -1 && officer != "" ? rc.Forward + rc.Return : 0) + rc.PendingWithCitizen + rc.Sanction + rc.Reject)
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
        public IActionResult GetFilteredCount(string? conditions)
        {
            int? UserId = HttpContext.Session.GetInt32("UserId");
            int? divisionCode = null;
            var user = dbcontext.Users.FirstOrDefault(u => u.UserId == UserId);
            var userSpecificDetails = JsonConvert.DeserializeObject<dynamic>(user!.UserSpecificDetails);
            if (userSpecificDetails!["DivisionCode"] != null)
                divisionCode = Convert.ToInt32(userSpecificDetails["DivisionCode"]);

            var Conditions = JsonConvert.DeserializeObject<Dictionary<string, string>>(conditions!);
            // var TotalCount = GetCount("Total", Conditions!.Count != 0 ? Conditions : null!, divisionCode);
            // var PendingCount = GetCount("Pending", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            // var RejectCount = GetCount("Reject", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            // var ForwardCount = GetCount("Forward", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            // var SanctionCount = GetCount("Sanction", Conditions.Count != 0 ? Conditions : null!, divisionCode);
            // var PendingWithCitizenCount = GetCount("PendingWithCitizen", Conditions.Count != 0 ? Conditions : null!, divisionCode);

            return Json(new { status = true });
        }

        public IActionResult GetTableRecords(int start, int length, string officer, int district, string type, int totalCount, int serviceId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            int accessCode = Convert.ToInt32(UserSpecificDetails!["AccessCode"].ToString());
            List<dynamic> ApplicationList = [];
            var applications = dbcontext.Applications
            .Join(dbcontext.CurrentPhases,
                app => app.ApplicationId,
                cp => cp.ApplicationId,
                (app, cp) => new { CurrentPhase = cp, Application = app })
            .Where(x => (type == "Total" || x.CurrentPhase.ActionTaken == type)
            && (string.IsNullOrEmpty(officer) || x.CurrentPhase.Officer == officer)
            && (district == 0 || x.CurrentPhase.AccessCode == district)
            && (serviceId == 0 || x.Application.ServiceId == serviceId))
            .Select(x => x.Application)
            .Distinct()  // Ensure uniqueness if needed
            .Skip(start)
            .Take(length)
            .ToList();




            var applicationColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Application Status" },
                    new { title = "Applied District" },
                    new { title = "Application Currently With" },
                    new { title = "Application Received On" },
                    new { title = "Application Submission Date"}
                };


            int appIndex = 1;

            foreach (var application in applications)
            {
                _logger.LogInformation($"------------APPLICATION ID: {application.ApplicationId}-------------------");

                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                string applicationStatus = userDetails.ApplicationStatus == "Initiated" ? "Pending" : userDetails.ApplicationStatus;
                var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == application.ApplicationId && (cur.ActionTaken == "Pending" || cur.ActionTaken == "Sanction" || cur.ActionTaken == "Reject"));

                List<dynamic> applicationData = [
                    appIndex,
                    application.ApplicationId,
                    application.ApplicantName,
                    applicationStatus,
                    appliedDistrict,
                    currentPhase!.Officer,
                    currentPhase.ReceivedOn,
                    application.SubmissionDate!,
                ];

                ApplicationList.Add(applicationData);
            }



            return Json(new
            {
                ApplicationList = new
                {
                    data = ApplicationList,
                    columns = applicationColumns,
                    recordsTotal = totalCount,
                    recordsFiltered = ApplicationList.Count
                },
                Type = type,
                ServiceId = serviceId
            });
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

            int ApplicationDivision = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == district)!.Division;

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
                string AppliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.DistrictName!;
                string AppliedService = dbcontext.Services.FirstOrDefault(s => s.ServiceId == application.ServiceId)?.ServiceName!;
                var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == application.ApplicationId && (cur.ActionTaken == "Pending" || cur.ActionTaken == "Sanction" || cur.ActionTaken == "Reject"));
                string ApplicationCurrentlyWith = currentPhase!.Officer;


                var obj = new
                {
                    ApplicationNo = application.ApplicationId,
                    application.ApplicantName,
                    application.ApplicationStatus,
                    AppliedDistrict,
                    AppliedService,
                    ApplicationCurrentlyWith
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
            string AppliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.DistrictName!;
            string AppliedService = dbcontext.Services.FirstOrDefault(s => s.ServiceId == application.ServiceId)?.ServiceName!;
            var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == application.ApplicationId && (cur.ActionTaken == "Pending" || cur.ActionTaken == "Sanction" || cur.ActionTaken == "Reject"));
            string ApplicationCurrentlyWith = currentPhase!.Officer;

            obj.ApplicationNo = application.ApplicationId;
            obj.ApplicantName = application.ApplicantName;
            obj.DateTime = dateTime;
            obj.PreviouslyWith = actionTaker;
            obj.PreviousStatus = actionTaken != "Sanction" ? "Pending" : "Sanction";
            obj.AppliedDistrict = AppliedDistrict;
            obj.AppliedService = AppliedService;
            obj.ApplicationCurrentlyWith = ApplicationCurrentlyWith;
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
                int? applicationDivision = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == applicationDistrict)!.Division;

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