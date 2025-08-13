using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Payment_method.Services;

namespace Payment_method.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _svc;
    private readonly IConfiguration _cfg;

    public PaymentsController(IPaymentService svc, IConfiguration cfg)
    {
        _svc = svc;
        _cfg = cfg;
    }

    // ===== DTOs =====
    public record PaymentSessionRequest(long OrderId, string? Method);
    public record RazorpayVerifyRequest(string razorpay_order_id, string razorpay_payment_id, string razorpay_signature);

    // ===== Create payment session (MOCK or Razorpay) =====
    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] PaymentSessionRequest body, CancellationToken ct)
    {
        var method = string.IsNullOrWhiteSpace(body.Method) ? "UPI" : body.Method!;
        var result = await _svc.CreatePaymentSessionAsync(body.OrderId, method, ct);
        return Ok(result);
    }

    // ===== Client-side verification after Razorpay checkout =====
    [HttpPost("razorpay/verify")]
    public async Task<IActionResult> Verify([FromBody] RazorpayVerifyRequest payload, CancellationToken ct)
    {
        var secret = _cfg["Payments:Razorpay:KeySecret"] ?? string.Empty;
        if (!VerifyRazorpayPaymentSig(payload.razorpay_order_id, payload.razorpay_payment_id, payload.razorpay_signature, secret))
            return BadRequest("Invalid payment signature");

        await _svc.MarkSuccessAsync(payload.razorpay_order_id, payload.razorpay_payment_id, payload.razorpay_signature, ct);
        return Ok(new { status = "ok" });
    }

    // ===== Webhook (source of truth) =====
    [HttpPost("webhook/razorpay")]
    public async Task<IActionResult> Webhook(
        [FromHeader(Name = "X-Razorpay-Signature")] string signature,
        CancellationToken ct)
    {
        // Read raw request body
        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync();

        var secret = _cfg["Payments:Webhook:RazorpaySecret"] ?? string.Empty;

        if (!VerifyRazorpayWebhook(payload, signature, secret))
            return BadRequest("Invalid webhook signature");

        var json = JsonDocument.Parse(payload).RootElement;
        var evt = json.GetProperty("event").GetString();

        if (evt == "payment.captured")
        {
            var entity = json.GetProperty("payload").GetProperty("payment").GetProperty("entity");
            var orderId = entity.GetProperty("order_id").GetString()!;
            var paymentId = entity.GetProperty("id").GetString()!;
            await _svc.MarkSuccessAsync(orderId, paymentId, signature, ct);
        }

        return Ok();
    }

    // ===== HMAC helpers =====
    private static bool VerifyRazorpayWebhook(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }

    private static bool VerifyRazorpayPaymentSig(string orderId, string paymentId, string signature, string secret)
    {
        var data = $"{orderId}|{paymentId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }
}
