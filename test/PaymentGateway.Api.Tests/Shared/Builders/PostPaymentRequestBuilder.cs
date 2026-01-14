using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Tests.Shared.TestData;

namespace PaymentGateway.Api.Tests.Shared.Builders;

/// <summary>
/// Fluent builder for creating PostPaymentRequest test data.
/// Provides sensible defaults for all fields to minimize test setup.
/// </summary>
public class PostPaymentRequestBuilder
{
    private string _cardNumber = TestCards.AuthorizedOdd; // Default: authorized
    private int _expiryMonth = 4;
    private int _expiryYear = 2026;
    private string _currency = "GBP";
    private int _amount = 100;
    private string _cvv = "123";

    public PostPaymentRequestBuilder WithCardNumber(string cardNumber)
    {
        _cardNumber = cardNumber;
        return this;
    }

    public PostPaymentRequestBuilder WithAuthorizedCard()
    {
        _cardNumber = TestCards.AuthorizedOdd;
        return this;
    }

    public PostPaymentRequestBuilder WithDeclinedCard()
    {
        _cardNumber = TestCards.DeclinedEven;
        return this;
    }

    public PostPaymentRequestBuilder WithAcquirerErrorCard()
    {
        _cardNumber = TestCards.AcquirerErrorCard;
        return this;
    }

    public PostPaymentRequestBuilder WithExpiryMonth(int expiryMonth)
    {
        _expiryMonth = expiryMonth;
        return this;
    }

    public PostPaymentRequestBuilder WithExpiryYear(int expiryYear)
    {
        _expiryYear = expiryYear;
        return this;
    }

    public PostPaymentRequestBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public PostPaymentRequestBuilder WithAmount(int amount)
    {
        _amount = amount;
        return this;
    }

    public PostPaymentRequestBuilder WithCvv(string cvv)
    {
        _cvv = cvv;
        return this;
    }

    public PostPaymentRequest Build() => new()
    {
        CardNumber = _cardNumber,
        ExpiryMonth = _expiryMonth,
        ExpiryYear = _expiryYear,
        Currency = _currency,
        Amount = _amount,
        Cvv = _cvv
    };
}
