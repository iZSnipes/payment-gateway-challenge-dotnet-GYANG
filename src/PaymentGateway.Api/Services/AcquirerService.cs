using System.Net.Http.Json;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Acquirer;

namespace PaymentGateway.Api.Services;

/// <summary>
/// Service for processing payments with the acquirer.
/// </summary>
public class AcquirerService : IAcquirerService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AcquirerService> _logger;

    public AcquirerService(IHttpClientFactory httpClientFactory, ILogger<AcquirerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AcquirerPaymentResponse> ProcessPaymentAsync(AcquirerPaymentRequest request)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("AcquirerClient");

            _logger.LogInformation("Sending payment request to acquirer for amount {Amount} {Currency}",
                request.Amount, request.Currency);

            var response = await httpClient.PostAsJsonAsync("/payments", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Acquirer returned error status code: {StatusCode}", response.StatusCode);
                throw new AcquirerServiceUnavailableException($"Acquirer returned {response.StatusCode}");
            }

            var bankResponse = await response.Content.ReadFromJsonAsync<AcquirerPaymentResponse>();

            if (bankResponse == null)
            {
                _logger.LogError("Failed to deserialize acquirer response");
                throw new AcquirerServiceUnavailableException("Invalid response from acquirer");
            }

            _logger.LogInformation("Acquirer response received: Authorized={Authorized}", bankResponse.Authorized);

            return bankResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error communicating with acquirer");
            throw new AcquirerServiceUnavailableException("Unable to connect to acquirer", ex);
        }
        catch (AcquirerServiceUnavailableException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment with acquirer");
            throw new AcquirerServiceUnavailableException("Unexpected error communicating with acquirer", ex);
        }
    }
}
