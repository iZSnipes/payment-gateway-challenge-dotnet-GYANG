using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Acquirer;

/// <summary>
/// Payment response DTO from acquirer API.
/// </summary>
public class AcquirerPaymentResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; set; }
}
