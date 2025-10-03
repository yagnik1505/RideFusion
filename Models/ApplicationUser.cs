using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace RideFusion.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
        // Legacy/simple vehicle field (kept for backward compatibility/display)
        public string? VehicleDetails { get; set; } // For Drivers, null for Passengers/Admin
        public bool IsVerified { get; set; } // ID/email verification

        // Common profile fields
        public string? Address { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Driver-specific detailed vehicle info
        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }
        public int? VehicleYear { get; set; }
        public string? VehicleColor { get; set; }
        public string? LicensePlate { get; set; }

        // Driver license details
        public string? DriversLicenseNumber { get; set; }
        public System.DateTime? DriversLicenseExpiry { get; set; }

        // Driver availability and payments
        public bool? IsAvailable { get; set; }
        public string? UpiId { get; set; } // Payment handle (e.g., user@upi)

        // Optional rating aggregates
        public double? DriverRatingAverage { get; set; }
        public int? DriverRatingCount { get; set; }

        // Passenger-side optional summary
        public string? PassengerRideSummary { get; set; }

        // New common fields
        public System.DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; } // e.g., Male, Female, Other

        // Navigation
        public ICollection<Ride> Rides { get; set; } = new List<Ride>();          // as Driver
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();    // as Passenger
    }
}
