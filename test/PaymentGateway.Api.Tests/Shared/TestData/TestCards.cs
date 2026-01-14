namespace PaymentGateway.Api.Tests.Shared.TestData;

/// <summary>
/// Well-known test card numbers for consistent testing.
/// Based on acquirer simulator behavior: odd digits = authorized, even = declined, 0 = error.
/// </summary>
public static class TestCards
{
    // Authorized cards (last digit odd)
    public const string AuthorizedOdd = "2222405343248877"; // ends in 7
    public const string AuthorizedOdd2 = "4111111111111111"; // ends in 1

    // Declined cards (last digit even)
    public const string DeclinedEven = "4242424242424242"; // ends in 2
    public const string DeclinedEven2 = "5555555555554444"; // ends in 4

    // Acquirer error card (ends in 0)
    public const string AcquirerErrorCard = "1111111111111110";

    // Edge cases
    public const string MinimumLength = "12345678901234"; // 14 digits
    public const string MaximumLength = "1234567890123456789"; // 19 digits
}
