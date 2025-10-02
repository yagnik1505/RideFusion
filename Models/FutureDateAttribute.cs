using System.ComponentModel.DataAnnotations;

namespace RideFusion.Models
{
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime > DateTime.Now;
            }
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return "Please enter a future date and time. Past dates are not allowed.";
        }
    }
}