using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;
using RideFusion.Filters;
using System.Security.Claims;

namespace RideFusion.Controllers
{
    public class RideController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RideController(ApplicationDbContext context)
        {
            _context = context;
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

        // GET: Ride/Create
        [Authorize(Roles = "Driver")]
        [ProfileCompleted]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ride/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Driver")]
        [ProfileCompleted]
        public async Task<IActionResult> Create([Bind("StartLocation,EndLocation,StartDateTime,AvailableSeats,PricePerSeat,DistanceKm,EstimatedMinutes")] Ride ride)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ride.DriverId = userId;
                _context.Add(ride);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyRides));
            }
            return View(ride);
        }

        // GET: Ride/MyRides
        [Authorize(Roles = "Driver")]
        [ProfileCompleted]
        public async Task<IActionResult> MyRides()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var rides = await _context.Rides
                .Include(r => r.Bookings)
                .Where(r => r.DriverId == userId)
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
