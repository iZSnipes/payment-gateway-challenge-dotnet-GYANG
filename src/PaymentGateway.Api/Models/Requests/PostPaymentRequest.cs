using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Models.Requests;

[FutureExpiryDate(ErrorMessage = "Card expiry date must be in the future")]
public class PostPaymentRequest
{
    [Required(ErrorMessage = "Card number is required")]
    [StringLength(19, MinimumLength = 14, ErrorMessage = "Card number must be 14-19 digits")]
    [RegularExpression(@"^\d{14,19}$", ErrorMessage = "Card number must contain only digits")]
    public string CardNumber { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12")]
    public int ExpiryMonth { get; set; }

    [Required]
    [Range(1, 9999, ErrorMessage = "Expiry year must be a valid year")]
    public int ExpiryYear { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be 3 characters")]
    [AllowedCurrencies(ErrorMessage = "Currency must be GBP, USD, or EUR")]
    public string Currency { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be positive")]
    public int Amount { get; set; }

    [Required(ErrorMessage = "CVV is required")]
    [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV must be 3-4 digits")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must contain only digits")]
    public string Cvv { get; set; }
}
