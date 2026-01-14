namespace PaymentGateway.Api.Exceptions;

/// <summary>
/// Exception thrown when the acquirer service is unavailable.
/// </summary>
public class AcquirerServiceUnavailableException : Exception
{
    public AcquirerServiceUnavailableException()
    {
    }

    public AcquirerServiceUnavailableException(string message)
        : base(message)
    {
    }

    public AcquirerServiceUnavailableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
