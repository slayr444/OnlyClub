using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;

namespace OnlyClub.Infrastructure.Repositories;

public class PromoterRepository(OnlyClubDbContext db) : IPromoterRepository
{
    public Task<Promoter?> GetByCodeAsync(string code, CancellationToken ct) => db.Promoters.FirstOrDefaultAsync(p => p.Code == code, ct);
    public Task<Promoter?> GetByGuestIdAsync(Guid guestId, CancellationToken ct) => db.Promoters.FirstOrDefaultAsync(p => p.GuestId == guestId, ct);
    public Task<Promoter?> GetAsync(Guid id, CancellationToken ct) => db.Promoters.FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task AddTouchAsync(PromoterTouch t, CancellationToken ct) => await db.PromoterTouches.AddAsync(t, ct);
    public Task<PromoterTouch?> GetFirstTouchAsync(Guid guestId, DateTime windowStart, DateTime windowEnd, CancellationToken ct) =>
        db.PromoterTouches.Where(t => t.GuestId == guestId && t.TouchedAt >= windowStart && t.TouchedAt <= windowEnd)
            .OrderBy(t => t.TouchedAt).FirstOrDefaultAsync(ct);
}
