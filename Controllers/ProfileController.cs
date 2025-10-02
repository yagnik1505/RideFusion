using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RideFusion.Models;
using System.ComponentModel.DataAnnotations;
using System;
using RideFusion.Data; // add context
using Microsoft.Data.Sqlite; // for reading sqlite connection
using System.IO;
using Microsoft.EntityFrameworkCore; // FIX: needed for DatabaseFacade extensions like GetDbConnection()

namespace RideFusion.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProfileController> _logger;
        private readonly ApplicationDbContext _context; // add

        public ProfileController(UserManager<ApplicationUser> userManager, ILogger<ProfileController> logger, ApplicationDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        public class ProfileInput
        {
            [Required]
            [Display(Name = "Full name")]
            public string FullName { get; set; } = string.Empty;

            [Phone]
            [Display(Name = "Contact Number (Phone)")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Residential Address")]
            public string? Address { get; set; }

            [Url]
            [Display(Name = "Profile Picture URL (optional)")]
            public string? ProfilePictureUrl { get; set; }

            // Driver detailed vehicle info
            [Display(Name = "Vehicle Make")]
            public string? VehicleMake { get; set; }

            [Display(Name = "Vehicle Model")]
            public string? VehicleModel { get; set; }

            [Range(1900, 2100)]
            [Display(Name = "Vehicle Year")]
            public int? VehicleYear { get; set; }

            [Display(Name = "Vehicle Color")]
            public string? VehicleColor { get; set; }

            [Display(Name = "License Plate")]
            public string? LicensePlate { get; set; }

            [Display(Name = "Driver's License Number")]
            public string? DriversLicenseNumber { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Driver's License Expiry Date")]
            public DateTime? DriversLicenseExpiry { get; set; }

            [Display(Name = "Availability Status")]
            public bool? IsAvailable { get; set; }

            [Display(Name = "UPI ID (online payment)")]
            public string? UpiId { get; set; }

            // Passenger optional summary
            [Display(Name = "Ride History Summary (optional)")]
            public string? PassengerRideSummary { get; set; }

            // Backward compatibility field (combined vehicle details)
            [Display(Name = "Vehicle Details (legacy)")]
            public string? VehicleDetails { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.IsDriver = roles.Any(r => string.Equals(r, "Driver", StringComparison.OrdinalIgnoreCase));

            var model = new ProfileInput
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                ProfilePictureUrl = user.ProfilePictureUrl,
                VehicleMake = user.VehicleMake,
                VehicleModel = user.VehicleModel,
                VehicleYear = user.VehicleYear,
                VehicleColor = user.VehicleColor,
                LicensePlate = user.LicensePlate,
                DriversLicenseNumber = user.DriversLicenseNumber,
                DriversLicenseExpiry = user.DriversLicenseExpiry,
                IsAvailable = user.IsAvailable,
                UpiId = user.UpiId,
                PassengerRideSummary = user.PassengerRideSummary,
                VehicleDetails = user.VehicleDetails
            };
            ViewBag.ReturnUrl = returnUrl;

            // Surface the real DB file path for verification
            try
            {
                var connStr = _context.Database.GetDbConnection().ConnectionString;
                var builder = new SqliteConnectionStringBuilder(connStr);
                var absolute = Path.GetFullPath(builder.DataSource);
                ViewBag.DbFile = absolute;
            }
            catch
            {
                ViewBag.DbFile = _context.Database.GetDbConnection().ConnectionString;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileInput input, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            bool isDriver = roles.Any(r => string.Equals(r, "Driver", StringComparison.OrdinalIgnoreCase));

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.IsDriver = isDriver;
                ViewBag.ReturnUrl = returnUrl;
                return View(input);
            }

            // Driver-specific validation to mirror filter enforcement
            if (isDriver)
            {
                if (string.IsNullOrWhiteSpace(input.Address)) ModelState.AddModelError(nameof(input.Address), "Address is required for drivers.");
                bool hasVehicle = !string.IsNullOrWhiteSpace(input.VehicleDetails) || (!string.IsNullOrWhiteSpace(input.VehicleMake) && !string.IsNullOrWhiteSpace(input.VehicleModel) && input.VehicleYear.HasValue);
                if (!hasVehicle) ModelState.AddModelError(nameof(input.VehicleMake), "Vehicle details are required for drivers.");
                if (string.IsNullOrWhiteSpace(input.LicensePlate)) ModelState.AddModelError(nameof(input.LicensePlate), "License plate is required for drivers.");
                if (string.IsNullOrWhiteSpace(input.DriversLicenseNumber) || !input.DriversLicenseExpiry.HasValue) ModelState.AddModelError(nameof(input.DriversLicenseNumber), "Driver's license number and expiry are required.");
                if (string.IsNullOrWhiteSpace(input.UpiId)) ModelState.AddModelError(nameof(input.UpiId), "UPI ID is required for drivers.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.IsDriver = isDriver;
                ViewBag.ReturnUrl = returnUrl;
                return View(input);
            }

            // Map back to user
            user.FullName = input.FullName;
            user.PhoneNumber = input.PhoneNumber;
            user.Address = input.Address;
            user.ProfilePictureUrl = input.ProfilePictureUrl;
            user.VehicleMake = input.VehicleMake;
            user.VehicleModel = input.VehicleModel;
            user.VehicleYear = input.VehicleYear;
            user.VehicleColor = input.VehicleColor;
            user.LicensePlate = input.LicensePlate;
            user.DriversLicenseNumber = input.DriversLicenseNumber;
            user.DriversLicenseExpiry = input.DriversLicenseExpiry;
            user.IsAvailable = input.IsAvailable;
            user.UpiId = input.UpiId;
            user.PassengerRideSummary = input.PassengerRideSummary;
            user.VehicleDetails = input.VehicleDetails;

            // Persist via UserManager and ensure changes are saved via DbContext
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                ViewBag.IsDriver = isDriver;
                ViewBag.ReturnUrl = returnUrl;
                return View(input);
            }

            // Extra safety: save changes for custom fields
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";

            // Redirect back to Edit so user sees persisted values immediately
            return RedirectToAction(nameof(Edit));
        }
    }
}
