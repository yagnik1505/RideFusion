using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RideFusion.Data;
using RideFusion.Models;
using RideFusion.Filters; // Added for [ProfileCompleted]
using System.Security.Claims; // Added for ClaimTypes and FindFirstValue

namespace RideFusion.Controllers
{
    [Authorize]
    public class RideController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RideController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Ride/Search
        public async Task<IActionResult> Search(string? startLocation, string? endLocation, DateTime? date)
        {
            ViewBag.StartLocation = startLocation;
            ViewBag.EndLocation = endLocation;
            ViewBag.Date = date;

            var rides = _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Bookings)
                .Where(r => r.AvailableSeats > 0 && r.StartDateTime >= DateTime.Now.AddMinutes(-30));

            if (!string.IsNullOrEmpty(startLocation))
            {
                var from = $"%{startLocation}%";
                rides = rides.Where(r => EF.Functions.Like(r.StartLocation, from));
            }

            if (!string.IsNullOrEmpty(endLocation))
            {
                var to = $"%{endLocation}%";
                rides = rides.Where(r => EF.Functions.Like(r.EndLocation, to));
            }

            if (date.HasValue)
            {
                rides = rides.Where(r => r.StartDateTime.Date == date.Value.Date);
            }

            var rideList = await rides
                .OrderBy(r => r.StartDateTime)
                .ToListAsync();
            return View(rideList);
        }

        // GET: Ride/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ride = await _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(m => m.RideId == id);

            if (ride == null)
            {
                return NotFound();
            }

            return View(ride);
        }

        // GET: Ride/Create (Offer Ride)
        [HttpGet]
        public IActionResult Create()
        {
            var model = new Ride
            {
                // Satisfy required members with defaults for the form; real values are posted back
                DriverId = _userManager.GetUserId(User) ?? string.Empty,
                StartLocation = string.Empty,
                EndLocation = string.Empty,
                StartDateTime = DateTime.Now.AddHours(1), // sensible default
                AvailableSeats = 1,
                PricePerSeat = 0
            };
            return View(model);
        }

        // POST: Ride/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ride ride)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Unable to identify current user.";
                    return View(ride);
                }

                // Ensure DriverId is set for validation
                ride.DriverId = userId;
                
                // Clear any DriverId validation errors since we set it server-side
                ModelState.Remove("DriverId");

                // Debug validation
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    TempData["Error"] = $"Validation failed: {errors}";
                    return View(ride);
                }

                _context.Rides.Add(ride);
                var result = await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Ride created successfully! Rows affected: {result}";
                return RedirectToAction(nameof(MyRides));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating ride: {ex.Message}";
                return View(ride);
            }
        }

        // GET: Ride/MyRides
        [HttpGet]
        public async Task<IActionResult> MyRides()
        {
            var userId = _userManager.GetUserId(User);
            var rides = await _context.Rides
                .Include(r => r.Bookings)
                .ThenInclude(b => b.Passenger)
                .Where(r => r.DriverId == userId)
                .OrderByDescending(r => r.StartDateTime)
                .ToListAsync();

            return View(rides);
        }

        // GET: Ride/Edit/5
        [Authorize(Roles = "Driver")]
        [ProfileCompleted]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ride = await _context.Rides.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ride == null || ride.DriverId != userId)
            {
                return NotFound();
            }

            return View(ride);
        }

        // POST: Ride/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Driver")]
        [ProfileCompleted]
        public async Task<IActionResult> Edit(int id, [Bind("RideId,StartLocation,EndLocation,StartDateTime,AvailableSeats,PricePerSeat,DistanceKm,EstimatedMinutes")] Ride ride)
        {
            if (id != ride.RideId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Ensure the ride remains associated with the current driver
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    ride.DriverId = userId;
                    _context.Update(ride);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RideExists(ride.RideId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(MyRides));
            }
            return View(ride);
        }

        // GET: Ride/Delete/5
        [Authorize(Roles = "Driver")]
        [ProfileCompleted]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ride = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(m => m.RideId == id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ride == null || ride.DriverId != userId)
            {
                return NotFound();
            }

            return View(ride);
        }

        // POST: Ride/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Driver")]
        [ProfileCompleted]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ride = await _context.Rides.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ride != null && ride.DriverId == userId)
            {
                _context.Rides.Remove(ride);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyRides));
        }

        private bool RideExists(int id)
        {
            return _context.Rides.Any(e => e.RideId == id);
        }
    }
}
