using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Identity;
using RideFusion.Models;

namespace RideFusion.Filters
{
    // Usage: [ProfileCompleted]
    public class ProfileCompletedAttribute : TypeFilterAttribute
    {
        public ProfileCompletedAttribute() : base(typeof(ProfileCompletedFilter)) { }

        private class ProfileCompletedFilter : IAsyncActionFilter
        {
            private readonly UserManager<ApplicationUser> _userManager;

            public ProfileCompletedFilter(UserManager<ApplicationUser> userManager)
            {
                _userManager = userManager;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var httpUser = context.HttpContext.User;
                if (httpUser?.Identity == null || !httpUser.Identity.IsAuthenticated)
                {
                    await next();
                    return;
                }

                var user = await _userManager.GetUserAsync(httpUser);
                if (user == null)
                {
                    await next();
                    return;
                }

                var roles = await _userManager.GetRolesAsync(user);
                var isDriver = roles.Contains("Driver");

                // Common requirements for all users
                bool hasFullName = !string.IsNullOrWhiteSpace(user.FullName);
                bool hasPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber);

                bool isComplete;
                if (isDriver)
                {
                    // Driver-specific requirements
                    bool hasAddress = !string.IsNullOrWhiteSpace(user.Address);
                    bool hasVehicle = !string.IsNullOrWhiteSpace(user.VehicleDetails) ||
                                      (!string.IsNullOrWhiteSpace(user.VehicleMake) && !string.IsNullOrWhiteSpace(user.VehicleModel) && user.VehicleYear.HasValue);
                    bool hasPlate = !string.IsNullOrWhiteSpace(user.LicensePlate);
                    bool hasLicense = !string.IsNullOrWhiteSpace(user.DriversLicenseNumber) && user.DriversLicenseExpiry.HasValue;
                    bool hasUpi = !string.IsNullOrWhiteSpace(user.UpiId);

                    isComplete = hasFullName && hasPhone && hasAddress && hasVehicle && hasPlate && hasLicense && hasUpi;
                }
                else
                {
                    // Passenger requirements (lighter)
                    isComplete = hasFullName && hasPhone;
                }

                if (!isComplete)
                {
                    context.Result = new RedirectToActionResult("Edit", "Profile", new { area = "", returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString });
                    return;
                }

                await next();
            }
        }

    }
}
