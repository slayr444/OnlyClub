using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Infrastructure.Repositories;

public class GuestRepository(OnlyClubDbContext db) : IGuestRepository
{
    public Task<Guest?> GetByTelegramIdAsync(long tg, CancellationToken ct) => db.Guests.FirstOrDefaultAsync(g => g.TelegramId == tg, ct);
    public Task<Guest?> GetAsync(Guid id, CancellationToken ct) => db.Guests.FirstOrDefaultAsync(g => g.Id == id, ct);
    public async Task<Guest> GetOrCreateAsync(long tg, string name, Role role, CancellationToken ct)
    {
        var g = await GetByTelegramIdAsync(tg, ct);
        if (g is not null) return g;
        g = new Guest { TelegramId = tg, Name = name, Role = role };
        db.Guests.Add(g); await db.SaveChangesAsync(ct);
        return g;
    }
}
