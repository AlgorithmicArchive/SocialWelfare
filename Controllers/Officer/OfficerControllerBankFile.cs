using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Renci.SshNet;
using SocialWelfare.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace SocialWelfare.Controllers.Officer
{
    public partial class OfficerController : Controller
    {
        [HttpPost]
        public IActionResult UploadCsv([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;

            string ftpHost = form["ftpHost"].ToString();
            string ftpUser = form["ftpUser"].ToString();
            string ftpPassword = form["ftpPassword"].ToString();
            int serviceId = Convert.ToInt32(form["serviceId"].ToString());
            int districtId = Convert.ToInt32(form["districtId"].ToString());

            var bankFile = dbcontext.BankFiles.FirstOrDefault(bf => bf.ServiceId == serviceId && bf.DistrictId == districtId && bf.FileSent == false);
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "exports", bankFile!.FileName);
            var ftpClient = new SftpClient(ftpHost, 22, ftpUser, ftpPassword);
            ftpClient.Connect();

            if (!ftpClient.IsConnected) return Json(new { status = false, message = "Unable to connect to the SFTP server." });

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                ftpClient.UploadFile(stream, Path.GetFileName(filePath));
            }
            ftpClient.Disconnect();

            bankFile.FileSent = true;
            dbcontext.SaveChanges();

            return Json(new { status = true, message = "File Uploaded Successfully." });
        }

        public IActionResult GetResponseBankFile([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            Models.Entities.User Officer = dbcontext.Users.Find(userId)!;
            var UserSpecificDetails = JsonConvert.DeserializeObject<dynamic>(Officer!.UserSpecificDetails);
            string officerDesignation = UserSpecificDetails!["Designation"]?.ToString() ?? string.Empty;
            string accessLevel = UserSpecificDetails!["AccessLevel"]?.ToString() ?? string.Empty;

            string ftpHost = form["ftpHost"].ToString();
            string ftpUser = form["ftpUser"].ToString();
            string ftpPassword = form["ftpPassword"].ToString();
            int serviceId = Convert.ToInt32(form["serviceId"].ToString());
            int districtId = Convert.ToInt32(form["districtId"].ToString());

            var bankFile = dbcontext.BankFiles.FirstOrDefault(bf => bf.ServiceId == serviceId && bf.DistrictId == districtId);
            
            if(bankFile == null){
                return Json(new{status=false,message="No Bank File for this district."});
            }
            else if(bankFile!=null && bankFile.FileSent==false){
                return Json(new{status=false,message="Bank File not sent."});
            }

            if (!string.IsNullOrEmpty(bankFile!.ResponseFile))
            {
                return Json(new { status = true, file = bankFile.ResponseFile });
            }


            string originalFileName = Path.GetFileNameWithoutExtension(bankFile!.FileName);
            string responseFile = $"{originalFileName}_response.csv";

            var ftpClient = new SftpClient(ftpHost, 22, ftpUser, ftpPassword);
            ftpClient.Connect();

         

            if (!ftpClient.IsConnected)
            {
                return Json(new { status = false, message = "Unable to connect to the SFTP server." });
            }

            if (!ftpClient.Exists(responseFile))
            {
                return Json(new { status = false, message = "No response file received yet." });
            }

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "exports", responseFile);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                ftpClient.DownloadFile(responseFile, stream);
            }

            bankFile.ResponseFile = responseFile;
            dbcontext.SaveChanges();

            return Json(new { status = true, file = bankFile.ResponseFile });

        }

        private async Task UpdateApplicationHistoryAsync(IEnumerable<BankFileModel> bankFileData, string officer, string fileName)
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
        public async Task<IActionResult> BankCsvFile(string serviceId, string districtId)
        {
            int serviceIdInt = Convert.ToInt32(serviceId);
            int districtIdInt = Convert.ToInt32(districtId);
            var service = dbcontext.Services.FirstOrDefault(s=>s.ServiceId == serviceIdInt);
            string staticAmount = service!.Amount.ToString()!;
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null) return Unauthorized();

            var officer = await dbcontext.Users.FindAsync(userId);
            if (officer == null) return NotFound();

            var details = JsonConvert.DeserializeObject<dynamic>(officer.UserSpecificDetails);
            string officerDesignation = details?.Designation!;
            int accessCode = Convert.ToInt32(details?.AccessCode);

            var recordsCount = await dbcontext.RecordCounts
                .FirstOrDefaultAsync(rc => rc.ServiceId == serviceIdInt && rc.Officer == officerDesignation && rc.AccessCode == accessCode);

            var bankFile = await dbcontext.BankFiles
                .FirstOrDefaultAsync(bf => bf.ServiceId == serviceIdInt && bf.DistrictId == districtIdInt && bf.FileSent == false);

            var district = await dbcontext.Districts
                .FirstOrDefaultAsync(d => d.DistrictId == districtIdInt);

            if (district == null) return NotFound("District not found");

            // Ensure the exports directory exists
            string webRootPath = _webHostEnvironment.WebRootPath;
            string exportsFolder = Path.Combine(webRootPath, "exports");

            Directory.CreateDirectory(exportsFolder);

            string fileName = bankFile?.FileName ?? $"{district.DistrictShort}_BankFile_{DateTime.Now:ddMMMyyyyhhmm}.csv";
            string filePath = Path.Combine(exportsFolder, fileName);

            // Notify the start of the process
            await hubContext.Clients.All.SendAsync("ReceiveProgress", 0);

            string DepartmentName = "Social Welfare";
            string DebitAccountNumber = "01234567890123456";
            string DebitBankName = "THE JAMMU AND KASHMIR BANK";
            string DebitIfsc = "JAKA0KEEPER";
            string DistrictShort = dbcontext.Districts.FirstOrDefault(d => d.DistrictId == districtIdInt)!.DistrictShort;
            string MonthShort = DateTime.Now.ToString("MMMyyyy").ToUpper();
            int count=0;
            var uniqueId = dbcontext.UniqueIdtables.FirstOrDefault(u=>u.DistrictNameShort==DistrictShort && u.MonthShort==MonthShort);
            if(uniqueId!=null)
                count = uniqueId.LastSequentialNumber +1;
            else count = 1;

            if (uniqueId == null)
            {
                // Handle the case where uniqueId is not found, perhaps create a new entry or return an error
                uniqueId = new UniqueIdtable
                {
                    DistrictNameShort = DistrictShort,
                    MonthShort = MonthShort,
                    LastSequentialNumber = 1 // Initialize with zero or another value if necessary
                };
                dbcontext.UniqueIdtables.Add(uniqueId);
                await dbcontext.SaveChangesAsync(); // Save the new uniqueId record to the database
            }

            count = uniqueId.LastSequentialNumber + 1;
            dbcontext.SaveChanges();
            string formattedNumber = count.ToString().PadLeft(12,'0');
            string UniqueID = DistrictShort+MonthShort+formattedNumber;
            // Fetch data using the stored procedure
            var bankFileData = await dbcontext.BankFileModels
                .FromSqlRaw("EXEC GetBankFileData @ServiceId, @DistrictId, @DepartmentName, @DebitAccountNumber, @StaticAmount, @FileCreationDate, @DebitBankName, @DebitIfsc, @UniqueId",
                            new SqlParameter("@ServiceId", serviceIdInt),
                            new SqlParameter("@DistrictId", districtId),
                            new SqlParameter("@DepartmentName", DepartmentName),
                            new SqlParameter("@DebitAccountNumber", DebitAccountNumber),
                            new SqlParameter("@StaticAmount", staticAmount),
                            new SqlParameter("@FileCreationDate", DateTime.Now.ToString("dd MMM yyyy hh:mm tt")),
                            new SqlParameter("@DebitBankName", DebitBankName),
                            new SqlParameter("@DebitIfsc", DebitIfsc),
                            new SqlParameter("@UniqueId", UniqueID)
                            )
                .AsNoTracking()
                .ToListAsync();

            await UpdateApplicationHistoryAsync(bankFileData, officerDesignation!, fileName);

            int totalRecords = bankFileData.Count;
            int batchSize = 1000; // Adjust the batch size as needed
            int processedRecords = 0;

            using (var streamWriter = new StreamWriter(filePath, append: true))
            using (var csvWriter = new CsvWriter(streamWriter, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false // Do not include headers
            }))
            {
                while (processedRecords < totalRecords)
                {
                    var batch = bankFileData.Skip(processedRecords).Take(batchSize);
                    await csvWriter.WriteRecordsAsync(batch);

                    processedRecords += batch.Count();
                    int progress = (int)(processedRecords / (double)totalRecords * 100);
                    await hubContext.Clients.All.SendAsync("ReceiveProgress", progress);
                }
            }


            // Notify completion
            await hubContext.Clients.All.SendAsync("ReceiveProgress", 100);

            if (recordsCount != null)
            {
                recordsCount.Sanction -= totalRecords;
            }

            if (bankFile == null)
            {
                var newBankFile = new BankFile
                {
                    ServiceId = serviceIdInt,
                    DistrictId = districtIdInt,
                    FileName = fileName,
                    GeneratedDate = DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
                    TotalRecords = totalRecords,
                    FileSent = false,
                    ResponseFile = ""
                };
                dbcontext.BankFiles.Add(newBankFile);
            }
            else
            {
                bankFile.TotalRecords += totalRecords;
                bankFile.GeneratedDate = DateTime.Now.ToString("dd MMM yyyy hh:mm tt");
            }

            await dbcontext.SaveChangesAsync();

            return Json(new { filePath = $"/exports/{fileName}" });
        }
        public IActionResult IsBankFile(string serviceId, string districtId)
        {
            int ServiceId = Convert.ToInt32(serviceId);
            int DistrictId = Convert.ToInt32(districtId);
            var bankFile = dbcontext.BankFiles.FirstOrDefault(bf => bf.ServiceId == ServiceId && bf.DistrictId == DistrictId && bf.FileSent == false);
            var newRecords = dbcontext.Applications
            .FromSqlRaw("SELECT * FROM Applications WHERE ApplicationStatus = 'Sanctioned' AND JSON_VALUE(ServiceSpecific, '$.District') = {0}", districtId)
             .ToList();

            return Json(new { bankFile, newRecords = newRecords.Count });
        }


    }
}