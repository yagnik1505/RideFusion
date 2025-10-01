using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;
using RideFusion.Filters;

namespace RideFusion.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Chat/Index
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> Index(int rideId)
        {
            var ride = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Bookings)
                .ThenInclude(b => b.Passenger)
                .FirstOrDefaultAsync(r => r.RideId == rideId);

            if (ride == null)
            {
                return NotFound();
            }

            // Check if user is authorized to access this chat
            bool isAuthorized = ride.DriverId == User.Identity.Name || 
                              ride.Bookings.Any(b => b.PassengerId == User.Identity.Name && b.Status == "Confirmed");

            if (!isAuthorized)
            {
                return Forbid();
            }

            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.RideId == rideId)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            ViewBag.Ride = ride;
            ViewBag.Messages = messages;

            return View();
        }

        // GET: Chat/MyChats
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> MyChats()
        {
            var userId = User.Identity.Name;

            var rides = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Bookings)
                .ThenInclude(b => b.Passenger)
                .Where(r => r.DriverId == userId || 
                           r.Bookings.Any(b => b.PassengerId == userId && b.Status == "Confirmed"))
                .ToListAsync();

            return View(rides);
        }
    }
}
