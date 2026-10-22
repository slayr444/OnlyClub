using OnlyClub.Core.Entities;
namespace OnlyClub.Application.Ports;

public interface IPaymentRepository
{
    Task<Payment?> GetByProviderRefAsync(string providerRef, CancellationToken ct);
    Task AddAsync(Payment p, CancellationToken ct);
}
