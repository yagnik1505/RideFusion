using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace RideFusion.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
        public string? VehicleDetails { get; set; } // For Drivers, null for Passengers/Admin
        public bool IsVerified { get; set; } // ID/email verification

        // Navigation
        public ICollection<Ride> Rides { get; set; } = new List<Ride>();          // as Driver
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();    // as Passenger
    }
}
