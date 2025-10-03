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

                // Basic required for everyone
                bool hasFullName = !string.IsNullOrWhiteSpace(user.FullName);
                bool hasPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber);
                bool hasAddress = !string.IsNullOrWhiteSpace(user.Address);

                bool isComplete = hasFullName && hasPhone && hasAddress;

                if (isDriver)
                {
                    bool hasUpi = !string.IsNullOrWhiteSpace(user.UpiId);
                    bool hasAvailability = user.IsAvailable.HasValue; // driver must set availability
                    isComplete = isComplete && hasUpi && hasAvailability;
                }

                if (!isComplete)
                {
                    context.Result = new RedirectToActionResult(
                        "Edit",
                        "Profile",
                        new { area = "", returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString }
                    );
                    return;
                }

                await next();
            }
        }

    }
}
