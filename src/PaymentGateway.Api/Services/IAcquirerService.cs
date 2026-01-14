using PaymentGateway.Api.Models.Acquirer;

namespace PaymentGateway.Api.Services;

/// <summary>
/// Interface for acquirer payment processing.
/// </summary>
public interface IAcquirerService
{
    /// <summary>
    /// Processes a payment request with the acquirer.
    /// </summary>
    Task<AcquirerPaymentResponse> ProcessPaymentAsync(AcquirerPaymentRequest request);
}
