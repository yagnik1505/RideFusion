using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RideFusion.Models
{
    public class Ride
    {
        public int RideId { get; set; }

        [Required]
        public string DriverId { get; set; }
        public ApplicationUser Driver { get; set; }

        [Required, MaxLength(200)]
        public string StartLocation { get; set; }

        [Required, MaxLength(200)]
        public string EndLocation { get; set; }

        public DateTime StartDateTime { get; set; }

        [Range(0, 50)]
        public int AvailableSeats { get; set; }

        [Range(0, 999999)]
        public decimal PricePerSeat { get; set; }

        // Optional extra fields
        public double? DistanceKm { get; set; }
        public int? EstimatedMinutes { get; set; }

        // Navigation
        public ICollection<Booking> Bookings { get; set; }
    }
}
