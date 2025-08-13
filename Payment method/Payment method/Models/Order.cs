namespace Payment_method.Models;

public class Order
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string RestaurantId { get; set; } = "";
    public long Subtotal { get; set; }      // in paise
    public long Tax { get; set; }
    public long DeliveryFee { get; set; }
    public long Tip { get; set; }
    public long Discount { get; set; }
    public long Total { get; set; }
    public string Status { get; set; } = "CREATED"; // CREATED, PENDING_PAYMENT, PAID, FAILED
    public string? CouponCode { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
