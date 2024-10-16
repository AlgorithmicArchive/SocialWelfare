using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Profile
{
    [Authorize(Roles = "Citizen,Officer,Admin")]
    public class ProfileController : Controller
    {
        private readonly SocialWelfareDepartmentContext _dbcontext;
        private readonly ILogger<ProfileController> _logger;
        private readonly UserHelperFunctions _helper;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Constructor
        public ProfileController(SocialWelfareDepartmentContext dbcontext, ILogger<ProfileController> logger, UserHelperFunctions helper, IWebHostEnvironment webHostEnvironment)
        {
            _dbcontext = dbcontext;
            _logger = logger;
            _helper = helper;
            _webHostEnvironment = webHostEnvironment;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");
            var User = _dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            string Profile = JsonConvert.DeserializeObject<dynamic>(User!.UserSpecificDetails)!.Profile;
            ViewData["UserType"] = userType;
            ViewData["UserName"] = User!.Username;
            ViewData["Profile"]= Profile;
        }

        [HttpGet]
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");

            if (userId.HasValue && !string.IsNullOrEmpty(userType))
            {
                var userDetails = _dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
                return View(userDetails);
            }
            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public IActionResult UpdateColumn([FromForm] IFormCollection form)
        {
            string? columnName = form["columnName"].ToString();
            string? columnValue = form["columnValue"].ToString();
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");
            string? TableName = "";

            if (userType == "Citizen")
                TableName = "Citizens";
            else if (userType == "Officer")
                TableName = "Officers";

            _dbcontext.Database.ExecuteSqlRaw("EXEC UpdateCitizenDetail @ColumnName,@ColumnValue,@TableName,@CitizenId", new SqlParameter("@ColumnName", columnName), new SqlParameter("@ColumnValue", columnValue), new SqlParameter("@TableName", TableName), new SqlParameter("@CitizenId", userId));

            return Json(new { status = true, url = "/Profile/Index" });
        }

        [HttpGet]
        public IActionResult GenerateBackupCodes()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? TableName = "Users";

            var unused = _helper.GenerateUniqueRandomCodes(10, 8);
            var backupCodes = new
            {
                unused,
                used = Array.Empty<string>(),
            };

            var backupCodesParam = new SqlParameter("@ColumnValue", JsonConvert.SerializeObject(backupCodes));

            _dbcontext.Database.ExecuteSqlRaw("EXEC UpdateCitizenDetail @ColumnName,@ColumnValue,@TableName,@CitizenId", new SqlParameter("@ColumnName", "BackupCodes"), backupCodesParam, new SqlParameter("@TableName", TableName), new SqlParameter("@CitizenId", userId));

            return Json(new { status = true, url = "/Profile/Settings" });
        }

        [HttpGet]
        public IActionResult Settings()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");

            if (userId.HasValue && !string.IsNullOrEmpty(userType))
            {
                var userDetails = _dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
                if (userType == "Admin") ViewData["Layout"] = "_AdminLayout";

                if (userDetails != null) return View(userDetails);
            }
            return RedirectToAction("Error", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> ChangeImage([FromForm] IFormCollection image)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            var user = _dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                return Json(new { isValid = false, errorMessage = "User not found." });
            }

            if (image.Files.Count == 0)
            {
                return Json(new { isValid = false, errorMessage = "No file uploaded." });
            }

            var uploadedFile = image.Files[0];

            var UserSpecific = JsonConvert.DeserializeObject<dynamic>(user.UserSpecificDetails);
            if (UserSpecific != null)
            {
                if (UserSpecific.Profile != null && !string.IsNullOrEmpty((string)UserSpecific.Profile))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath, UserSpecific.Profile.ToString().TrimStart('/'));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(existingFilePath);
                            _logger.LogInformation($"Existing file {existingFilePath} deleted.");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error deleting file {existingFilePath}: {ex.Message}");
                        }
                    }
                }

                var filePath = await _helper.GetFilePath(uploadedFile,"profile");

                UserSpecific.Profile = filePath;

                user.UserSpecificDetails = JsonConvert.SerializeObject(UserSpecific);
                _dbcontext.SaveChanges();

                return Json(new { isValid = true, filePath });
            }
            else
            {
                _logger.LogInformation("UserSpecific is null.");
                return Json(new { isValid = false, errorMessage = "User-specific details are missing." });
            }
        }
    }
}
