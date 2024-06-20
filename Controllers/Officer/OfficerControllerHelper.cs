using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        public dynamic PendingApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];


            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationsForOfficer @OfficerDesignation,@ActionTaken,@District,@ServiceId", new SqlParameter("@OfficerDesignation", officerDesignation), new SqlParameter("@ActionTaken", "Pending"), new SqlParameter("@District", districtCode), new SqlParameter("@ServiceId", 1)).ToList();

            bool canSanction = false;
            bool canUpdate = false;
            List<Application> UpdateList = [];
            List<Application> PoolList = [];
            List<Application> PendingList = [];
            JArray pool = [];
            foreach (var application in applicationList)
            {
                var updateRequest = JsonConvert.DeserializeObject<dynamic>(application.UpdateRequest!);
                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(dbcontext.Services.FirstOrDefault(u => u.ServiceId == application.ServiceId)!.WorkForceOfficers!);

                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);
                canSanction = officer!["canSanction"];
                canUpdate = officer["canUpdate"];
                int requested = updateRequest!["requested"];
                int updated = updateRequest!["updated"];

                if (officerDesignation == "Director Finance" && canSanction)
                    pool = JArray.Parse(officer["pool"].ToString());
                else if (canSanction)
                {
                    var DistrictCode = districtCode.ToString();
                    var poolElement = officer["pool"][districtCode];
                    if (poolElement != null)
                        pool = JArray.Parse(poolElement.ToString());
                    else
                    {
                        pool = [];
                        officer["pool"][DistrictCode] = pool;
                    }
                }

                if (requested == 1 && updated == 0)
                    UpdateList.Add(application);
                else if (pool.Count == 0)
                    PendingList.Add(application);
                else
                {
                    bool inPool = pool.Any(item => item.ToString() == application.ApplicationId);
                    if (inPool)
                        PoolList.Add(application);
                    else PendingList.Add(application);
                }
            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.PendingList = PendingList;
            obj.UpdateList = UpdateList;
            obj.PoolList = PoolList;
            obj.canSanction = canSanction;
            obj.canUpdate = canUpdate;
            obj.Type = "Pending";
            obj.ServiceId = 1;

            return obj;
        }
        public dynamic SentApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];


            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationsForOfficer @OfficerDesignation,@ActionTaken,@District,@ServiceId", new SqlParameter("@OfficerDesignation", officerDesignation), new SqlParameter("@ActionTaken", "Forward,Return"), new SqlParameter("@District", districtCode), new SqlParameter("@ServiceId", 1)).ToList();


            var SentApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                bool? canPull = false;
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);
                foreach (var phase in phases!)
                {
                    if (phase["Officer"] == officerDesignation)
                        canPull = phase["CanPull"];
                }
                var data = new
                {
                    application.ApplicationId,
                    application.ApplicantName,
                    canPull
                };
                SentApplications.Add(data);
            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.SentApplications = SentApplications;
            obj.Type = "Sent";
            return obj;
        }
        public dynamic SanctionApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];

            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationsForOfficer @OfficerDesignation,@ActionTaken,@District,@ServiceId", new SqlParameter("@OfficerDesignation", officerDesignation), new SqlParameter("@ActionTaken", "Sanction"), new SqlParameter("@District", districtCode), new SqlParameter("@ServiceId", 1)).ToList();

            var SantionApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                var data = new
                {
                    application.ApplicationId,
                    application.ApplicantName,
                };
                SantionApplications.Add(data);
            }


            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.MiscellaneousList = SantionApplications;
            obj.Type = "Sanction";
            return obj;
        }
        public dynamic RejectApplications(Models.Entities.User Officer)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"];
            string districtCode = UserSpecificDetails["DistrictCode"];

            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationsForOfficer @OfficerDesignation,@ActionTaken,@District,@ServiceId", new SqlParameter("@OfficerDesignation", officerDesignation), new SqlParameter("@ActionTaken", "Reject"), new SqlParameter("@District", districtCode), new SqlParameter("@ServiceId", 1)).ToList();

            var SantionApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                var data = new
                {
                    application.ApplicationId,
                    application.ApplicantName,
                };
                SantionApplications.Add(data);
            }


            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.MiscellaneousList = SantionApplications;
            obj.Type = "Reject";
            return obj;
        }

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

        public List<dynamic> GetCount(string type, Dictionary<string, string> conditions)
        {
            StringBuilder Condition1 = new StringBuilder();
            StringBuilder Condition2 = new StringBuilder();

            if (type == "Pending")
                Condition1.Append("AND application.ApplicationStatus='Initiated'");
            else if (type == "Sanction")
                Condition1.Append("AND application.ApplicationStatus='Sanctioned'");
            else if (type == "Reject")
                Condition1.Append("AND application.ApplicationStatus='Rejected'");
            else if (type == "PendingWithCitizen")
                Condition1.Append("AND Application.ApplicationStatus='Initiated' AND JSON_VALUE(app.value, '$.ActionTaken')='ReturnToEdit'");

            int conditionCount = 0;
            int splitPoint = conditions != null ? conditions.Count / 2 : 0;

            if (conditions != null && conditions.Count != 0)
            {
                foreach (var condition in conditions)
                {
                    if (conditionCount < splitPoint)
                        Condition1.Append($" AND {condition.Key}='{condition.Value}'");
                    else
                        Condition2.Append($" AND {condition.Key}='{condition.Value}'");

                    conditionCount++;
                }

            }

            if (conditions != null && conditions.ContainsKey("JSON_VALUE(app.value, '$.Officer')") && type != "Total")
            {
                Condition2.Append($" AND JSON_VALUE(app.value, '$.ActionTaken') = '{type}'");
            }
            else if (type == "Total")
            {
                Condition2.Append($" AND JSON_VALUE(app.value, '$.ActionTaken') != ''");
            }

            var applications = dbcontext.Applications.FromSqlRaw("EXEC GetApplications @Condition1, @Condition2",
            new SqlParameter("@Condition1", Condition1.ToString()),
           new SqlParameter("@Condition2", Condition2.ToString())).ToList();

            var list = new List<dynamic>();

            foreach (var application in applications)
            {
                int districtCode = Convert.ToInt32(JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific)!["District"]);
                _logger.LogInformation($"District Code: {districtCode}");
                string AppliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.Uuid == districtCode)!.DistrictName;
                string AppliedService = dbcontext.Services.FirstOrDefault(s => s.ServiceId == application.ServiceId)!.ServiceName;
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

        public IActionResult GetFilteredCount(string? conditions)
        {
            var Conditions = JsonConvert.DeserializeObject<Dictionary<string, string>>(conditions!);
            var TotalCount = GetCount("Total", Conditions!);
            var PendingCount = GetCount("Pending", Conditions!);
            var RejectCount = GetCount("Reject", Conditions!);
            var SanctionCount = GetCount("Sanction", Conditions!);

            return Json(new { status = true, TotalCount, PendingCount, RejectCount, SanctionCount });
        }



    }
}