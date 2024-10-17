using DocumentFormat.OpenXml.EMMA;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        public dynamic PendingApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
        {
            _logger.LogInformation($"Entries: Officer: {Officer} Start: {start} Length :{length} Type:{type} ServiceID:{serviceId}");
            
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = UserSpecificDetails["AccessCode"].ToString();

            // Convert poolList and approveList to comma-separated strings
            var arrayLists = dbcontext.ApplicationLists
                .FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));
            
       string poolList = arrayLists?.PoolList != null && JsonConvert.DeserializeObject<List<string>>(arrayLists.PoolList)!.Count > 0
            ? string.Join(",", JsonConvert.DeserializeObject<List<string>>(arrayLists.PoolList)!)
            : string.Empty;

        string approveList = arrayLists?.ApprovalList != null && JsonConvert.DeserializeObject<List<string>>(arrayLists.ApprovalList)!.Count > 0
            ? string.Join(",", JsonConvert.DeserializeObject<List<string>>(arrayLists.ApprovalList)!)
            : string.Empty;

            // Call the stored procedure to get all the data
            var applicationList = dbcontext.Applications
                .FromSqlRaw(@"EXEC GetPendingApplications 
                                @serviceId = {0}, 
                                @officerDesignation = {1}, 
                                @accessLevel = {2}, 
                                @accessCode = {3}, 
                                @poolList = {4}, 
                                @approveList = {5}",
                            serviceId, officerDesignation, accessLevel, accessCode, poolList, approveList)
                .ToList();

        

            // Get total pending applications
            var totalPending = applicationList.Count;

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

            // Prepare the PendingList for output
            List<dynamic> PendingList = new List<dynamic>();
            int pendingIndex = 1;

            foreach (var application in applicationList)
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);
                var button = new
                {
                    function = "UserDetails",
                    parameters = new[] { application.ApplicationId },
                    buttonText = "View"
                };

                var pendingData = new List<dynamic>
                {
                    pendingIndex,
                    $"<input type='checkbox' class='form-check pending-element' value='{application.ApplicationId}' name='pending-element' />",
                    application.ApplicationId,
                    application.ApplicantName,
                    serviceSpecific["DateOfMarriage"],
                    application.SubmissionDate!,
                    JsonConvert.SerializeObject(button)
                };
                
                PendingList.Add(pendingData);
                pendingIndex++;
            }

            var obj = new
            {
                PendingList = new
                {
                    data = PendingList.Skip(start).Take(length),
                    columns = pendingColumns,
                    recordsTotal = totalPending, // Total records (before pagination)
                    recordsFiltered = PendingList.Count // Filtered records (after pagination)
                },
                PoolCount = poolList == "" ? 0 : poolList.Split(',').Length,
                ApproveCount =approveList == "" ? 0 : approveList.Split(',').Length,
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
            // Extract officer-specific details from JSON
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);

            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = UserSpecificDetails!["AccessCode"].ToString();

            // Fetch the pool list from the database based on officer and service
            var arrayLists = dbcontext.ApplicationLists
                .FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));

            // Convert the poolList to a comma-separated string to pass to the stored procedure
            string poolList = arrayLists?.PoolList != null && JsonConvert.DeserializeObject<List<string>>(arrayLists.PoolList)!.Count > 0
                ? string.Join(",", JsonConvert.DeserializeObject<List<string>>(arrayLists.PoolList)!)
                : string.Empty;

            // Call the stored procedure to retrieve applications from the pool
            var applicationList = dbcontext.Applications
                .FromSqlRaw(@"EXEC GetPoolApplications 
                                @serviceId = {0}, 
                                @officerDesignation = {1}, 
                                @accessLevel = {2}, 
                                @accessCode = {3}, 
                                @poolList = {4}",
                            serviceId, officerDesignation, accessLevel, accessCode, poolList)
                .ToList();  // No need to apply further filtering since it's already done in the stored procedure.

            // Fetch officer's role in the service
            var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
            var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);

            bool canSanction = officer!["canSanction"];
            bool canUpdate = officer["canUpdate"];

            // Prepare the PoolList and columns for the response
            List<dynamic> PoolList = new List<dynamic>();
            var poolColumns = new List<dynamic>
            {
                new { title = "S.No" },
                new { title = "Choose <input type='checkbox' class='form-check poolList-parent' value='' name='poolList-parent' />" },
                new { title = "Reference Number" },
                new { title = "Applicant Name" },
                new { title = "Date Of Marriage" },
                new { title = "Submission Date" },
                new { title = "Action" }
            };

            int poolIndex = 1;
            foreach (var application in applicationList)
            {
                var (userDetails, preAddressDetails, perAddressDetails, serviceSpecific, bankDetails) = helper.GetUserDetailsAndRelatedData(application.ApplicationId);

                List<dynamic> poolData = new List<dynamic>
                {
                    poolIndex,
                    $"<input type='checkbox' class='form-check poolList-element' value='{application.ApplicationId}' name='poolList-element' />",
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
                    $"<button class='btn btn-dark w-100' onclick=UserDetails('{application.ApplicationId}');>View</button>"
                };

                // Insert DateOfMarriage if it exists, otherwise remove the column
                if (serviceSpecific["DateOfMarriage"] != null)
                    poolData.Insert(4, serviceSpecific["DateOfMarriage"]);
                else
                    poolColumns.RemoveAt(poolColumns.Count - 2);

                PoolList.Add(poolData);
                poolIndex++;
            }

            // Return the result
            var obj = new
            {
                PoolList = new
                {
                    data = PoolList.Skip(start).Take(length), // Apply pagination
                    columns = poolColumns,
                    recordsTotal = poolList.Split(',').Length, // Total pool list count
                    recordsFiltered = PoolList.Count // Filtered record count after pagination
                },
                Type = type,
                ServiceId = serviceId
            };

            return !AllData ? obj : new { columns = poolColumns, data = PoolList };
        }
        public dynamic ApproveApplications(Models.Entities.User Officer, int start, int length, string type, int serviceId, bool AllData = false)
        {
            // Extract officer-specific details from JSON
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            string accessCode = UserSpecificDetails!["AccessCode"].ToString();

            // Fetch the approval list from the database based on officer and service
            var arrayLists = dbcontext.ApplicationLists
                .FirstOrDefault(list => list.ServiceId == serviceId && list.Officer == officerDesignation && list.AccessLevel == accessLevel && list.AccessCode == Convert.ToInt32(accessCode));

            // Convert the approvalList to a comma-separated string to pass to the stored procedure
            string approvalList = arrayLists?.ApprovalList != null && JsonConvert.DeserializeObject<List<string>>(arrayLists.ApprovalList)!.Count > 0
                ? string.Join(",", JsonConvert.DeserializeObject<List<string>>(arrayLists.ApprovalList)!)
                : string.Empty;

            // Call the stored procedure to retrieve applications from the approval list
            var applicationList = dbcontext.Applications
                .FromSqlRaw(@"EXEC GetApproveApplications 
                                @serviceId = {0}, 
                                @officerDesignation = {1}, 
                                @accessLevel = {2}, 
                                @accessCode = {3}, 
                                @approvalList = {4}",
                            serviceId, officerDesignation, accessLevel, accessCode, approvalList)
                .ToList();  // No need to apply further filtering since it's already done in the stored procedure

            // Fetch officer's role in the service
            var service = dbcontext.Services.FirstOrDefault(u => u.ServiceId == serviceId);
            var workForceOfficers = JsonConvert.DeserializeObject<IEnumerable<dynamic>>(service!.WorkForceOfficers!);
            var officer = workForceOfficers!.FirstOrDefault(o => o["Designation"] == officerDesignation);

            bool canSanction = officer!["canSanction"];
            bool canUpdate = officer["canUpdate"];

            // Prepare the ApproveList and columns for the response
            List<dynamic> ApproveList = new List<dynamic>();
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

                List<dynamic> approveData = new List<dynamic>
                {
                    approveIndex,
                    $"<input type='checkbox' class='form-check approve-element' value='{application.ApplicationId}' name='approve-element' />",
                    application.ApplicationId,
                    application.ApplicantName,
                    appliedDistrict,
                    application.RelationName + $" ({application.Relation.ToUpper()})",
                    serviceSpecific["MotherName"],
                    application.DateOfBirth,
                    $"{bankDetails["BankName"]}/{bankDetails["IfscCode"]}/{bankDetails["AccountNumber"]}",
                    $"{preAddressDetails.Address!.ToUpper()}, TEHSIL:{preAddressDetails.Tehsil!.ToUpper()}, DISTRICT:{preAddressDetails.District!.ToUpper()}, PINCODE:{preAddressDetails.Pincode}",
                    application.SubmissionDate!
                };

                if (serviceSpecific["DateOfMarriage"] != null)
                    approveData.Insert(approveData.Count - 2, serviceSpecific["DateOfMarriage"]);
                else
                    approveColumns.RemoveAt(approveColumns.Count - 2);

                ApproveList.Add(approveData);
                approveIndex++;
            }

            // Return the result
            var obj = new
            {
                ApproveList = new
                {
                    data = ApproveList.Skip(start).Take(length), // Apply pagination
                    columns = approveColumns,
                    recordsTotal = approvalList.Split(',').Length, // Total approval list count
                    recordsFiltered = ApproveList.Count // Filtered record count after pagination
                },
                Type = type,
                ServiceId = serviceId
            };

            _logger.LogInformation($"------------AllDATA: {AllData}-----------------------------");

            return !AllData ? obj : new { columns = approveColumns, data = ApproveList };
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

                 var button = new{
                    function = "PullApplication",
                    parameters = new[] { application.ApplicationId },
                    buttonText="Pull"
                };

                List<dynamic> sentData = [
                    index+1,
                    application.ApplicationId,
                    application.ApplicantName,
                    application.SubmissionDate!,
                    canPull?JsonConvert.SerializeObject(button):"Cannot Pull"
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
                // BankFile = 
            };

            if (!AllData)
                return obj;
            else return new { columns = sanctionColumns, data = SanctionApplications };
        }
        public dynamic RejectApplications(Models.Entities.User Officer, int start, int length, string type, int? serviceId, bool AllData = false)
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

                RejectList = new
                {
                    data = RejectApplications.AsEnumerable().Skip(start).Take(length),
                    columns = RejectColumns,
                    recordsTotal = RejectApplications.Count,
                    recordsFiltered = RejectApplications.AsEnumerable().Skip(start).Take(length).ToList().Count
                },
                Type = type,
                ServiceId = serviceId
            };

            if (!AllData)
                return obj;
            else return new { columns = RejectColumns, data = RejectApplications };
        }

        public IActionResult GetTableRecords(int start, int length, string type, int totalCount, int serviceId)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer?.UserSpecificDetails!);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;
            int accessCode = Convert.ToInt32(UserSpecificDetails!["AccessCode"].ToString());
            List<dynamic> ApplicationList = [];
            var applications = dbcontext.Applications.Join(dbcontext.CurrentPhases,
                  app => app.ApplicationId,
                  cp => cp.ApplicationId,
                  (app, cp) => new { CurrentPhase = cp, Application = app })
                  .Where(x => (type == "Total" || x.CurrentPhase.ActionTaken == type)
                    && x.CurrentPhase.Officer == officerDesignation
                    && x.CurrentPhase.AccessCode == accessCode)
                  .Select(x => x.Application)
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


    }
}