using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validators;

public class FutureExpiryDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not PostPaymentRequest request)
        {
            return ValidationResult.Success;
        }

        DateTime expiryDate;
        try
        {
            // Let range validators handle invalid month/year
            expiryDate = new DateTime(request.ExpiryYear, request.ExpiryMonth, 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            return ValidationResult.Success;
        }

        var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        if (expiryDate >= currentDate)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            ErrorMessage ?? "Card expiry date must be in the future",
            new[] { validationContext.MemberName ?? nameof(PostPaymentRequest.ExpiryYear) });
    }
}
