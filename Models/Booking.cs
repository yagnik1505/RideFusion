using System;
using System.ComponentModel.DataAnnotations;

namespace RideFusion.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int RideId { get; set; }
        public required Ride Ride { get; set; }

        [Required]
        public required string PassengerId { get; set; }
        public required ApplicationUser Passenger { get; set; }

        [Range(1, 10)]
        public int SeatsBooked { get; set; } = 1;

        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
