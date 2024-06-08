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

        public dynamic PendingApplications(Models.Entities.Officer Officer)
        {

            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationsForOfficer @OfficerDesignation,@District,@ServiceId", new SqlParameter("@OfficerDesignation", Officer.Designation), new SqlParameter("@District", Officer.DistrictCode), new SqlParameter("@ServiceId", 1)).ToList();


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

                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == Officer.Designation);
                canSanction = officer!["canSanction"];
                canUpdate = officer["canUpdate"];
                int requested = updateRequest!["requested"];
                int updated = updateRequest!["updated"];

                if (Officer.Designation == "Director Finance" && canSanction)
                    pool = JArray.Parse(officer["pool"].ToString());
                else if (canSanction)
                {
                    var districtCode = Officer.DistrictCode.ToString();
                    var poolElement = officer["pool"][districtCode];
                    if (poolElement != null)
                        pool = JArray.Parse(poolElement.ToString());
                    else
                    {
                        pool = [];
                        officer["pool"][districtCode] = pool;
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
        public dynamic SentApplications(Models.Entities.Officer Officer)
        {
            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetPullApplicationsForOfficer @OfficerDesignation, @District", new SqlParameter("@OfficerDesignation", Officer!.Designation), new SqlParameter("@District", Officer!.DistrictCode.ToString())).ToList();

            var SentApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                bool? canPull = false;
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);
                foreach (var phase in phases!)
                {
                    if (phase["Officer"] == Officer.Designation)
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
        public dynamic SanctionApplications(Models.Entities.Officer Officer)
        {
            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationCountForOfficer @OfficerDesignation,@ActionTaken,@HasApplication,@District", new SqlParameter("@OfficerDesignation", Officer!.Designation), new SqlParameter("@ActionTaken", "Sanction"), new SqlParameter("@HasApplication", "false"), new SqlParameter("@District", Officer!.DistrictCode.ToString())).ToList();

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
        public dynamic RejectApplications(Models.Entities.Officer Officer)
        {
            var applicationList = dbcontext.Applications.FromSqlRaw("EXEC GetApplicationCountForOfficer @OfficerDesignation,@ActionTaken,@HasApplication,@District", new SqlParameter("@OfficerDesignation", Officer!.Designation), new SqlParameter("@ActionTaken", "Reject"), new SqlParameter("@HasApplication", "false"), new SqlParameter("@District", Officer!.DistrictCode.ToString())).ToList();

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