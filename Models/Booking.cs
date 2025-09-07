using System;
using System.ComponentModel.DataAnnotations;

namespace RideFusion.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int RideId { get; set; }
        public Ride Ride { get; set; }

        [Required]
        public string PassengerId { get; set; }
        public ApplicationUser Passenger { get; set; }

        [Range(1, 10)]
        public int SeatsBooked { get; set; } = 1;

        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled

        [MaxLength(10)]
        public string OTP { get; set; } // 6-digit recommended

        public bool IsVerified { get; set; }  // true when OTP validated

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
