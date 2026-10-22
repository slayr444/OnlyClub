using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;

namespace OnlyClub.Infrastructure.Repositories;

public class PaymentRepository(OnlyClubDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByProviderRefAsync(string providerRef, CancellationToken ct) =>
        db.Payments.FirstOrDefaultAsync(p => p.ProviderRef == providerRef, ct);
    public async Task AddAsync(Payment p, CancellationToken ct) => await db.Payments.AddAsync(p, ct);
}
