using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using RideFusion.Filters;
using Microsoft.AspNetCore.Identity;

namespace RideFusion.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Booking/Create
        [Authorize]
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
            
            // Create a new booking model with default values
            var booking = new Booking
            {
                RideId = rideId,
                SeatsBooked = 1,
                // Required properties will be set when saving
                PassengerId = string.Empty,
                OTP = string.Empty,
                Ride = ride,
                Passenger = null!
            };
            
            return View(booking);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(Booking booking)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Unable to identify current user.";
                    return View(booking);
                }

                // Check if ride has enough available seats
                var ride = await _context.Rides.FindAsync(booking.RideId);
                if (ride == null)
                {
                    TempData["Error"] = "Ride not found.";
                    return View(booking);
                }

                if (ride.AvailableSeats < booking.SeatsBooked)
                {
                    TempData["Error"] = $"Not enough seats available. Only {ride.AvailableSeats} seats left.";
                    return View(booking);
                }

                // Set required properties for immediate booking confirmation
                booking.PassengerId = userId;
                booking.OTP = GenerateOTP(); // Keep for future use, but not required now
                booking.Status = "Confirmed"; // Directly confirm the booking
                booking.IsVerified = true; // Mark as verified since no OTP needed
                booking.CreatedAt = DateTime.UtcNow;

                // Update available seats immediately
                ride.AvailableSeats -= booking.SeatsBooked;

                // Clear validation errors for server-set properties
                ModelState.Remove("PassengerId");
                ModelState.Remove("OTP");
                ModelState.Remove("Ride");
                ModelState.Remove("Passenger");

                if (!ModelState.IsValid)
                {
                    var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    TempData["Error"] = $"Validation failed: {errors}";
                    
                    var rideForView = await _context.Rides
                        .Include(r => r.Driver)
                        .FirstOrDefaultAsync(r => r.RideId == booking.RideId);
                    ViewBag.Ride = rideForView;
                    return View(booking);
                }

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Booking confirmed successfully! Your ride is booked.";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating booking: {ex.Message}";
                
                var ride = await _context.Rides
                    .Include(r => r.Driver)
                    .FirstOrDefaultAsync(r => r.RideId == booking.RideId);
                ViewBag.Ride = ride;
                return View(booking);
            }
        }

        // GET: Booking/MyBookings
        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Ride)
                .ThenInclude(r => r.Driver)
                .Include(b => b.Passenger)
                .Where(b => b.PassengerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // GET: Booking/DriverBookings
        [Authorize]
        public async Task<IActionResult> DriverBookings(int rideId)
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Passenger)
                .Include(b => b.Ride)
                .Where(b => b.RideId == rideId && b.Ride.DriverId == userId)
                .ToListAsync();

            return View(bookings);
        }

        // POST: Booking/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Ride)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.PassengerId == userId);

            if (booking != null)
            {
                booking.Status = "Cancelled";
                // Restore available seats
                booking.Ride.AvailableSeats += booking.SeatsBooked;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Booking cancelled successfully.";
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
