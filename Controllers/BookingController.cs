using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using RideFusion.Filters;

namespace RideFusion.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking/Create
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> Create(int rideId)
        {
            var ride = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.RideId == rideId);

            if (ride == null)
            {
                return NotFound();
            }

            ViewBag.Ride = ride;
            return View();
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> Create([Bind("RideId,SeatsBooked")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                // Generate 6-digit OTP
                booking.OTP = GenerateOTP();
                booking.PassengerId = User.Identity.Name; // This will be updated when we implement proper authentication
                booking.Status = "Pending";
                booking.IsVerified = false;

                _context.Add(booking);
                await _context.SaveChangesAsync();

                // TODO: Send OTP via email/SMS
                // For now, we'll show it in the view

                return RedirectToAction(nameof(VerifyOTP), new { bookingId = booking.BookingId });
            }

            var ride = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.RideId == booking.RideId);
            ViewBag.Ride = ride;
            return View(booking);
        }

        // GET: Booking/VerifyOTP
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> VerifyOTP(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .ThenInclude(r => r.Driver)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.PassengerId != User.Identity.Name)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Booking/VerifyOTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> VerifyOTP(int bookingId, string otp)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null || booking.PassengerId != User.Identity.Name)
            {
                return NotFound();
            }

            if (booking.OTP == otp)
            {
                booking.IsVerified = true;
                booking.Status = "Confirmed";
                
                // Update available seats
                booking.Ride.AvailableSeats -= booking.SeatsBooked;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "OTP verified successfully! Your booking is confirmed.";
                return RedirectToAction(nameof(MyBookings));
            }
            else
            {
                ModelState.AddModelError("", "Invalid OTP. Please try again.");
            }

            return View(booking);
        }

        // GET: Booking/MyBookings
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> MyBookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Ride)
                .ThenInclude(r => r.Driver)
                .Where(b => b.PassengerId == User.Identity.Name)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking/DriverBookings
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> DriverBookings(int rideId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.Ride)
                .Where(b => b.RideId == rideId && b.Ride.DriverId == User.Identity.Name)
                .ToListAsync();

            return View(bookings);
        }

        // POST: Booking/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> Approve(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking != null && booking.Ride.DriverId == User.Identity.Name)
            {
                booking.Status = "Confirmed";
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(DriverBookings), new { rideId = booking.RideId });
        }

        // POST: Booking/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> Reject(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking != null && booking.Ride.DriverId == User.Identity.Name)
            {
                booking.Status = "Cancelled";
                booking.Ride.AvailableSeats += booking.SeatsBooked;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(DriverBookings), new { rideId = booking.RideId });
        }

        // POST: Booking/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ProfileCompleted]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.PassengerId == User.Identity.Name);

            if (booking != null)
            {
                booking.Status = "Cancelled";
                booking.Ride.AvailableSeats += booking.SeatsBooked;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(MyBookings));
        }

        private string GenerateOTP()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
                return (randomNumber % 1000000).ToString("D6");
            }
        }
    }
}
