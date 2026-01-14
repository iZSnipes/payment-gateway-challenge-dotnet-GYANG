using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Constants;

namespace PaymentGateway.Api.Validators;

public class AllowedCurrenciesAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is string currency && PaymentConstants.AllowedCurrencies.All.Contains(currency.ToUpper()))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            ErrorMessage ?? $"Currency must be one of: {string.Join(", ", PaymentConstants.AllowedCurrencies.All)}",
            new[] { validationContext.MemberName ?? "Currency" });
    }
}
