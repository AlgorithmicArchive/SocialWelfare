using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        public dynamic PendingApplications(Models.Entities.User Officer, int start, int length)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);

            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string districtCode = UserSpecificDetails?["DistrictCode"]?.ToString() ?? string.Empty;
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

            AccessLevelCode = new SqlParameter("@AccessLevelCode", accessCode ?? string.Empty);



            var applicationList = dbcontext.Applications.FromSqlRaw(
                "EXEC GetApplicationsForOfficer @OfficerDesignation, @ActionTaken, @AccessLevel, @AccessLevelCode, @ServiceId",
                new SqlParameter("@OfficerDesignation", officerDesignation),
                new SqlParameter("@ActionTaken", "Pending"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).AsEnumerable().Skip(start).Take(length).ToList();



            bool canSanction = false;
            bool canUpdate = false;
            int serviceId = 0;
            List<dynamic> PoolList = [];
            List<dynamic> ApprovalList = [];
            List<dynamic> PendingList = [];
            JArray pool = [];

            foreach (var application in applicationList)
            {
                var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == application.ServiceId);
                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                serviceId = service.ServiceId;


                canSanction = officer!["canSanction"];
                canUpdate = officer["canUpdate"];

                var app = new
                {
                    applicationId = application.ApplicationId,
                    applicantName = application.ApplicantName,
                    appliedDistrict,
                    parentage = application.RelationName + $" ({application.Relation.ToUpper()})",
                    motherName = serviceSpecific["MotherName"],
                    dateOfBirth = application.DateOfBirth,
                    dateOfMarriage = serviceSpecific!["DateOfMarriage"],
                    bankDetails = $"{bankDetails["BankName"]}/{bankDetails["IfscCode"]}/{bankDetails["AccountNumber"]}",
                    address = $"{preAddressDetails.Address!.ToUpper()}, TEHSIL:{preAddressDetails.Tehsil!.ToUpper()}, DISTRICT:{preAddressDetails.District!.ToUpper()}, PINCODE:{preAddressDetails.Pincode}",
                    submissionDate = application.SubmissionDate,
                };

                var arrayLists = dbcontext.ApplicationLists.FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));

                if (canSanction)
                {
                    if (arrayLists == null)
                    {
                        dbcontext.Database.ExecuteSqlRaw("EXEC InsertApplicationListTable @ServiceId,@Officer,@AccessLevel,@AccessCode", new SqlParameter("@ServiceId", serviceId), new SqlParameter("@Officer", officerDesignation), new SqlParameter("@AccessLevel", accessLevel), new SqlParameter("@AccessCode", Convert.ToInt32(accessCode)));
                        PendingList.Add(app);
                    }
                    else
                    {
                        var poolList = JsonConvert.DeserializeObject<List<string>>(arrayLists.PoolList);
                        var approvalList = JsonConvert.DeserializeObject<List<string>>(arrayLists.ApprovalList);

                        if (approvalList!.Contains(app.applicationId)) ApprovalList.Add(app);
                        else if (poolList!.Contains(app.applicationId)) PoolList.Add(app);
                        else PendingList.Add(app);
                    }
                }
                else PendingList.Add(app);

            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.PendingList = PendingList;
            obj.PoolList = PoolList;
            obj.ApproveList = ApprovalList;
            obj.canSanction = canSanction;
            obj.canUpdate = canUpdate;
            obj.Type = "Pending";
            obj.ServiceId = serviceId;

            return obj;
        }
        public dynamic SentApplications(Models.Entities.User Officer)
        {
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
                new SqlParameter("@ActionTaken", "Forward,Return,ReturnToEdit"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).ToList();

            var SentApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                bool? canPull = false;
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
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
                    dateOfMarriage = serviceSpecific!["DateOfMarriage"],
                    application.SubmissionDate,
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

            var SantionApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                bool? canPull = false;
                var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(application.ServiceSpecific);
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
                    dateOfMarriage = serviceSpecific!["DateOfMarriage"],
                    application.SubmissionDate,
                    canPull
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
                new SqlParameter("@ActionTaken", "Reject"),
                new SqlParameter("@AccessLevel", accessLevel),
                AccessLevelCode,
                new SqlParameter("@ServiceId", 1)).ToList();

            var RejectApplications = new List<dynamic>();

            foreach (var application in applicationList)
            {
                var data = new
                {
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate,
                };
                RejectApplications.Add(data);
            }

            dynamic obj = new System.Dynamic.ExpandoObject();
            obj.MiscellaneousList = RejectApplications;
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
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);

                foreach (var phase in phases!)
                {
                    if (phase["ActionTaken"] == "Pending" || phase["ActionTaken"] == "Sanction")
                    {
                        applicationCurrentlyWith = phase["Officer"];
                        receivedOn = phase["ReceivedOn"];
                        break;
                    }
                    else if (phase["ActionTaken"] == "ReturnToEdit")
                    {
                        applicationCurrentlyWith = "Citizen";
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


    }
}