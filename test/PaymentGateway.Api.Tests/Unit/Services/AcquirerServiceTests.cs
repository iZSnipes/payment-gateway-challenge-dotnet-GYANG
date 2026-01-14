using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Acquirer;
using PaymentGateway.Api.Services;
using Xunit;

namespace PaymentGateway.Api.Tests.Unit.Services;

public class AcquirerServiceTests
{
    private readonly Mock<ILogger<AcquirerService>> _mockLogger;

    public AcquirerServiceTests()
    {
        _mockLogger = new Mock<ILogger<AcquirerService>>();
    }

    private AcquirerPaymentRequest CreateValidAcquirerRequest()
    {
        return new AcquirerPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryDate = "04/2026",
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }

    [Fact]
    public async Task ProcessPaymentAsync_SuccessfulAuthorization_ReturnsAuthorizedResponse()
    {
        // Arrange
        var expectedResponse = new AcquirerPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var acquirerService = new AcquirerService(mockHttpClientFactory.Object, _mockLogger.Object);
        var request = CreateValidAcquirerRequest();

        // Act
        var result = await acquirerService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Authorized);
        Assert.Equal("AUTH123", result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DeclinedPayment_ReturnsDeclinedResponse()
    {
        // Arrange
        var expectedResponse = new AcquirerPaymentResponse
        {
            Authorized = false,
            AuthorizationCode = null
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedResponse)
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var acquirerService = new AcquirerService(mockHttpClientFactory.Object, _mockLogger.Object);
        var request = CreateValidAcquirerRequest();

        // Act
        var result = await acquirerService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Authorized);
    }

    [Fact]
    public async Task ProcessPaymentAsync_AcquirerReturns500_ThrowsAcquirerServiceUnavailableException()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var acquirerService = new AcquirerService(mockHttpClientFactory.Object, _mockLogger.Object);
        var request = CreateValidAcquirerRequest();

        // Act & Assert
        await Assert.ThrowsAsync<AcquirerServiceUnavailableException>(
            async () => await acquirerService.ProcessPaymentAsync(request));
    }

    [Fact]
    public async Task ProcessPaymentAsync_NetworkFailure_ThrowsAcquirerServiceUnavailableException()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var acquirerService = new AcquirerService(mockHttpClientFactory.Object, _mockLogger.Object);
        var request = CreateValidAcquirerRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AcquirerServiceUnavailableException>(
            async () => await acquirerService.ProcessPaymentAsync(request));

        Assert.Contains("Unable to connect to acquirer", exception.Message);
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCorrectRequestToAcquirer()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new AcquirerPaymentResponse { Authorized = true })
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var acquirerService = new AcquirerService(mockHttpClientFactory.Object, _mockLogger.Object);
        var request = CreateValidAcquirerRequest();

        // Act
        await acquirerService.ProcessPaymentAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("http://localhost:8080/payments", capturedRequest.RequestUri?.ToString());
        Assert.NotNull(capturedRequest.Content);
    }
}
