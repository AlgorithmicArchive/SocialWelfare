using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers
{
    public class BaseController : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext;
        protected readonly ILogger<BaseController> _logger;

        private const long MinImageFile = 20 * 1024;  // 20KB
        private const long MaxImageFile = 50 * 1024;  // 50KB
        private const long MinPdfFile = 100 * 1024; // 100KB
        private const long MaxPdfFile = 200 * 1024; // 200KB

        public BaseController(SocialWelfareDepartmentContext dbcontext, ILogger<BaseController> logger)
        {
            this.dbcontext = dbcontext;
            this._logger = logger;
        }

        public IActionResult UsernameAlreadyExist(string Username)
        {
            var isUsernameInUsers = dbcontext.Users.FirstOrDefault(u => u.Username == Username);

            if (isUsernameInUsers == null)
                return Json(new { status = false });
            else
                return Json(new { status = true });
        }

        public IActionResult EmailAlreadyExist(string email)
        {
            var isEmailInUsers = dbcontext.Users.FirstOrDefault(u => u.Email == email);

            if (isEmailInUsers == null)
                return Json(new { status = false });
            else
                return Json(new { status = true });
        }

        public IActionResult MobileNumberAlreadyExist(string MobileNumber)
        {
            var isMobileNumberInUsers = dbcontext.Users.FirstOrDefault(u => u.MobileNumber == MobileNumber);

            if (isMobileNumberInUsers == null)
                return Json(new { status = false });
            else
                return Json(new { status = true });
        }

        public IActionResult IsOldPasswordValid(string Password)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            var isPasswordInUsers = dbcontext.Users.FromSqlRaw("EXEC IsOldPasswordValid @UserId,@Password,@TableName", new SqlParameter("@UserId", userId), new SqlParameter("@Password", Password), new SqlParameter("@TableName", "Users")).ToList();

            if (isPasswordInUsers!.Count == 0)
            {
                return Json(new { status = false });
            }

            return Json(new { status = true });
        }

        public int GetCount(string type, Dictionary<string, string> conditions)
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

            int count = dbcontext.Applications.FromSqlRaw("EXEC GetApplications @Condition1, @Condition2",
        new SqlParameter("@Condition1", Condition1.ToString()),
        new SqlParameter("@Condition2", Condition2.ToString())).ToList().Count;

            return count;
        }

        public IActionResult GetFilteredCount(string? conditions)
        {
            var Conditions = JsonConvert.DeserializeObject<Dictionary<string, string>>(conditions!);
            int TotalCount = GetCount("Total", Conditions!);
            int PendingCount = GetCount("Pending", Conditions!);
            int RejectCount = GetCount("Reject", Conditions!);
            int SanctionCount = GetCount("Sanction", Conditions!);

            return Json(new { status = true, TotalCount, PendingCount, RejectCount, SanctionCount });
        }

        [HttpGet]
        public IActionResult GetDistricts()
        {
            var districts = dbcontext.Districts.ToList();
            return Json(new { status = true, districts });
        }

        [HttpGet]
        public IActionResult GetDesignations()
        {
            // JsonConvert.DeserializeObject
            var designations = dbcontext.OfficersDesignations.Where(des => !des.Designation.Contains("Admin")).ToList();
            return Json(new { status = true, designations });
        }

        [HttpGet]
        public IActionResult GetServices()
        {
            var services = dbcontext.Services.Where(ser => ser.Active == true).ToList();
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

        [HttpGet]
        public IActionResult IsDuplicateAccNo(string accNo, string applicationId)
        {
            var application = dbcontext.Applications.FromSqlRaw("EXEC GetDuplicateAccNo @AccountNo", new SqlParameter("@AccountNo", accNo)).ToList();

            if (application.Count == 0)
                return Json(new { status = false });
            else if (application[0].ApplicationId == applicationId)
                return Json(new { status = false });
            else
            {
                if (application[0].ApplicationStatus == "Rejected") return Json(new { status = false });
                else return Json(new { status = true });
            }
        }

        [HttpPost]
        public IActionResult Validate([FromForm] IFormCollection file)
        {
            // Ensure a file is provided
            if (file.Files.Count == 0)
            {
                return Json(new { isValid = false, errorMessage = "No file uploaded." });
            }

            var uploadedFile = file.Files[0];
            string fileType = file["fileType"].ToString();

            using (var fileStream = uploadedFile.OpenReadStream())
            {
                byte[] fileHeader = new byte[4];
                fileStream.Read(fileHeader, 0, 4); // Read first 4 bytes of the file

                string fileExtension = Path.GetExtension(uploadedFile.FileName)?.ToLower()!;

                // Check if the file type is an image
                if (fileType == "image")
                {
                    if (!IsValidImage(fileHeader, fileExtension))
                    {
                        return Json(new { isValid = false, errorMessage = "The uploaded file is not a valid image." });
                    }

                    // If it's a valid image, check the file size
                    if (uploadedFile.Length < MinImageFile || uploadedFile.Length > MaxImageFile)
                    {
                        return Json(new { isValid = false, errorMessage = "Image file size must be between 20KB and 50KB." });
                    }
                }
                // Check if the file type is a PDF
                else if (fileType == "pdf")
                {
                    if (!IsValidPdf(fileHeader, fileExtension))
                    {
                        return Json(new { isValid = false, errorMessage = "The uploaded file is not a valid PDF." });
                    }

                    // If it's a valid PDF, check the file size
                    if (uploadedFile.Length < MinPdfFile || uploadedFile.Length > MaxPdfFile)
                    {
                        return Json(new { isValid = false, errorMessage = "PDF file size must be between 100KB and 200KB." });
                    }
                }
                else
                {
                    return Json(new { isValid = false, errorMessage = "Unsupported file type." });
                }
            }

            // If all checks pass, return success
            return Json(new { isValid = true, message = "" });
        }

        private bool IsValidImage(byte[] header, string fileExtension)
        {
            // PNG: 89 50 4E 47 (hex) / JPG: FF D8 FF E0 or FF D8 FF E1
            if (fileExtension == ".png" && header[0] == 0x89 && header[1] == 0x50 &&
                header[2] == 0x4E && header[3] == 0x47)
            {
                return true;
            }

            if (fileExtension == ".jpg" || fileExtension == ".jpeg")
            {
                return header[0] == 0xFF && header[1] == 0xD8 && (header[2] == 0xFF);
            }

            return false;
        }

        private bool IsValidPdf(byte[] header, string fileExtension)
        {
            // PDF files start with: 25 50 44 46 (hex)
            return fileExtension == ".pdf" && header[0] == 0x25 && header[1] == 0x50 &&
                header[2] == 0x44 && header[3] == 0x46;
        }

    }
}