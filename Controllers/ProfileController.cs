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
    public class ProfileController(SocialWelfareDepartmentContext dbcontext, ILogger<ProfileController> logger, UserHelperFunctions _helper) : Controller
    {

        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<ProfileController> _logger = logger;

        protected readonly UserHelperFunctions helper = _helper;

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");
            string? username = dbcontext.Users.FirstOrDefault(u => u.UserId == userId)!.Username;
            ViewData["UserType"] = userType;
            ViewData["UserName"] = username;

        }

        [HttpGet]
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");

            if (userId.HasValue && !string.IsNullOrEmpty(userType))
            {
                var userDetails = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
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



            dbcontext.Database.ExecuteSqlRaw("EXEC UpdateCitizenDetail @ColumnName,@ColumnValue,@TableName,@CitizenId", new SqlParameter("@ColumnName", columnName), new SqlParameter("@ColumnValue", columnValue), new SqlParameter("@TableName", TableName), new SqlParameter("@CitizenId", userId));

            return Json(new { status = true, url = "/Profile/Index" });
        }

        [HttpGet]
        public IActionResult GenerateBackupCodes()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? TableName = "Users";


            var unused = helper.GenerateUniqueRandomCodes(10, 8);
            var backupCodes = new
            {
                unused,
                used = Array.Empty<string>(),
            };

            var bankupCodesParam = new SqlParameter("@ColumnValue", JsonConvert.SerializeObject(backupCodes));


            dbcontext.Database.ExecuteSqlRaw("EXEC UpdateCitizenDetail @ColumnName,@ColumnValue,@TableName,@CitizenId", new SqlParameter("@ColumnName", "BackupCodes"), bankupCodesParam, new SqlParameter("@TableName", TableName), new SqlParameter("@CitizenId", userId));

            return Json(new { status = true, url = "/Profile/Settings" });


        }

        [HttpGet]
        public IActionResult Settings()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");

            if (userId.HasValue && !string.IsNullOrEmpty(userType))
            {
                var userDetails = dbcontext.Users.FirstOrDefault(u => u.UserId == userId);
                if (userType == "Admin") ViewData["Layout"] = "_AdminLayout";

                if (userDetails != null) return View(userDetails);

            }
            return RedirectToAction("Error", "Home");
        }
    }
}