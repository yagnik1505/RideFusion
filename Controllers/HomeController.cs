using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace RideFusion.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);

                var recentRidesQuery = _context.Rides
                    .Include(r => r.Driver)
                    .Where(r => r.StartDateTime > DateTime.Now && r.AvailableSeats > 0);

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    recentRidesQuery = recentRidesQuery.Where(r => r.DriverId != currentUserId);
                }

                var recentRides = await recentRidesQuery
                    .OrderBy(r => r.StartDateTime)
                    .Take(6)
                    .ToListAsync();

                ViewBag.RecentRides = recentRides;
            }
            catch
            {
                ViewBag.RecentRides = new List<RideFusion.Models.Ride>();
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
