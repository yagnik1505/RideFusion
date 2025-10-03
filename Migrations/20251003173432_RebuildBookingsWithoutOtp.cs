using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RideFusion.Migrations
{
    /// <inheritdoc />
    public partial class RebuildBookingsWithoutOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Purge orphaned booking rows that violate FK constraints (causing rebuild copy failure)
            migrationBuilder.Sql(@"DELETE FROM Bookings WHERE PassengerId NOT IN (SELECT Id FROM AspNetUsers);");
            migrationBuilder.Sql(@"DELETE FROM Bookings WHERE RideId NOT IN (SELECT RideId FROM Rides);");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "OTP",
                table: "Bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Bookings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OTP",
                table: "Bookings",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }
    }
}
