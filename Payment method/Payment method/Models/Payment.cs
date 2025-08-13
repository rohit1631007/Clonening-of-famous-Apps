namespace Payment_method.Models;

public class Payment
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string Provider { get; set; } = "RAZORPAY";
    public string Method { get; set; } = "UPI";
    public string Currency { get; set; } = "INR";
    public long Amount { get; set; }
    public string Status { get; set; } = "INITIATED";
    public string? ProviderOrderId { get; set; }   // <-- needed
    public string? ProviderPaymentId { get; set; } // <-- used in MarkSuccessAsync
    public string? Signature { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
