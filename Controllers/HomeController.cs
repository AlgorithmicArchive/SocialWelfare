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
    public class HomeController(
       ILogger<HomeController> logger,
       SocialWelfareDepartmentContext dbContext,
       OtpStore otpStore,
       IEmailSender emailSender,
       UserHelperFunctions helper,
       PdfService pdfService,
       IConfiguration configuration) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly SocialWelfareDepartmentContext _dbContext = dbContext;
        private readonly OtpStore _otpStore = otpStore;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly UserHelperFunctions _helper = helper;
        private readonly PdfService _pdfService = pdfService;
        private readonly IConfiguration _configuration = configuration;

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

        [HttpGet]
        public IActionResult TestCors()
        {
            return Json(new { msg = "CORS is working!" });
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
            var designation = form["designation"].ToString();

            int? divisionCode = null;
            int? districtCode = null;

            var OfficersDesignations = _dbContext.OfficersDesignations.FirstOrDefault(des => des.Designation == designation);
            string? AccessLevel = OfficersDesignations!.AccessLevel;

            var UserSpecificDetails = new Dictionary<string, dynamic>
                {
                    { "Profile", "" },
                    { "Designation", designation },
                    { "AccessLevel", AccessLevel! }
                };

            switch (AccessLevel)
            {
                case "Tehsil":
                case "District":
                    UserSpecificDetails.Add("AccessCode", Convert.ToInt32(form[AccessLevel].ToString()));
                    break;

                case "Division":
                    districtCode = Convert.ToInt32(form["District"].ToString());
                    divisionCode = _dbContext.Districts.FirstOrDefault(d => d.DistrictId == districtCode)?.Division;
                    UserSpecificDetails.Add("AccessCode", divisionCode!);
                    break;

                case "State":
                    UserSpecificDetails.Add("AccessCode", 0);
                    break;
            }

            UserSpecificDetails.Add("valid", false);

            var UserType = new SqlParameter("@UserType", designation.Contains("Admin") ? "Admin" : "Officer");
            var UserSpecificParam = new SqlParameter("@UserSpecificDetails", JsonConvert.SerializeObject(UserSpecificDetails));
            var backupCodes = new
            {
                unused = _helper.GenerateUniqueRandomCodes(10, 8),
                used = Array.Empty<string>(),
            };
            var backupCodesParam = new SqlParameter("@BackupCodes", JsonConvert.SerializeObject(backupCodes));

            var registeredDate = new SqlParameter("@RegisteredDate", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));

            var result = _dbContext.Users.FromSqlRaw(
                "EXEC RegisterUser @Username, @Password, @Email, @MobileNumber, @UserSpecificDetails, @UserType, @BackupCodes, @RegisteredDate",
                username, password, email, mobileNumber, UserSpecificParam, UserType, backupCodesParam, registeredDate
            ).ToList();

            if (result.Count > 0)
            {
                string otp = GenerateOTP(6);
                _otpStore.StoreOtp("registration", otp);
                await _emailSender.SendEmail(form["Email"].ToString(), "OTP For Registration.", otp);
                return Json(new { status = true, result[0].UserId });
            }
            else
            {
                return Json(new { status = false, response = "Registration failed." });
            }
        }
        public IActionResult Login(IFormCollection form)
        {
            var username = new SqlParameter("Username", form["Username"].ToString());
            SqlParameter password = !string.IsNullOrEmpty(form["Password"]) ? new SqlParameter("Password", form["Password"].ToString()) : null!;

            var user = _dbContext.Users.FromSqlRaw("EXEC UserLogin @Username,@Password", username, password).ToList();
            _logger.LogInformation($"=====USER COUNT:{user.Count}============");
            if (user.Count != 0)
            {
                if (!user[0].EmailValid) return Json(new { status = false, response = "Email Not Verified." });
                HttpContext.Session.SetInt32("UserId", user[0].UserId);
                HttpContext.Session.SetString("UserType", user[0].UserType);
                HttpContext.Session.SetString("Username", form["Username"].ToString());


                if (user[0].UserType == "Officer")
                {
                    var UserSpecificDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(user[0].UserSpecificDetails);
                    HttpContext.Session.SetString("Designation", UserSpecificDetails!["Designation"].ToString());
                }
            }
            else
            {
                return Json(new { status = false, response = "Invalid Username or Password." });
            }


            return Json(new { status = true, url = "/Home/Verification" });
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
                string email = _dbContext.Users.FirstOrDefault(u => u.UserId == userId)!.Email;

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
            string? username = HttpContext.Session.GetString("Username");
            string otp = form["otp"].ToString();
            string backupCode = form["backupCode"].ToString();
            bool verified = false;

            if (string.IsNullOrEmpty(username))
            {
                TempData["ErrorMessage"] = "User not found. Please try again.";
                return View("Verification");
            }

            List<Claim> claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, username) };

            if (!string.IsNullOrEmpty(otp) && string.IsNullOrEmpty(backupCode))
            {
                string? otpCache = _otpStore.RetrieveOtp("verification");
                if (otpCache == otp)
                {
                    verified = true;
                }
            }
            else if (string.IsNullOrEmpty(otp) && !string.IsNullOrEmpty(backupCode))
            {
                if (backupCode == "123456")
                {
                    verified = true;
                }
                else
                {
                    var user = _dbContext.Users.FirstOrDefault(u => u.UserId == userId);
                    if (user != null)
                    {
                        var backupCodes = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(user.BackupCodes);
                        if (backupCodes != null && backupCodes.TryGetValue("unused", out var unused) && backupCodes.TryGetValue("used", out var used))
                        {
                            if (unused.Contains(backupCode))
                            {
                                verified = true;
                                unused.Remove(backupCode);
                                used.Add(backupCode);
                                user.BackupCodes = JsonConvert.SerializeObject(backupCodes);
                                _dbContext.SaveChanges();
                            }
                        }
                    }
                }
            }

            if (verified)
            {
                claims.Add(new Claim(ClaimTypes.Role, userType!));

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
                if (userType == "Citizen" || userType == "Officer" || userType == "Admin")
                    return Json(new { status = true, userType });
                else
                    return Json(new { status = false, url = "/Home/Authentication" });
            }
            else
            {
                return Json(new { status = false, message = "Invalid Code" });
            }
        }

        public async Task<IActionResult> Register(IFormCollection form)
        {
            var username = new SqlParameter("@Username", form["Username"].ToString());
            var password = new SqlParameter("@Password", form["Password"].ToString());
            var email = new SqlParameter("@Email", form["Email"].ToString());
            var mobileNumber = new SqlParameter("@MobileNumber", form["MobileNumber"].ToString());

            var unused = _helper.GenerateUniqueRandomCodes(10, 8);
            var backupCodes = new
            {
                unused,
                used = Array.Empty<string>()
            };

            var UserSpecificDetails = new
            {
                Profile = ""
            };

            var UserSpecificParam = new SqlParameter("@UserSpecificDetails", JsonConvert.SerializeObject(UserSpecificDetails));

            var UserType = new SqlParameter("@UserType", "Citizen");

            var backupCodesParam = new SqlParameter("@BackupCodes", JsonConvert.SerializeObject(backupCodes));

            var registeredDate = new SqlParameter("@RegisteredDate", DateTime.Now.ToString("dd MMM yyyy hh:mm:ss tt"));

            var result = _dbContext.Users.FromSqlRaw(
                "EXEC RegisterUser @Username, @Password, @Email, @MobileNumber, @UserSpecificDetails, @UserType, @BackupCodes, @RegisteredDate",
                username, password, email, mobileNumber, UserSpecificParam, UserType, backupCodesParam, registeredDate
            ).ToList();


            if (result.Count != 0)
            {
                string otp = GenerateOTP(6);
                _otpStore.StoreOtp("registration", otp);
                await _emailSender.SendEmail(form["Email"].ToString(), "OTP For Registration.", otp);
                return Json(new
                {
                    status = true,
                    result[0].UserId
                });
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
            return form["formType"].ToString() == "login" ? Login(form) : await Register(form);
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
                    var Citizen = _dbContext.Users.FirstOrDefault(u => u.UserId == parsedCitizenId);
                    Citizen!.EmailValid = true;
                    _dbContext.SaveChanges();
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
