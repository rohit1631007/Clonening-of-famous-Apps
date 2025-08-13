namespace Payment_method.Services;

public interface IPaymentService
{
    Task<object> CreatePaymentSessionAsync(long orderId, string method, CancellationToken ct);
    Task MarkSuccessAsync(string providerOrderId, string providerPaymentId, string signature, CancellationToken ct);
}
