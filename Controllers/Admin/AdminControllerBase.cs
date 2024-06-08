using Microsoft.AspNetCore.Mvc;
using SocialWelfare.Models.Entities;

namespace SocialWelfare.Controllers.Admin
{
    public partial class AdminController(SocialWelfareDepartmentContext dbcontext) : Controller
    {
        protected readonly SocialWelfareDepartmentContext dbcontext = dbcontext;

        public IActionResult Dashboard()
        {
            int ServiceCount = dbcontext.Services.ToList().Count;
            int OfficerCount = dbcontext.Officers.ToList().Count;
            int CitizenCount = dbcontext.Citizens.ToList().Count;
            int ApplicationCount = dbcontext.Applications.ToList().Count;
            var countList = new
            {
                ServiceCount,
                OfficerCount,
                CitizenCount,
                ApplicationCount
            };
            return View(countList);
        }

    }
}