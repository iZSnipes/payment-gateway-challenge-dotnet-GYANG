using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for PaymentsController using WebApplicationFactory.
/// Tests the full HTTP pipeline including routing, model binding, validation, and middleware.
/// REQUIRES: Acquirer simulator running on localhost:8080 (docker-compose up)
/// </summary>
public class PaymentsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    // Match API enum-as-string JSON serialization in tests.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true  // Allow case-insensitive property matching
    };

    public PaymentsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    #region GET /api/payments/{id} Tests

    [Fact]
    public async Task Get_ExistingPayment_Returns200WithPayment()
    {
        // Arrange
        var client = _factory.CreateClient();
        var future = DateTime.Now.AddMonths(1);

        // First, create a payment via POST (card ending in odd = authorized)
        var postRequest = new PostPaymentRequest
        {
            CardNumber = "2222405343248877", // Ends in 7 (odd) - will be authorized
            ExpiryMonth = future.Month,
            ExpiryYear = future.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        var postResponse = await client.PostAsJsonAsync("/api/Payments", postRequest);
        var createdPayment = await postResponse.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        // Act - Retrieve the payment we just created
        var getResponse = await client.GetAsync($"/api/Payments/{createdPayment!.Id}");
        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(retrievedPayment);
        Assert.Equal(createdPayment.Id, retrievedPayment.Id);
        Assert.Equal(PaymentStatus.Authorized, retrievedPayment.Status);
        Assert.Equal(8877, retrievedPayment.CardNumberLastFour);
    }

    [Fact]
    public async Task Get_NonExistentPayment_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/Payments/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/payments Tests

    [Fact]
    public async Task Post_AuthorizedCard_Returns201Created()
    {
        // Arrange
        var client = _factory.CreateClient();
        var future = DateTime.Now.AddMonths(1);
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877", // Ends in 7 (odd) - authorized by acquirer
            ExpiryMonth = future.Month,
            ExpiryYear = future.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal(8877, payment.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, payment.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, payment.ExpiryYear);
        Assert.Equal(request.Currency, payment.Currency);
        Assert.Equal(request.Amount, payment.Amount);
        Assert.NotEqual(Guid.Empty, payment.Id);
    }

    [Fact]
    public async Task Post_DeclinedCard_Returns201WithDeclinedStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        var future = DateTime.Now.AddMonths(1);
        var request = new PostPaymentRequest
        {
            CardNumber = "4242424242424242", // Ends in 2 (even) - declined by acquirer
            ExpiryMonth = future.Month,
            ExpiryYear = future.Year,
            Currency = "USD",
            Amount = 250,
            Cvv = "456"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Declined, payment.Status);
        Assert.Equal(4242, payment.CardNumberLastFour);
    }

    [Fact]
    public async Task Post_InvalidCardNumber_Returns400WithValidationErrors()
    {
        // Arrange
        var client = _factory.CreateClient();
        var future = DateTime.Now.AddMonths(1);
        var request = new PostPaymentRequest
        {
            CardNumber = "123",  // Too short - validation failure
            ExpiryMonth = future.Month,
            ExpiryYear = future.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Standard ASP.NET Core ValidationProblemDetails response
        Assert.True(payload.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("CardNumber", out _));

        // Note: "Rejected" status handling is simplified per Decision 12
        // ASP.NET Core returns standard ValidationProblemDetails for 400 errors
        // Merchants distinguish Rejected from Authorized/Declined via HTTP status code (400 vs 201)
    }

    [Fact]
    public async Task Post_ReturnsLocationHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var future = DateTime.Now.AddMonths(1);
        var request = new PostPaymentRequest
        {
            CardNumber = "5555555555554444", // Ends in 4 (even) - declined but still creates payment
            ExpiryMonth = future.Month,
            ExpiryYear = future.Year,
            Currency = "EUR",
            Amount = 500,
            Cvv = "789"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(payment!.Id.ToString(), response.Headers.Location.ToString());
    }

    #endregion

    #region PCI Compliance Tests

    [Fact]
    public async Task Post_OnlyReturnsLast4Digits_NeverFullCardNumber()
    {
        // Arrange
        var client = _factory.CreateClient();
        var future = DateTime.Now.AddMonths(1);
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123451",  // Full card number (ends in 1 = authorized)
            ExpiryMonth = future.Month,
            ExpiryYear = future.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var responseBody = await response.Content.ReadAsStringAsync();
        var payment = System.Text.Json.JsonSerializer.Deserialize<PaymentResponse>(responseBody, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(3451, payment!.CardNumberLastFour);  // Last 4 only
        Assert.DoesNotContain("1234567890123451", responseBody);  // Full card NOT in response
    }

    #endregion
}
