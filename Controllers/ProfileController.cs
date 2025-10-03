using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RideFusion.Models;
using System.ComponentModel.DataAnnotations;
using System;
using RideFusion.Data; 
using Microsoft.Data.Sqlite; 
using System.IO;
using Microsoft.EntityFrameworkCore; 

namespace RideFusion.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProfileController> _logger;
        private readonly ApplicationDbContext _context; 

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

            // Common new fields
            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Gender")]
            public string? Gender { get; set; }

            // Driver detailed vehicle info (only shown for drivers)
            [Display(Name = "Vehicle Company")] // renamed from Vehicle Make
            public string? VehicleMake { get; set; }

            [Display(Name = "Vehicle Model")]
            public string? VehicleModel { get; set; }

            [Range(1900, 2100)]
            [Display(Name = "Vehicle Year")]
            public int? VehicleYear { get; set; }

            [Display(Name = "Vehicle Color")]
            public string? VehicleColor { get; set; }

            [Display(Name = "Plate Number")] // renamed from License Plate
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

            [Display(Name = "Ride History Summary (optional)")]
            public string? PassengerRideSummary { get; set; }

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
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
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

            // Only validate driver-specific requirements if user is driver
            if (isDriver)
            {
                if (string.IsNullOrWhiteSpace(input.UpiId)) ModelState.AddModelError(nameof(input.UpiId), "UPI ID is required for drivers.");
                if (!input.IsAvailable.HasValue) ModelState.AddModelError(nameof(input.IsAvailable), "Availability is required for drivers.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.IsDriver = isDriver;
                ViewBag.ReturnUrl = returnUrl;
                return View(input);
            }

            user.FullName = input.FullName;
            user.PhoneNumber = input.PhoneNumber;
            user.Address = input.Address;
            user.ProfilePictureUrl = input.ProfilePictureUrl;
            user.DateOfBirth = input.DateOfBirth;
            user.Gender = input.Gender;
            user.VehicleMake = isDriver ? input.VehicleMake : null;
            user.VehicleModel = isDriver ? input.VehicleModel : null;
            user.VehicleYear = isDriver ? input.VehicleYear : null;
            user.VehicleColor = isDriver ? input.VehicleColor : null;
            user.LicensePlate = isDriver ? input.LicensePlate : null;
            user.DriversLicenseNumber = isDriver ? input.DriversLicenseNumber : null;
            user.DriversLicenseExpiry = isDriver ? input.DriversLicenseExpiry : null;
            user.IsAvailable = isDriver ? input.IsAvailable : null;
            user.UpiId = isDriver ? input.UpiId : null;
            user.PassengerRideSummary = input.PassengerRideSummary;
            user.VehicleDetails = isDriver ? input.VehicleDetails : null;

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

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";

            return RedirectToAction(nameof(Edit));
        }
    }
}
