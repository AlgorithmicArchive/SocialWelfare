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

    }
}