using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;

namespace RideFusion.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalRides = await _context.Rides.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var pendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending");

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalRides = totalRides;
            ViewBag.TotalBookings = totalBookings;
            ViewBag.PendingBookings = pendingBookings;

            return View();
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: Admin/Rides
        public async Task<IActionResult> Rides()
        {
            var rides = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Bookings)
                .ToListAsync();

            return View(rides);
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.Ride)
                .ThenInclude(r => r.Driver)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // POST: Admin/ApproveUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsVerified = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/BanUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/RemoveRide
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRide(int rideId)
        {
            var ride = await _context.Rides.FindAsync(rideId);
            if (ride != null)
            {
                _context.Rides.Remove(ride);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Rides));
        }
    }
}
