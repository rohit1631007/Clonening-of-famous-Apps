using Microsoft.AspNetCore.Mvc;
using Payment_method.Data;
using Payment_method.Models;

namespace Payment_method.Controllers;

[ApiController]
[Route("api/checkout")]
public class CheckoutController : ControllerBase
{
    private readonly Payment_methodDb _db;
    public CheckoutController(Payment_methodDb db) { _db = db; }

    public record CheckoutRequest(string UserId, string RestaurantId, long Subtotal, long Tax, long DeliveryFee, long Tip, long Discount, string? CouponCode);

    [HttpPost("create-order")]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CheckoutRequest req, CancellationToken ct)
    {
        var total = req.Subtotal + req.Tax + req.DeliveryFee + req.Tip - req.Discount;
        var o = new Order
        {
            UserId = req.UserId,
            RestaurantId = req.RestaurantId,
            Subtotal = req.Subtotal,
            Tax = req.Tax,
            DeliveryFee = req.DeliveryFee,
            Tip = req.Tip,
            Discount = req.Discount,
            Total = total,
            CouponCode = req.CouponCode
        };
        _db.Orders.Add(o);
        await _db.SaveChangesAsync(ct);
        return Ok(o);
    }
}
