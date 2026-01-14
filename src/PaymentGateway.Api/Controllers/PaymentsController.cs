using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Acquirer;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IAcquirerService _acquirerService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        PaymentsRepository paymentsRepository,
        IAcquirerService acquirerService,
        ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _acquirerService = acquirerService;
        _logger = logger;
    }

    [HttpGet("{id:guid}", Name = "GetPaymentById")]
    public async Task<ActionResult<PaymentResponse?>> GetPaymentAsync(Guid id)
    {
        _logger.LogInformation("Retrieving payment with ID: {PaymentId}", id);

        var payment = _paymentsRepository.Get(id);

        if (payment == null)
        {
            _logger.LogWarning("Payment not found: {PaymentId}", id);
            return NotFound();
        }

        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> PostPaymentAsync([FromBody] PostPaymentRequest request)
    {
        _logger.LogInformation("Processing payment request for amount {Amount} {Currency}",
            request.Amount, request.Currency);

        // Validation is automatic via ModelState
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid payment request: {ValidationErrors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            var problemDetails = new ValidationProblemDetails(ModelState);
            problemDetails.Extensions["paymentStatus"] = PaymentStatus.Rejected.ToString();
            return BadRequest(problemDetails);
        }

        try
        {
            // Call acquirer to authorize payment
            var acquirerRequest = new AcquirerPaymentRequest
            {
                CardNumber = request.CardNumber,
                ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",  // Format: MM/YYYY
                Currency = request.Currency,
                Amount = request.Amount,
                Cvv = request.Cvv
            };

            var acquirerResponse = await _acquirerService.ProcessPaymentAsync(acquirerRequest);

            // Mask card number to last 4 digits for PCI compliance
            var cardNumberLastFour = int.Parse(request.CardNumber.Substring(request.CardNumber.Length - 4));

            // Create and store payment record
            var payment = new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = acquirerResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
                CardNumberLastFour = cardNumberLastFour,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount
            };

            _paymentsRepository.Add(payment);

            _logger.LogInformation("Payment processed successfully: {PaymentId}, Status: {Status}",
                payment.Id, payment.Status);

            // Return 201 with location header
            return CreatedAtRoute("GetPaymentById", new { id = payment.Id }, payment);
        }
        catch (AcquirerServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Acquirer service unavailable while processing payment");
            return StatusCode(502, new { error = "Acquirer service is currently unavailable. Please try again later." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment");
            return StatusCode(500, new { error = "An unexpected error occurred while processing your payment." });
        }
    }
}
