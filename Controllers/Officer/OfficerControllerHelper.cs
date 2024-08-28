using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SocialWelfare.Models.Entities;
using System.Text;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Crypto;
using iText.Bouncycastle.Crypto;
using iText.Commons.Bouncycastle.Cert;
using iText.Bouncycastle.X509;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using iText.Forms.Form.Element;
using iText.Forms.Fields.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        public dynamic PendingApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);

            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = UserSpecificDetails["AccessCode"].ToString();



            var recordsCount = dbcontext.RecordCounts.FirstOrDefault(rc => rc.ServiceId == serviceId && rc.Officer == officerDesignation && rc.AccessCode == Convert.ToInt32(accessCode));
            int totalPending = recordsCount!.Pending;
            var arrayLists = dbcontext.ApplicationLists.FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));
            List<string> poolList = [];
            List<string> approveList = [];

            if (arrayLists != null)
            {
                poolList = JsonConvert.DeserializeObject<List<string>>(arrayLists!.PoolList)!;
                approveList = JsonConvert.DeserializeObject<List<string>>(arrayLists!.ApprovalList)!;
                totalPending -= poolList.Count - approveList.Count;
            }

            var applicationList = dbcontext.CurrentPhases
                .Join(dbcontext.Applications,
                    cp => cp.ApplicationId,
                    a => a.ApplicationId,
                    (cp, a) => new { CurrentPhase = cp, Application = a })
                .Where(x =>
                    x.Application.ServiceId == serviceId &&
                    x.CurrentPhase.ActionTaken == "Pending" &&
                    x.CurrentPhase.Officer == officerDesignation &&
                    (arrayLists == null ||
                    (!poolList.Contains(x.Application.ApplicationId) &&
                     !approveList.Contains(x.Application.ApplicationId)))
                )
                .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
                .Where(x =>
                {
                    if (accessLevel != "State")
                    {
                        var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                        string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                        return locationCode == accessCode;
                    }
                    return true; // If accessLevel is "State", do not filter out the item.
                })
                .Select(x => x.Application)
                .Skip(start)
                .Take(length)
                .ToList();

            var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
            var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);

            bool canSanction = officer!["canSanction"];

            var pendingColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Choose <input type='checkbox' class='form-check pending-parent' name='pending-parent' />" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Date Of Marriage" },
                    new { title = "Submission Date" },
                    new { title = "Action" }
                };

            if (!canSanction) pendingColumns.RemoveAt(1);

            List<dynamic> PendingList = [];

            int pendingIndex = 1;

            foreach (var application in applicationList)
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();

                List<dynamic> pendingData =
                    [
                        pendingIndex,
                        $"<input type='checkbox' class='form-check pending-element' value='{application.ApplicationId}' name='pending-element' />",
                        application.ApplicationId,
                        application.ApplicantName,
                        serviceSpecific["DateOfMarriage"],
                        application.SubmissionDate!,
                        $"<button class='btn btn-dark w-100' onclick=UserDetails('{application.ApplicationId}');>View</button>"
                    ];
                if (!canSanction) pendingData.RemoveAt(1);
                if (serviceSpecific["DateOfMarriage"] == null)
                    pendingColumns.RemoveAt(pendingColumns.Count - 2);

                PendingList.Add(pendingData);
                pendingIndex++;
            }

            var obj = new
            {
                PendingList = new
                {
                    data = PendingList,
                    columns = pendingColumns,
                    recordsTotal = totalPending,
                    recordsFiltered = PendingList.Count
                },
                PoolCount = poolList.Count,
                ApproveCount = approveList.Count,
                Type = type,
                ServiceId = serviceId
            };

            return !AllData ? obj : new
            {
                columns = pendingColumns,
                data = PendingList
            };
        }


        public dynamic PoolApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
        {

            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);

            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = UserSpecificDetails!["AccessCode"].ToString();


            var arrayLists = dbcontext.ApplicationLists.FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));
            var poolList = JsonConvert.DeserializeObject<List<string>>(arrayLists!.PoolList) ?? [];


            var applicationList = dbcontext.CurrentPhases
            .Join(dbcontext.Applications,
                cp => cp.ApplicationId,
                a => a.ApplicationId,
                (cp, a) => new { CurrentPhase = cp, Application = a })
            .Where(x =>
                poolList.Contains(x.Application.ApplicationId) &&
                x.Application.ServiceId == serviceId &&
                x.CurrentPhase.ActionTaken == "Pending" &&
                x.CurrentPhase.Officer == officerDesignation)
            .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
            .Where(x =>
            {
                if (accessLevel != "State")
                {
                    var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                    string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                    return locationCode == accessCode;
                }
                return true; // If accessLevel is "State", do not filter out the item.
            })
            .Select(x => x.Application)
            .Skip(start)
            .Take(length)
            .ToList();

            var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
            var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);

            bool canSanction = officer!["canSanction"];
            bool canUpdate = officer["canUpdate"];

            List<dynamic> PoolList = [];
            JArray pool = [];
            var poolColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Choose <input type='checkbox' class='form-check poolList-parent' value='' name='poolList-parent' />" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Date Of Marriage" },
                    new { title = "Submission Date" },
                    new { title = "Action"}
                };


            int poolIndex = 1;

            foreach (var application in applicationList)
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();


                List<dynamic> poolData = [
                    poolIndex,
                    $"<input type='checkbox' class='form-check poolList-element' value='{application.ApplicationId}' name='poolList-element' />",
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
                    $"<button class='btn btn-dark w-100' onclick=UserDetails('{application.ApplicationId}');>View</button>"
                ];

                if (serviceSpecific["DateOfMarriage"] != null)
                    poolData.Insert(poolData.Count - 2, serviceSpecific["DateOfMarriage"]);
                else
                    poolColumns.RemoveAt(poolColumns.Count - 2);


                PoolList.Add(poolData);

            }

            var obj = new
            {

                PoolList = new
                {
                    data = PoolList,
                    columns = poolColumns,
                    recordsTotal = poolList.Count,
                    recordsFiltered = PoolList.Count
                },
                Type = type,
                ServiceId = 1
            };

            if (!AllData)
                return obj;
            else return new { columns = poolColumns, data = PoolList };
        }

        public dynamic ApproveApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
        {

            _logger.LogInformation($"-----------H E R E  I N  A P P R O V E  L I S T  ALLDATA:{AllData}-------------");

            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);

            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = UserSpecificDetails!["AccessCode"].ToString();


            var arrayLists = dbcontext.ApplicationLists.FirstOrDefault(list =>
                list.ServiceId == serviceId &&
                list.Officer == officerDesignation &&
                list.AccessLevel == accessLevel &&
                list.AccessCode == Convert.ToInt32(accessCode));


            var approvalList = JsonConvert.DeserializeObject<List<string>>(arrayLists!.ApprovalList) ?? [];

            var applicationList = dbcontext.CurrentPhases
                .Join(dbcontext.Applications,
                    cp => cp.ApplicationId,
                    a => a.ApplicationId,
                    (cp, a) => new { CurrentPhase = cp, Application = a })
                .Where(x =>
                    approvalList.Contains(x.Application.ApplicationId) &&
                    x.Application.ServiceId == serviceId &&
                    x.CurrentPhase.ActionTaken == "Pending" &&
                    x.CurrentPhase.Officer == officerDesignation)
                .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
                .Where(x =>
                {
                    if (accessLevel != "State")
                    {
                        var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                        string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                        return locationCode == accessCode;
                    }
                    return true; // If accessLevel is "State", do not filter out the item.
                })
                .Select(x => x.Application)
                .Skip(start)
                .Take(length)
                .ToList();

            var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
            var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);

            bool canSanction = officer!["canSanction"];
            bool canUpdate = officer["canUpdate"];

            List<dynamic> ApproveList = [];
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
                new { title = "Submission Date" }
            };

            int approveIndex = 1;

            foreach (var application in applicationList)
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();

                List<dynamic> approveData =
                [
                    approveIndex,
                    $"<input type='checkbox' class='form-check approve-element' value='{application.ApplicationId}' name='approve-element' />",
                    application.ApplicationId,
                    application.ApplicantName,
                    appliedDistrict,
                    application.RelationName + $" ({application.Relation.ToUpper()})",
                    serviceSpecific["MotherName"],
                    application.DateOfBirth,
                    serviceSpecific["DateOfMarriage"],
                    $"{bankDetails["BankName"]}/{bankDetails["IfscCode"]}/{bankDetails["AccountNumber"]}",
                    $"{preAddressDetails.Address!.ToUpper()}, TEHSIL:{preAddressDetails.Tehsil!.ToUpper()}, DISTRICT:{preAddressDetails.District!.ToUpper()}, PINCODE:{preAddressDetails.Pincode}",
                    application.SubmissionDate!
                ];

                if (serviceSpecific["DateOfMarriage"] != null)
                    approveData.Insert(approveData.Count - 2, serviceSpecific["DateOfMarriage"]);
                else
                    approveColumns.RemoveAt(approveColumns.Count - 2);

                ApproveList.Add(approveData);
                approveIndex++;
            }

            var obj = new
            {
                ApproveList = new
                {
                    data = ApproveList,
                    columns = approveColumns,
                    recordsTotal = approvalList.Count,
                    recordsFiltered = ApproveList.Count
                },
                Type = type,
                ServiceId = serviceId
            };

            _logger.LogInformation($"------------AllDATA: {AllData}-----------------------------");


            if (!AllData)
                return obj;
            else
                return new { columns = approveColumns, data = ApproveList };
        }



        public dynamic SentApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
        {
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;

            string accessCode = UserSpecificDetails!["AccessCode"].ToString();

            var applicationList = dbcontext.CurrentPhases
           .Join(dbcontext.Applications,
               cp => cp.ApplicationId,
               a => a.ApplicationId,
               (cp, a) => new { CurrentPhase = cp, Application = a })
           .Where(x => x.Application.ServiceId == serviceId && (x.CurrentPhase.ActionTaken == "Forward" || x.CurrentPhase.ActionTaken == "Return") && x.CurrentPhase.Officer == officerDesignation)
           .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
           .Where(x =>
           {
               if (accessLevel != "State")
               {
                   var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                   string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                   return locationCode == accessCode;
               }
               return true; // If accessLevel is "State", do not filter out the item.
           })
           .Select(x => x.Application)
           .ToList();

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
                var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific!["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.ApplicationId == application.ApplicationId && cur.Officer == officerDesignation);
                canPull = currentPhase!.CanPull;


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

            var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
            var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);

            int accessCode = Convert.ToInt32(UserSpecificDetails!["AccessCode"].ToString());
            var recordsCount = dbcontext.RecordCounts.FirstOrDefault(rc => rc.ServiceId == serviceId && rc.Officer == officerDesignation && rc.AccessCode == Convert.ToInt32(accessCode));
            var currentPhase = dbcontext.CurrentPhases.FirstOrDefault(cur => cur.AccessCode == accessCode && cur.Officer == officerDesignation);


            var applicationList = dbcontext.Applications
                 .Where(app => app.ServiceId == serviceId && app.ApplicationStatus == "Sanctioned")
                 .AsEnumerable().Skip(start).Take(length)
                 .ToList();

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

                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific!["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                canPull = currentPhase!.CanPull;

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
                    data = SanctionApplications,
                    columns = sanctionColumns,
                    recordsTotal = recordsCount!.Sanction,
                    recordsFiltered = applicationList.Count,
                },
                Type = type,
                ServiceId = serviceId,
                BankFile = service.BankDispatchFile
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
            string accessCode = UserSpecificDetails!["AccessCode"].ToString();


            var applicationList = dbcontext.CurrentPhases
          .Join(dbcontext.Applications,
              cp => cp.ApplicationId,
              a => a.ApplicationId,
              (cp, a) => new { CurrentPhase = cp, Application = a })
          .Where(x => x.Application.ServiceId == serviceId && x.CurrentPhase.ActionTaken == "Reject" && x.CurrentPhase.Officer == officerDesignation)
          .AsEnumerable() // Switch to LINQ to Objects for JSON deserialization
          .Where(x =>
          {
              if (accessLevel != "State")
              {
                  var serviceSpecific = JsonConvert.DeserializeObject<dynamic>(x.Application.ServiceSpecific);
                  string locationCode = serviceSpecific?[accessLevel]?.ToString() ?? string.Empty;
                  return locationCode == accessCode;
              }
              return true; // If accessLevel is "State", do not filter out the item.
          })
          .Select(x => x.Application)
          .ToList();

            var RejectApplications = new List<dynamic>();

            var RejectColumns = new List<dynamic>
                {
                    new { title = "S.No" },
                    new { title = "Reference Number" },
                    new { title = "Applicant Name" },
                    new { title = "Date Of Marriage" },
                    new { title = "Submission Date" },
                };

            int index = 0;
            foreach (var application in applicationList)
            {
                var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == application.ServiceId);
                var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
                var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                int DistrictCode = Convert.ToInt32(serviceSpecific!["District"]);
                string appliedDistrict = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == DistrictCode)!.DistrictName.ToUpper();
                serviceId = service.ServiceId;


                List<dynamic> RejectData = [
                    index+1,
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
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

        private async Task UpdateApplicationHistoryAsync(IEnumerable<BankFile> bankFileData, string officer, string fileName)
        {
            // Get all the ApplicationIds from the bankFileData
            var applicationIds = bankFileData.Select(data => data.ApplicationId).ToList();

            // Fetch all relevant application histories in a single query
            var applicationHistories = await dbcontext.ApplicationsHistories
                .Where(app => applicationIds.Contains(app.ApplicationId))
                .ToListAsync();

            foreach (var data in bankFileData)
            {
                // Find the corresponding history record
                var applicationHistory = applicationHistories.FirstOrDefault(app => app.ApplicationId == data.ApplicationId);
                if (applicationHistory != null)
                {
                    // Deserialize history
                    var history = JsonConvert.DeserializeObject<List<dynamic>>(applicationHistory.History) ?? new List<dynamic>();

                    // Create the new history object
                    var newHistoryEntry = new
                    {
                        ActionTaker = officer,
                        ActionTaken = "Appended To Bank File",
                        Remarks = "NIL",
                        DateTime = DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
                        UpdateObject = (dynamic)null!,
                        File = fileName
                    };

                    // Add the new history entry
                    history.Add(newHistoryEntry);

                    // Serialize the history back to the string and update the database entity
                    applicationHistory.History = JsonConvert.SerializeObject(history);
                }
            }

            // Save all changes in a single call
            await dbcontext.SaveChangesAsync();
        }




        public async Task<IActionResult> BankCsvFile(string serviceId)
        {
            int serviceIdInt = Convert.ToInt32(serviceId);
            string staticAmount = "50000"; // or any other value you need
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            var details = JsonConvert.DeserializeObject<dynamic>(Officer.UserSpecificDetails);
            string officerDesignation = details!["Designation"];
            int accessCode = Convert.ToInt32(details["AccessCode"]);
            var service = dbcontext.Services.FirstOrDefault(s => s.ServiceId == serviceIdInt)!;

            var recordsCount = dbcontext.RecordCounts
                .FirstOrDefault(rc => rc.ServiceId == serviceIdInt && rc.Officer == officerDesignation && rc.AccessCode == accessCode);

            // Define the path where the CSV file will be stored
            string webRootPath = _webHostEnvironment.WebRootPath;
            string exportsFolder = Path.Combine(webRootPath, "exports");

            // Ensure the exports directory exists
            if (!Directory.Exists(exportsFolder))
            {
                Directory.CreateDirectory(exportsFolder);
            }

            string fileName;
            bool fileExists;

            if (!string.IsNullOrEmpty(service.BankDispatchFile))
            {
                fileName = service.BankDispatchFile;
                fileExists = true;
            }
            else
            {
                fileName = $"BankCsvFile_{serviceIdInt}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                fileExists = false;
                service.BankDispatchFile = fileName;
            }
            string filePath = Path.Combine(exportsFolder, fileName);

            // Notify the start of the process
            await hubContext.Clients.All.SendAsync("ReceiveProgress", 0);

            // Fetch data using the stored procedure
            var bankFileData = await dbcontext.BankFiles
                .FromSqlRaw("EXEC GetBankFileData @ServiceId, @StaticAmount",
                            new SqlParameter("@ServiceId", serviceIdInt),
                            new SqlParameter("@StaticAmount", staticAmount))
                .AsNoTracking()
                .ToListAsync();

            await UpdateApplicationHistoryAsync(bankFileData, officerDesignation, fileName);

            int totalRecords = bankFileData.Count;
            int batchSize = 1000; // Adjust the batch size as needed
            int processedRecords = 0;

            // Check if the file already exists

            using (var streamWriter = new StreamWriter(filePath, append: fileExists))
            using (var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = !fileExists }))
            {
                while (processedRecords < totalRecords)
                {
                    // Determine the next batch of records
                    var batch = bankFileData.Skip(processedRecords).Take(batchSize);

                    // Write the current batch to the CSV file
                    await csvWriter.WriteRecordsAsync(batch);

                    // Update progress
                    processedRecords += batch.Count();
                    int progress = (int)(processedRecords / (double)totalRecords * 100);
                    await hubContext.Clients.All.SendAsync("ReceiveProgress", progress);
                }
            }

            // Notify completion
            await hubContext.Clients.All.SendAsync("ReceiveProgress", 100);

            recordsCount!.Sanction -= totalRecords;
            dbcontext.SaveChanges();

            // Return the file path as JSON
            return Json(new { filePath = $"/exports/{fileName}" });
        }

        public IActionResult IsBankFile(string serviceId)
        {
            int ServiceId = Convert.ToInt32(serviceId);
            string BankDispatchFile = dbcontext.Services.FirstOrDefault(s => s.ServiceId == ServiceId)!.BankDispatchFile;

            return Json(new { isFilePresent = BankDispatchFile != "" });
        }


        [HttpPost]
        public async Task<IActionResult> UploadDSC([FromForm] IFormCollection form)

        {
            var file = form.Files["dscFile"];
            string password = form["password"].ToString();
            if (file == null || file.Length == 0 || Path.GetExtension(file.FileName).ToLower() != ".pfx")
            {
                return Json(new { status = false, message = "Invalid file type or no file uploaded." });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var certificateBytes = await ReadStreamToByteArray(stream);

                using var pfx = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.Exportable);
                // if (pfx.Issuer == pfx.Subject)
                // {
                //     return Json(new { status = false, message = "Self-signed certificates are not allowed." });
                // }

                // Check if the certificate is expired
                if (DateTime.UtcNow > pfx.NotAfter)
                {
                    return Json(new { status = false, message = "The certificate has expired." });
                }

                // Check if the certificate is not yet valid
                if (DateTime.UtcNow < pfx.NotBefore)
                {
                    return Json(new { status = false, message = "The certificate is not yet valid." });
                }

                byte[] encryptionKey = encryptionService.GenerateKey();
                byte[] encryptionIV = encryptionService.GenerateIV();
                byte[] encryptedCertificate = encryptionService.EncryptData(certificateBytes, encryptionKey, encryptionIV);
                byte[] encryptedPassword = encryptionService.EncryptData(Encoding.UTF8.GetBytes(password), encryptionKey, encryptionIV);


                SaveDSCToDatabase(encryptedCertificate, encryptedPassword, encryptionKey, encryptionIV);

                return Json(new { status = true, message = "Certificate Registered Properly." });
            }
            catch (CryptographicException ex)
            {
                return BadRequest($"Cryptographic error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing .pfx file: {ex.Message}");
            }
        }


        private static async Task<byte[]> ReadStreamToByteArray(Stream stream)
        {
            using MemoryStream ms = new();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }


        public void SaveDSCToDatabase(byte[] encryptedCertificate, byte[] encryptedPassword, byte[] encryptionKey, byte[] encryptionIV)
        {
            int userId = (int)HttpContext.Session.GetInt32("UserId")!;

            byte[] kek = Convert.FromBase64String(Environment.GetEnvironmentVariable("KEY_ENCRYPTION_KEY")!);
            byte[] encryptedKey = encryptionService.EncryptData(encryptionKey, kek, encryptionIV);


            var certificateDetails = new Certificate
            {
                OfficerId = userId,
                EncryptedCertificateData = encryptedCertificate,
                EncryptedPassword = encryptedPassword,
                EncryptionKey = encryptedKey,
                EncryptionIv = encryptionIV
            };

            dbcontext.Certificates.Add(certificateDetails);
            dbcontext.SaveChanges();

        }


        public void Sign(string src, string dest, Org.BouncyCastle.X509.X509Certificate[] chain, ICipherParameters pk,
                    string digestAlgorithm, PdfSigner.CryptoStandard subfilter, string reason, string location,
                    ICollection<ICrlClient>? crlList, IOcspClient? ocspClient, ITSAClient? tsaClient, int estimatedSize)
        {
            using PdfReader reader = new(src);
            using FileStream fs = new(dest, FileMode.Open);

            PdfSigner signer = new(reader, fs, new StampingProperties());

            // Set the name of the signature field
            signer.SetFieldName("SignatureFieldName");

            // Set the position and page number for the signature
            signer.SetPageRect(new iText.Kernel.Geom.Rectangle(380, 20, 200, 100));  // Rectangle(x, y, width, height)
            signer.SetPageNumber(1);



            // Set reason and location for the signature
            signer.SetReason("Digital Signing");
            signer.SetLocation("Social Welfare Department");

            SignedAppearanceText appearanceText = new();
            appearanceText.SetReasonLine("Reason: " + signer.GetReason());
            appearanceText.SetLocationLine("Department: " + signer.GetLocation());

            // Initialize the appearance object
            SignatureFieldAppearance appearance = new("app");

            // Set up the appearance content
            appearance.SetContent(appearanceText);

            // Set font, color, and size for the appearance text
            appearance.SetFontSize(10);
            appearance.SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD));
            appearance.SetFontColor(new DeviceRgb(0, 0, 0)); // Black color



            // Apply the appearance to the signer
            signer.SetSignatureAppearance(appearance);





            IExternalSignature pks = new PrivateKeySignature(new PrivateKeyBC(pk), digestAlgorithm);
            IX509Certificate[] certificateWrappers = new IX509Certificate[chain.Length];
            for (int i = 0; i < certificateWrappers.Length; ++i)
            {
                certificateWrappers[i] = new X509CertificateBC(chain[i]);
            }

            // Sign the document
            signer.SignDetached(pks, certificateWrappers, crlList, ocspClient, tsaClient, estimatedSize, subfilter);
        }


        public IActionResult SignPdf(string ApplicationId)
        {
            _logger.LogInformation($"-----------------HERE IN SIGN PDF FUNCTION------------------------");

            int userId = (int)HttpContext.Session.GetInt32("UserId")!;
            string inputPdfPath = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "SanctionLetter.pdf");

            var certificate = dbcontext.Certificates.FirstOrDefault(cer => cer.OfficerId == userId);

            byte[] kek = Convert.FromBase64String(Environment.GetEnvironmentVariable("KEY_ENCRYPTION_KEY")!);
            byte[] encryptionKey = encryptionService.DecryptData(certificate!.EncryptionKey, kek, certificate!.EncryptionIv);
            byte[] encryptionIV = certificate.EncryptionIv;

            byte[] certificateBytes = encryptionService.DecryptData(certificate.EncryptedCertificateData, encryptionKey, encryptionIV);
            byte[] certificatePasswordBytes = encryptionService.DecryptData(certificate.EncryptedPassword, encryptionKey, encryptionIV);
            string decryptedPassword = Encoding.UTF8.GetString(certificatePasswordBytes);


            using (var pfxStream = new MemoryStream(certificateBytes))
            {
                Pkcs12Store pkcs12 = new Pkcs12StoreBuilder().Build();
                pkcs12.Load(pfxStream, decryptedPassword.ToCharArray());
                string? alias = null;
                foreach (var a in pkcs12.Aliases)
                {
                    alias = (string)a;
                    if (pkcs12.IsKeyEntry(alias))
                        break;
                }

                ICipherParameters pk = pkcs12.GetKey(alias).Key;
                X509CertificateEntry[] ce = pkcs12.GetCertificateChain(alias);
                Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
                for (int k = 0; k < ce.Length; ++k)
                {
                    chain[k] = ce[k].Certificate;
                }

                Sign(inputPdfPath, inputPdfPath, chain, pk, DigestAlgorithms.SHA256, PdfSigner.CryptoStandard.CMS, "Digital Signing", "JAMMU", null, null, null, 0);

            }

            return Json(new { status = true });
        }


    }
}