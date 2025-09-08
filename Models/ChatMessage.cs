using System;
using System.ComponentModel.DataAnnotations;

namespace RideFusion.Models
{
    public class ChatMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int RideId { get; set; }
        public required Ride Ride { get; set; }

        [Required]
        public required string SenderId { get; set; }
        public required ApplicationUser Sender { get; set; }

        [Required, MaxLength(1000)]
        public required string MessageText { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
