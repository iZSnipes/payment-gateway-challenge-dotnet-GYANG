using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Models.Requests;
using Xunit;

namespace PaymentGateway.Api.Tests.Unit.Validators;

public class PostPaymentRequestValidationTests
{
    public static IEnumerable<object[]> ExpiryYearTestCases()
    {
        var now = DateTime.Now;

        return new[]
        {
            new object[] { now.Year - 1, now.Month, false, "Past year" },
            new object[] { now.Year, now.Month, true, "Current year" },
            new object[] { now.Year + 1, now.Month, true, "Future year" },
            new object[] { 9999, now.Month, true, "Maximum year" },
            new object[] { 10000, now.Month, false, "Above maximum" }
        };
    }

    public static IEnumerable<object[]> FutureExpiryDateTestCases()
    {
        var now = DateTime.Now;
        var past = now.AddMonths(-1);
        var future = now.AddMonths(1);

        return new[]
        {
            new object[] { past.Year, past.Month, false, "Past month" },
            new object[] { now.Year, now.Month, true, "Current month" },
            new object[] { future.Year, future.Month, true, "Future date" }
        };
    }

    private ValidationContext CreateValidationContext(PostPaymentRequest request)
    {
        return new ValidationContext(request);
    }

    private PostPaymentRequest CreateValidRequest(DateTime? referenceDate = null)
    {
        var now = referenceDate ?? DateTime.Now;
        var futureDate = now.AddMonths(1);

        return new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = futureDate.Month,
            ExpiryYear = futureDate.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        // Arrange
        var request = CreateValidRequest();
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("123", false, "Too short - 3 digits")]
    [InlineData("12345678901234", true, "Valid - 14 digits (minimum)")]
    [InlineData("2222405343248877", true, "Valid - 16 digits (typical)")]
    [InlineData("1234567890123456789", true, "Valid - 19 digits (maximum)")]
    [InlineData("12345678901234567890", false, "Too long - 20 digits")]
    [InlineData("abcd567890123456", false, "Contains letters")]
    [InlineData("1234 5678 9012 3456", false, "Contains spaces")]
    [InlineData("", false, "Empty string")]
    public void CardNumber_Validation(string cardNumber, bool shouldBeValid, string scenario)
    {
        // Arrange
        var request = CreateValidRequest();
        request.CardNumber = cardNumber;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("CardNumber"));
        }
    }

    [Theory]
    [InlineData(0, false, "Below minimum")]
    [InlineData(1, true, "Valid - January")]
    [InlineData(6, true, "Valid - June")]
    [InlineData(12, true, "Valid - December (maximum)")]
    [InlineData(13, false, "Above maximum")]
    public void ExpiryMonth_Validation(int expiryMonth, bool shouldBeValid, string scenario)
    {
        // Arrange
        var request = CreateValidRequest();
        request.ExpiryMonth = expiryMonth;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("ExpiryMonth"));
        }
    }

    [Theory]
    [MemberData(nameof(ExpiryYearTestCases))]
    public void ExpiryYear_Validation(int expiryYear, int expiryMonth, bool shouldBeValid, string scenario)
    {
        // Arrange
        var request = CreateValidRequest();
        request.ExpiryYear = expiryYear;
        request.ExpiryMonth = expiryMonth;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("ExpiryYear"));
        }
    }

    [Theory]
    [InlineData("GBP", true, "Valid - GBP")]
    [InlineData("USD", true, "Valid - USD")]
    [InlineData("EUR", true, "Valid - EUR")]
    [InlineData("gbp", true, "Valid - lowercase")]
    [InlineData("JPY", false, "Invalid currency")]
    [InlineData("XX", false, "Too short")]
    [InlineData("GBPX", false, "Too long")]
    [InlineData("", false, "Empty string")]
    public void Currency_Validation(string currency, bool shouldBeValid, string scenario)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Currency = currency;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("Currency"));
        }
    }

    [Theory]
    [InlineData(0, false, "Zero amount")]
    [InlineData(-100, false, "Negative amount")]
    [InlineData(1, true, "Minimum valid amount")]
    [InlineData(100, true, "Typical amount")]
    [InlineData(999999, true, "Large amount")]
    public void Amount_Validation(int amount, bool shouldBeValid, string scenario)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Amount = amount;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("Amount"));
        }
    }

    [Theory]
    [InlineData("123", true, "Valid - 3 digits")]
    [InlineData("1234", true, "Valid - 4 digits (Amex)")]
    [InlineData("12", false, "Too short")]
    [InlineData("12345", false, "Too long")]
    [InlineData("abc", false, "Contains letters")]
    [InlineData("12 3", false, "Contains spaces")]
    [InlineData("", false, "Empty string")]
    public void Cvv_Validation(string cvv, bool shouldBeValid, string scenario)
    {
        // Arrange
        var request = CreateValidRequest();
        request.Cvv = cvv;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("Cvv"));
        }
    }

    [Theory]
    [MemberData(nameof(FutureExpiryDateTestCases))]
    public void FutureExpiryDate_Validation(int year, int month, bool shouldBeValid, string scenario)
    {
        // Arrange
        var request = CreateValidRequest();
        request.ExpiryYear = year;
        request.ExpiryMonth = month;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid)
        {
            Assert.NotEmpty(results);
        }
    }

    [Fact]
    public void NullCardNumber_FailsValidation()
    {
        // Arrange
        var request = CreateValidRequest();
        request.CardNumber = null;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("CardNumber"));
    }

    [Fact]
    public void NullCurrency_FailsValidation()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Currency = null;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Currency"));
    }

    [Fact]
    public void NullCvv_FailsValidation()
    {
        // Arrange
        var request = CreateValidRequest();
        request.Cvv = null;
        var context = CreateValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains("Cvv"));
    }
}
