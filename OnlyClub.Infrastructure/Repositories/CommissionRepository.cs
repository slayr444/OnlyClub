using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;

namespace OnlyClub.Infrastructure.Repositories;

public class CommissionRepository(OnlyClubDbContext db) : ICommissionRepository
{
    public Task<Commission?> GetAsync(Guid id, CancellationToken ct) => db.Commissions.Include(c => c.Lines).FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<Commission?> GetForPromoterEventAsync(Guid promoterId, Guid eventId, CancellationToken ct) =>
        db.Commissions.Include(c => c.Lines).FirstOrDefaultAsync(c => c.PromoterId == promoterId && c.EventId == eventId, ct);
    public Task<List<Commission>> ForEventAsync(Guid eventId, CancellationToken ct) =>
        db.Commissions.Include(c => c.Lines).Where(c => c.EventId == eventId).ToListAsync(ct);
    public async Task AddAsync(Commission c, CancellationToken ct) => await db.Commissions.AddAsync(c, ct);
}
