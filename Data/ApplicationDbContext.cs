using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RideFusion.Models;

namespace RideFusion.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Ride> Rides { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Ride relationships
            builder.Entity<Ride>()
                .HasOne(r => r.Driver)
                .WithMany(u => u.Rides)
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Booking relationships
            builder.Entity<Booking>()
                .HasOne(b => b.Ride)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Booking>()
                .HasOne(b => b.Passenger)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure ChatMessage relationships
            builder.Entity<ChatMessage>()
                .HasOne(c => c.Ride)
                .WithMany()
                .HasForeignKey(c => c.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatMessage>()
                .HasOne(c => c.Sender)
                .WithMany()
                .HasForeignKey(c => c.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
