using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RideFusion.Models;
using RideFusion.Data;
using Microsoft.EntityFrameworkCore;

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
            var rides = _context.Rides
                .Include(r => r.Driver)
                .Include(r => r.Bookings)
                .Where(r => r.AvailableSeats > 0);

            if (!string.IsNullOrEmpty(startLocation))
            {
                rides = rides.Where(r => r.StartLocation.Contains(startLocation));
            }

            if (!string.IsNullOrEmpty(endLocation))
            {
                rides = rides.Where(r => r.EndLocation.Contains(endLocation));
            }

            if (date.HasValue)
            {
                rides = rides.Where(r => r.StartDateTime.Date == date.Value.Date);
            }

            var rideList = await rides.ToListAsync();
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
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ride/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create([Bind("StartLocation,EndLocation,StartDateTime,AvailableSeats,PricePerSeat,DistanceKm,EstimatedMinutes")] Ride ride)
        {
            if (ModelState.IsValid)
            {
                ride.DriverId = User.Identity.Name; // This will be updated when we implement proper authentication
                _context.Add(ride);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(MyRides));
            }
            return View(ride);
        }

        // GET: Ride/MyRides
        [Authorize]
        public async Task<IActionResult> MyRides()
        {
            var rides = await _context.Rides
                .Include(r => r.Bookings)
                .Where(r => r.DriverId == User.Identity.Name)
                .ToListAsync();

            return View(rides);
        }

        // GET: Ride/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ride = await _context.Rides.FindAsync(id);
            if (ride == null || ride.DriverId != User.Identity.Name)
            {
                return NotFound();
            }

            return View(ride);
        }

        // POST: Ride/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ride = await _context.Rides
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(m => m.RideId == id);

            if (ride == null || ride.DriverId != User.Identity.Name)
            {
                return NotFound();
            }

            return View(ride);
        }

        // POST: Ride/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ride = await _context.Rides.FindAsync(id);
            if (ride != null && ride.DriverId == User.Identity.Name)
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
