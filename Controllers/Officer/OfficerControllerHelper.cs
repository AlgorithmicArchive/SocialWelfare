using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        public dynamic PendingApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
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
                new SqlParameter("@ServiceId", 1)).ToList();


            var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
            var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);

            bool canSanction = officer!["canSanction"];
            bool canUpdate = officer["canUpdate"];

            List<dynamic> PoolList = [];
            List<dynamic> ApprovalList = [];
            List<dynamic> PendingList = [];
            JArray pool = [];
            var pendingColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Choose <input type='checkbox' class='form-check pending-parent' name='pending-parent' />" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Date Of Marriage" },
                    new { title = "Submission Date" },
                    new { title = "Action"}
                };

            var approveColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Choose <input type='checkbox' class='form-check approve-parent' name='approve-parent' />" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Submission Location" },
                    new { title = "Parentage" },
                    new { title = "Mother's Name" },
                    new { title = "Date Of Birth" },
                    new { title = "Date Of Marriage" },
                    new { title = "Bank Details" },
                    new { title = "Address" },
                    new { title = "Submission Date" },
                };

            if (!canSanction) pendingColumns.RemoveAt(1);

            var poolColumns = new List<dynamic>(pendingColumns);

            int pendingIndex = 1;
            int approveIndex = 1;
            int poolIndex = 1;

            foreach (var application in applicationList)
            {

                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();



                List<dynamic> pendingData = [
                    pendingIndex,
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
                    $"<button class='btn btn-dark w-100' onclick=UserDetails('{application.ApplicationId}');>View</button>"
                ];


                if (canSanction)
                    pendingData.Insert(1, $"<input type='checkbox' class='form-check pending-element' value='{application.ApplicationId}' name='pending-element' />");


                if (serviceSpecific["DateOfMarriage"] != null)
                    pendingData.Insert(pendingData.Count - 2, serviceSpecific["DateOfMarriage"]);
                else
                    pendingColumns.RemoveAt(pendingColumns.Count - 2);

                List<dynamic> approveData = [
                    approveIndex,
                    $"<input type='checkbox' class='form-check approve-element' value='{application.ApplicationId}' name='pending-element' />",
                    application.ApplicationId,
                    application.ApplicantName,
                    appliedDistrict,
                    application.RelationName + $" ({application.Relation.ToUpper()})",
                    serviceSpecific["MotherName"],
                    application.DateOfBirth,
                    serviceSpecific!["DateOfMarriage"],
                    $"{bankDetails["BankName"]}/{bankDetails["IfscCode"]}/{bankDetails["AccountNumber"]}",
                    $"{preAddressDetails.Address!.ToUpper()}, TEHSIL:{preAddressDetails.Tehsil!.ToUpper()}, DISTRICT:{preAddressDetails.District!.ToUpper()}, PINCODE:{preAddressDetails.Pincode}",
                    application.SubmissionDate!
                ];

                var arrayLists = dbcontext.ApplicationLists.FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));

                if (canSanction)
                {
                    if (arrayLists == null)
                    {
                        dbcontext.Database.ExecuteSqlRaw("EXEC InsertApplicationListTable @ServiceId,@Officer,@AccessLevel,@AccessCode", new SqlParameter("@ServiceId", serviceId), new SqlParameter("@Officer", officerDesignation), new SqlParameter("@AccessLevel", accessLevel), new SqlParameter("@AccessCode", Convert.ToInt32(accessCode)));
                        PendingList.Add(pendingData);
                        pendingIndex++;
                    }
                    else
                    {
                        var poolList = JsonConvert.DeserializeObject<List<string>>(arrayLists.PoolList);
                        var approvalList = JsonConvert.DeserializeObject<List<string>>(arrayLists.ApprovalList);

                        if (approvalList!.Contains(application.ApplicationId))
                        {
                            ApprovalList.Add(approveData);
                            approveIndex++;
                        }
                        else if (poolList!.Contains(application.ApplicationId))
                        {
                            poolColumns[1] = new { title = "<input type='checkbox' class='form-check poolList-parent' value='' name='poolList-parent' />" };
                            pendingData[1] = $"<input type='checkbox' class='form-check poolList-element' value='{application.ApplicationId}' name='poolList-element' />";
                            PoolList.Add(pendingData);
                            poolIndex++;
                        }
                        else
                        {
                            PendingList.Add(pendingData);
                            pendingIndex++;
                        }
                    }
                }
                else
                {
                    PendingList.Add(pendingData);
                    pendingIndex++;
                }
            }



            var obj = new
            {
                PendingList = new
                {
                    data = PendingList.AsEnumerable().Skip(start).Take(length),
                    columns = pendingColumns,
                    recordsTotal = PendingList.Count,
                    recordsFiltered = PendingList.AsEnumerable().Skip(start).Take(length).ToList().Count,
                },
                PoolList = new
                {
                    data = PoolList.AsEnumerable().Skip(start).Take(length),
                    columns = poolColumns,
                    recordsTotal = PoolList.Count,
                    recordsFiltered = PoolList.AsEnumerable().Skip(start).Take(length).ToList().Count
                },
                ApproveList = new
                {
                    data = ApprovalList.AsEnumerable().Skip(start).Take(length),
                    columns = approveColumns,
                    recordsTotal = ApprovalList.Count,
                    recordsFiltered = ApprovalList.AsEnumerable().Skip(start).Take(length).ToList().Count
                },
                Type = type,
                ServiceId = 1
            };

            if (!AllData)
                return obj;
            else return type == "Pending" ? new { columns = pendingColumns, data = PendingList } : type == "Approve" ? new { columns = approveColumns, data = ApprovalList } : new { columns = poolColumns, data = PoolList };
        }
        public dynamic SentApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
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

            var sentColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Date Of Marriage" },
                    new { title = "Submission Date" },
                    new { title = "Action"}
                };

            int index = 0;
            foreach (var application in applicationList)
            {
                bool canPull = false;
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);
                var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific!["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();

                foreach (var phase in phases!)
                {
                    if (phase["Officer"] == officerDesignation)
                        canPull = phase["CanPull"];
                }

                List<dynamic> sentData = [
                    index+1,
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
                    canPull? $"<button class='btn btn-dark w-100' onclick=PullApplication('${application.ApplicationId}');>Pull</button>":"Cannot Pull"
                ];

                if (serviceSpecific["DateOfMarriage"] != null)
                    sentData.Insert(sentData.Count - 1, serviceSpecific["DateOfMarriage"]);
                else
                    sentColumns.RemoveAt(sentData.Count - 1);


                SentApplications.Add(sentData);

                index++;
            }

            var obj = new
            {

                SentList = new
                {
                    data = SentApplications.AsEnumerable().Skip(start).Take(length),
                    columns = sentColumns,
                    recordsTotal = SentApplications.Count,
                    recordsFiltered = SentApplications.AsEnumerable().Skip(start).Take(length).ToList().Count
                },
                Type = type,
                ServiceId = serviceId
            };

            if (!AllData)
                return obj;
            else return new { columns = sentColumns, data = SentApplications };
        }
        public dynamic SanctionApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
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

            // Filter the applications in code
            applicationList = applicationList.Where(app => app.ApplicationStatus == "Sanctioned").ToList();

            var SanctionApplications = new List<dynamic>();
            var sanctionColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Date Of Marriage" },
                    new { title = "Submission Date" },
                    new { title = "Action"}
                };

            int index = 0;
            foreach (var application in applicationList)
            {
                bool canPull = false;
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);
                var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == application.ServiceId);
                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific!["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                serviceId = service.ServiceId;

                foreach (var phase in phases!)
                {
                    if (phase["Officer"] == officerDesignation)
                        canPull = phase["CanPull"];
                }

                List<dynamic> sanctionData = [
                    index+1,
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
                    canPull? $"<button class='btn btn-dark w-100' onclick=PullApplication('${application.ApplicationId}');>Pull</button>":"Cannot Pull"
                ];

                if (serviceSpecific["DateOfMarriage"] != null)
                    sanctionData.Insert(sanctionData.Count - 1, serviceSpecific["DateOfMarriage"]);
                else
                    sanctionColumns.RemoveAt(sanctionData.Count - 1);


                SanctionApplications.Add(sanctionData);

                index++;
            }

            var obj = new
            {

                SanctionList = new
                {
                    data = SanctionApplications.AsEnumerable().Skip(start).Take(length),
                    columns = sanctionColumns,
                    recordsTotal = SanctionApplications.Count,
                    recordsFiltered = SanctionApplications.AsEnumerable().Skip(start).Take(length).ToList().Count
                },
                Type = type,
                ServiceId = 1
            };

            if (!AllData)
                return obj;
            else return new { columns = sanctionColumns, data = SanctionApplications };
        }
        public dynamic RejectApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
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

            var RejectColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Date Of Marriage" },
                    new { title = "Submission Date" },
                    new { title = "Action"}
                };

            int index = 0;
            foreach (var application in applicationList)
            {
                bool canPull = false;
                var phases = JsonConvert.DeserializeObject<dynamic>(application.Phase);
                var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == application.ServiceId);
                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific!["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                serviceId = service.ServiceId;

                foreach (var phase in phases!)
                {
                    if (phase["Officer"] == officerDesignation)
                        canPull = phase["CanPull"];
                }

                List<dynamic> RejectData = [
                    index+1,
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
                    canPull? $"<button class='btn btn-dark w-100' onclick=PullApplication('${application.ApplicationId}');>Pull</button>":"Cannot Pull"
                ];

                if (serviceSpecific["DateOfMarriage"] != null)
                    RejectData.Insert(RejectData.Count - 1, serviceSpecific["DateOfMarriage"]);
                else
                    RejectColumns.RemoveAt(RejectData.Count - 1);


                RejectApplications.Add(RejectData);

                index++;
            }

            var obj = new
            {

                SanctionList = new
                {
                    data = RejectApplications.AsEnumerable().Skip(start).Take(length),
                    columns = RejectColumns,
                    recordsTotal = RejectApplications.Count,
                    recordsFiltered = RejectApplications.AsEnumerable().Skip(start).Take(length).ToList().Count
                },
                Type = type,
                ServiceId = 1
            };

            if (!AllData)
                return obj;
            else return new { columns = RejectColumns, data = RejectApplications };
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