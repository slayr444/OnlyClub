namespace OnlyClub.Application.Ports;

public record RefundResult(bool Success, string? RefundRef, string? Error);

public interface IPaymentProvider
{
    Task<PaymentResult> ChargeAsync(decimal amount, string idempotencyKey, CancellationToken ct);
    Task<RefundResult> RefundAsync(string providerRef, decimal amount, string idempotencyKey, CancellationToken ct);
}