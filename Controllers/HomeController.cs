using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;

namespace RideFusion.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get recent rides for homepage
                var recentRides = await _context.Rides
                    .Include(r => r.Driver)
                    .Where(r => r.StartDateTime > DateTime.Now && r.AvailableSeats > 0)
                    .OrderBy(r => r.StartDateTime)
                    .Take(6)
                    .ToListAsync();

                ViewBag.RecentRides = recentRides;
            }
            catch
            {
                // If database is not ready, show empty list
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
