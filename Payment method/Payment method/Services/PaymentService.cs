using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using System.Text.Json;

// Aliases to avoid the Order name clash
using AppOrder = Payment_method.Models.Order;
using AppPayment = Payment_method.Models.Payment;

using Payment_method.Data;

namespace Payment_method.Services;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _cfg;
    private readonly Payment_methodDb _db;

    public PaymentService(IConfiguration cfg, Payment_methodDb db)
    {
        _cfg = cfg;
        _db = db;
    }

    public async Task<object> CreatePaymentSessionAsync(long orderId, string method, CancellationToken ct)
    {
        var provider = _cfg["Payments:Provider"] ?? "MOCK";

        // Get the order from your DB
        var order = await _db.Orders.FindAsync(new object?[] { orderId }, ct)
                    ?? throw new InvalidOperationException("Order not found");

        // Move to pending state
        order.Status = "PENDING_PAYMENT";
        await _db.SaveChangesAsync(ct);

        // ---------- MOCK MODE ----------
        if (provider.Equals("MOCK", StringComparison.OrdinalIgnoreCase))
        {
            var p = await SaveInitAsync(order, method, $"MOCK_ORDER_{Guid.NewGuid()}", order.Total, ct);
            return new
            {
                provider = "MOCK",
                orderId = p.ProviderOrderId,     // <-- make sure Payment has this property
                amount = order.Total,
                currency = "INR",
                key = "MOCK_KEY"
            };
        }

        // ---------- RAZORPAY MODE ----------
        if (provider.Equals("RAZORPAY", StringComparison.OrdinalIgnoreCase))
        {
            var key = _cfg["Payments:Razorpay:KeyId"] ?? throw new InvalidOperationException("Razorpay KeyId missing");
            var secret = _cfg["Payments:Razorpay:KeySecret"] ?? throw new InvalidOperationException("Razorpay KeySecret missing");

            var client = new RazorpayClient(key, secret);

            var options = new Dictionary<string, object>
            {
                { "amount",  order.Total },           // paise
                { "currency","INR"     },
                { "receipt", $"rcpt_{order.Id}" },
                { "payment_capture", 1 }
            };

            // This is Razorpay.Api.Order (not your model)
            Order provOrder = client.Order.Create(options);

            var p = await SaveInitAsync(order, method, provOrder["id"].ToString(), order.Total, ct);

            return new
            {
                provider = "RAZORPAY",
                orderId = p.ProviderOrderId,          // store and return your saved provider order id
                amount = order.Total,                 // same as what you created
                currency = "INR",
                key = key
            };
        }

        throw new NotSupportedException("Unsupported provider");
    }

    public async Task MarkSuccessAsync(string providerOrderId, string providerPaymentId, string signature, CancellationToken ct)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(x => x.ProviderOrderId == providerOrderId, ct)
                      ?? throw new InvalidOperationException("Payment not found");

        payment.Status = "SUCCESS";
        payment.ProviderPaymentId = providerPaymentId;
        payment.Signature = signature;
        await _db.SaveChangesAsync(ct);

        var order = await _db.Orders.FindAsync(new object?[] { payment.OrderId }, ct)
                    ?? throw new InvalidOperationException("Order not found");
        order.Status = "PAID";
        await _db.SaveChangesAsync(ct);
    }

    // Helper to persist a Payment row when we start a session
    private async Task<AppPayment> SaveInitAsync(AppOrder o, string method, string providerOrderId, long amt, CancellationToken ct)
    {
        var p = new AppPayment
        {
            OrderId = o.Id,
            Method = method,
            Provider = "RAZORPAY",
            ProviderOrderId = providerOrderId,
            Amount = amt,
            Currency = "INR",
            Status = "INITIATED"
        };

        _db.Payments.Add(p);
        await _db.SaveChangesAsync(ct);
        return p;
    }
}
