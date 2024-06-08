using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers
{
    public class BaseController : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext;
        protected readonly ILogger<BaseController> _logger;

        public BaseController(SocialWelfareDepartmentContext dbcontext, ILogger<BaseController> logger)
        {
            this.dbcontext = dbcontext;
            this._logger = logger;
        }

        public IActionResult UsernameAlreadyExist(string Username)
        {
            var isUsernameInCitizens = dbcontext.Citizens.FirstOrDefault(u => u.Username == Username);
            var isUsernameInOfficers = dbcontext.Citizens.FirstOrDefault(u => u.Username == Username);

            if (isUsernameInCitizens == null && isUsernameInOfficers == null)
                return Json(new { status = false });
            else
                return Json(new { status = true });
        }

        public IActionResult EmailAlreadyExist(string email)
        {
            var isEmailInCitizens = dbcontext.Citizens.FirstOrDefault(u => u.Email == email);
            var isEmailInOfficers = dbcontext.Citizens.FirstOrDefault(u => u.Email == email);

            if (isEmailInCitizens == null && isEmailInOfficers == null)
                return Json(new { status = false });
            else
                return Json(new { status = true });
        }

        public IActionResult MobileNumberAlreadyExist(string MobileNumber)
        {
            var isMobileNumberInCitizens = dbcontext.Citizens.FirstOrDefault(u => u.MobileNumber == MobileNumber);
            var isMobileNumberInOfficers = dbcontext.Citizens.FirstOrDefault(u => u.MobileNumber == MobileNumber);

            if (isMobileNumberInCitizens == null && isMobileNumberInOfficers == null)
                return Json(new { status = false });
            else
                return Json(new { status = true });
        }

        public IActionResult IsOldPasswordValid(string Password)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            var isPasswordInCitizens = dbcontext.Citizens.FromSqlRaw("EXEC IsOldPasswordValid @UserId,@Password,@TableName", new SqlParameter("@UserId", userId), new SqlParameter("@Password", Password), new SqlParameter("@TableName", "Citizens")).ToList();

            var isPasswordInOfficers = dbcontext.Officers.FromSqlRaw("EXEC IsOldPasswordValid @UserId,@Password,@TableName", new SqlParameter("@UserId", userId), new SqlParameter("@Password", Password), new SqlParameter("@TableName", "Officers")).ToList();

            if (isPasswordInCitizens!.Count == 0 && isPasswordInOfficers!.Count == 0)
            {
                return Json(new { status = false });
            }

            return Json(new { status = true });
        }
    }
}