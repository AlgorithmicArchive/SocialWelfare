using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SendEmails;
using SocialWelfare.Models;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SocialWelfareDepartmentContext _dbContext;
        private readonly OtpStore _otpStore;
        private readonly EmailSender _emailSender;
        private readonly UserHelperFunctions _helper;
        private readonly PdfService _pdfService;

        public HomeController(ILogger<HomeController> logger, SocialWelfareDepartmentContext dbContext, OtpStore otpStore, EmailSender emailSender, UserHelperFunctions helper, PdfService pdfService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _otpStore = otpStore;
            _emailSender = emailSender;
            _helper = helper;
            _pdfService = pdfService;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            ViewData["UserType"] = "";
        }

        public static string GenerateOTP(int length)
        {
            if (length < 4 || length > 10)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be between 4 and 10.");

            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => random.Next(0, 10).ToString()[0]).ToArray());
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Authentication()
        {
            return View();
        }

        public IActionResult OfficerRegistration()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> OfficerRegistration([FromForm] IFormCollection form)
        {
            var username = new SqlParameter("@Username", form["Username"].ToString());
            var password = new SqlParameter("@Password", form["Password"].ToString());
            var email = new SqlParameter("@Email", form["Email"].ToString());
            var mobileNumber = new SqlParameter("@MobileNumber", form["MobileNumber"].ToString());
            var designation = new SqlParameter("@Designation", form["designation"].ToString());
            var divisionCode = new SqlParameter("@DivisionCode", 1);
            var districtCode = new SqlParameter("@DistrictCode", Convert.ToInt32(form["District"].ToString()));
            var tehsilCode = new SqlParameter("@TehsilCode", Convert.ToInt32(form["Tehsil"].ToString()));

            var used = _helper.GenerateUniqueRandomCodes(10, 8);
            var backupCodes = new
            {
                used,
                unused = new string[0],
            };

            var backupCodesParam = new SqlParameter("@BackupCodes", JsonConvert.SerializeObject(backupCodes));

            var result = _dbContext.Officers.FromSqlRaw("EXEC RegisterOfficer @Username,@Email,@Password,@MobileNumber,@Designation,@DivisionCode,@DistrictCode,@TehsilCode,@BackupCodes", username, email, password, mobileNumber, designation, divisionCode, districtCode, tehsilCode, backupCodesParam).ToList();

            if (result.Count > 0)
            {
                string otp = GenerateOTP(6);
                _otpStore.StoreOtp("registration", otp);
                await _emailSender.SendEmail(form["Email"].ToString(), "OTP For Registration.", otp);
                return Json(new { status = true, result[0].OfficerId });
            }
            else
            {
                return Json(new { status = false, response = "Registration failed." });
            }
        }

        public async Task<IActionResult> Login(IFormCollection form)
        {
            var username = new SqlParameter("Username", form["Username"].ToString());
            SqlParameter password = !string.IsNullOrEmpty(form["Password"]) ? new SqlParameter("Password", form["Password"].ToString()) : null!;

            var isCitizen = _dbContext.Citizens.FromSqlRaw("EXEC UserLogin @Username, @Password, @TableName", username, password, new SqlParameter("TableName", "Citizens")).ToList();
            var isOfficer = _dbContext.Officers.FromSqlRaw("EXEC UserLogin @Username, @Password, @TableName", username, password, new SqlParameter("TableName", "Officers")).ToList();
            var isAdmin = _dbContext.Admins.FromSqlRaw("EXEC UserLogin @Username, @Password, @TableName", username, password, new SqlParameter("TableName", "Admins")).ToList();


            int userId;
            string? url = "";

            if (isCitizen.Count != 0)
            {
                userId = isCitizen[0].CitizenId;
                HttpContext.Session.SetString("UserType", "Citizen");
                url = "/Home/Verification";
            }
            else if (isOfficer.Count != 0)
            {
                userId = isOfficer[0].OfficerId;
                HttpContext.Session.SetString("Designation", isOfficer[0].Designation);
                HttpContext.Session.SetString("UserType", "Officer");
                url = "/Home/Verification";
            }
            else if (isAdmin.Count != 0)
            {
                userId = isAdmin[0].Uuid;
                HttpContext.Session.SetString("UserType", "Admin");
                url = "/Admin/Dashboard";
                List<Claim> claims = [new Claim(ClaimTypes.NameIdentifier, isAdmin[0].Username)];
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Sign in the user with a persistent cookie
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Create a persistent cookie
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // Set the cookie expiration to 30 days
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            }
            else
            {
                return Json(new { status = false, response = "Invalid Username or Password." });
            }


            HttpContext.Session.SetInt32("UserId", userId);
            HttpContext.Session.SetString("Username", form["Username"].ToString());

            return Json(new { status = true, url });
        }

        public IActionResult Verification()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SendOtp()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");

            if (userId != null && userType != null)
            {
                string email = userType == "Citizen" ? _dbContext.Citizens.FirstOrDefault(u => u.CitizenId == userId)?.Email! : _dbContext.Officers.FirstOrDefault(u => u.OfficerId == userId)?.Email!;

                if (!string.IsNullOrEmpty(email))
                {
                    string otp = GenerateOTP(6);
                    _otpStore.StoreOtp("verification", otp);
                    await _emailSender.SendEmail(email, "OTP For Registration.", otp);
                }
            }

            return Json(new { status = true });
        }

        [HttpPost]
        public async Task<IActionResult> Verification([FromForm] IFormCollection form)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? userType = HttpContext.Session.GetString("UserType");
            string? Username = HttpContext.Session.GetString("Username");
            string otp = form["otp"].ToString();
            string backupCode = form["backupCode"].ToString();
            bool verified = false;

            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, Username!)
            ];
            if (otp != "" && backupCode == "")
            {
                string otpCache = _otpStore.RetrieveOtp("verification")!;
                if (otpCache == otp) verified = true;
            }
            else if (otp == "" && backupCode != "")
            {
                if (userType == "Citizen")
                {
                    var citizen = _dbContext.Citizens.FirstOrDefault(u => u.CitizenId == userId)!;
                    var backupCodes = JsonConvert.DeserializeObject<dynamic>(citizen.BackupCodes!);
                    var unused = backupCodes!["unused"];
                    var used = backupCodes!["used"];

                    foreach (var code in unused)
                    {
                        if (code == backupCode)
                        {
                            verified = true;
                            used.Add(code);
                            unused.Remove(code);
                            _dbContext.Database.ExecuteSqlRaw("EXEC UpdateCitizenDetail @ColumnName,@ColumnValue,@TableName,@CitizenId", new SqlParameter("@ColumnName", "BackupCodes"), new SqlParameter("@ColumnValue", JsonConvert.SerializeObject(backupCodes)), new SqlParameter("@TableName", "Citizens"), new SqlParameter("@CitizenId", userId));
                            break;
                        }
                    }
                }
                else if (userType == "Officer")
                {
                    var officer = _dbContext.Officers.FirstOrDefault(u => u.OfficerId == userId)!;
                    var backupCodes = JsonConvert.DeserializeObject<dynamic>(officer.BackupCodes!);
                    var unused = backupCodes!["unused"];
                    var used = backupCodes!["used"];

                    foreach (var code in unused)
                    {
                        if (code == backupCode)
                        {
                            verified = true;
                            used.Add(code);
                            unused.Remove(code);
                            _dbContext.Database.ExecuteSqlRaw("EXEC UpdateCitizenDetail @ColumnName,@ColumnValue,@TableName,@CitizenId", new SqlParameter("@ColumnName", "BackupCodes"), new SqlParameter("@ColumnValue", JsonConvert.SerializeObject(backupCodes)), new SqlParameter("@TableName", "Officers"), new SqlParameter("@CitizenId", userId));
                            break;
                        }
                    }

                }

            }

            if (verified)
            {
                if (userType == "Citizen")
                    claims.Add(new Claim(ClaimTypes.Role, "Citizen"));
                else if (userType == "Officer")
                    claims.Add(new Claim(ClaimTypes.Role, "Officer"));


                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Sign in the user with a persistent cookie
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true, // Create a persistent cookie
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // Set the cookie expiration to 30 days
                };
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                return RedirectToAction("Index", userType == "Citizen" ? "User" : "Officer");
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid OTP or Backup Code. Try again";
                return View("Verification");
            }
        }

        public async Task<IActionResult> Register(IFormCollection form)
        {
            var username = new SqlParameter("@Username", form["Username"].ToString());
            var password = new SqlParameter("@Password", form["Password"].ToString());
            var email = new SqlParameter("@Email", form["Email"].ToString());
            var mobileNumber = new SqlParameter("@MobileNumber", form["MobileNumber"].ToString());

            var used = _helper.GenerateUniqueRandomCodes(10, 8);
            var backupCodes = new
            {
                used,
                unused = Array.Empty<string>()
            };

            var backupCodesParam = new SqlParameter("@BackupCodes", JsonConvert.SerializeObject(backupCodes));

            var result = _dbContext.Citizens.FromSqlRaw("EXEC RegisterCitizen @Username,@Email,@Password,@MobileNumber,@BackupCodes", username, email, password, mobileNumber, backupCodesParam).ToList();

            if (result.Count != 0)
            {
                string otp = GenerateOTP(6);
                _otpStore.StoreOtp("registration", otp);
                await _emailSender.SendEmail(form["Email"].ToString(), "OTP For Registration.", otp);
                return Json(new { status = true, result[0].CitizenId });
            }
            else
            {
                return Json(new { status = false, response = "Registration failed." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Authentication([FromForm] IFormCollection form)
        {
            _logger.LogInformation($"Form Type: {form["formType"].ToString()}");
            return form["formType"].ToString() == "login" ? await Login(form) : await Register(form);
        }

        [HttpPost]
        public IActionResult OTPValidation([FromForm] IFormCollection form)
        {
            string otpUser = form["otp"].ToString();
            string otpCache = _otpStore.RetrieveOtp("registration")!;
            _logger.LogInformation($"Citizen ID : {form["CitizenId"].ToString()}");
            _logger.LogInformation($"OTP CACHE: {otpCache}  OTP USER: {otpUser}");

            if (otpCache == otpUser)
            {
                if (int.TryParse(form["CitizenId"].ToString(), out int parsedCitizenId))
                {
                    _logger.LogInformation($"CITIZEN ID : {parsedCitizenId}");
                    var citizenIdParam = new SqlParameter("@CitizenId", parsedCitizenId);
                    _dbContext.Database.ExecuteSqlRaw("EXEC ValidateUserEmail @CitizenId", citizenIdParam);
                    return Json(new { status = true, response = "Registration Successful." });
                }
                else
                {
                    return Json(new { status = false, response = "Invalid Citizen ID." });
                }
            }
            else
            {
                return Json(new { status = false, response = "Invalid OTP." });
            }
        }

        public new IActionResult Unauthorized()
        {
            return View();
        }

        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult GetDistricts()
        {
            var districts = _dbContext.Districts.ToList();
            return Json(new { status = true, districts });
        }

        [HttpGet]
        public IActionResult GetTehsils(string districtId)
        {
            if (int.TryParse(districtId, out int districtIdParsed))
            {
                var tehsils = _dbContext.Tehsils.Where(u => u.DistrictId == districtIdParsed).ToList();
                return Json(new { status = true, tehsils });
            }
            return Json(new { status = false, response = "Invalid district ID." });
        }

        [HttpGet]
        public IActionResult GetDesignations()
        {
            var designations = _dbContext.OfficersDesignations.ToList();
            return Json(new { status = true, designations });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
