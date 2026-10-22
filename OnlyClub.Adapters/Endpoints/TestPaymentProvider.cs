using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlyClub.Application.Ports;

namespace OnlyClub.Adapters;

public sealed class TestPaymentProvider : IPaymentProvider
{
    private readonly ILogger<TestPaymentProvider> _log;

    public TestPaymentProvider(ILogger<TestPaymentProvider> log, IConfiguration config)
    {
        if (!(bool)(config.GetValue<bool?>("Payments:AllowTestProvider") ?? false))
            throw new InvalidOperationException(
                "TestPaymentProvider is disabled. Set 'Payments:AllowTestProvider=true' only in dev/test.");

        _log = log;
    }

    public Task<PaymentResult> ChargeAsync(decimal amount, string idempotencyKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var reference = "tp_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();
        _log.LogWarning("TEST PAYMENT charged {Amount} idempotencyKey={Idem} ref={Ref}",
            amount, idempotencyKey, reference);

        return Task.FromResult(new PaymentResult(true, reference, null));
    }

    public Task<RefundResult> RefundAsync(string providerRef, decimal amount, string idempotencyKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(providerRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var refundRef = "rf_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant();

        _log.LogWarning("TEST PAYMENT refunded {Amount} for providerRef={ProviderRef} idempotencyKey={Idem} refundRef={RefundRef}",
            amount, providerRef, idempotencyKey, refundRef);

        return Task.FromResult(new RefundResult(true, refundRef, null));
    }
}