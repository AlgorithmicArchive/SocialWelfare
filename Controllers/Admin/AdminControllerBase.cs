using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public partial class AdminController(SocialWelfareDepartmentContext dbcontext, ILogger<AdminController> logger) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;
        protected readonly ILogger<AdminController> _logger = logger;

         public IActionResult Dashboard()
        {
            int ServiceCount = dbcontext.Services.ToList().Count;
            int OfficerCount = dbcontext.Officers.ToList().Count;
            int CitizenCount = dbcontext.Citizens.ToList().Count;
            int ApplicationCount = dbcontext.Applications.ToList().Count;

            int TotalCount = GetCount("Total", null!);
            int PendingCount = GetCount("Pending", null!);
            int RejectCount = GetCount("Reject", null!);
            int SanctionCount = GetCount("Sanction", null!);

            var countList = new
            {
                ServiceCount,
                OfficerCount,
                CitizenCount,
                ApplicationCount,
                TotalCount,
                PendingCount,
                RejectCount,
                SanctionCount
            };
            return View(countList);
        }

        public IActionResult Reports()
        {
            return View();
        }

        [HttpGet("Admin/Services/Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpGet("Admin/Services/Modify")]
        public IActionResult Modify()
        {
            return View();
        }

       
    }
}