using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Infrastructure.Repositories;

public class GuestListRepository(OnlyClubDbContext db) : IGuestListRepository
{
    public Task<GuestListEntry?> GetAsync(Guid id, CancellationToken ct) =>
        db.GuestListEntries.Include(e => e.Event).Include(e => e.Guest).FirstOrDefaultAsync(e => e.Id == id, ct);
    public Task<GuestListEntry?> GetActiveForGuestAsync(Guid eventId, Guid guestId, CancellationToken ct) =>
        db.GuestListEntries.FirstOrDefaultAsync(e => e.EventId == eventId && e.GuestId == guestId && e.Status != GuestListStatus.Cancelled, ct);
    public Task<int> CountAsync(Guid eventId, CancellationToken ct) =>
        db.GuestListEntries.CountAsync(e => e.EventId == eventId && e.ParentEntryId == null && e.Status != GuestListStatus.Cancelled, ct);
    public Task<int> CountCompanionsAsync(Guid parentEntryId, CancellationToken ct) =>
        db.GuestListEntries.CountAsync(e => e.ParentEntryId == parentEntryId && e.Status != GuestListStatus.Cancelled, ct);
    public Task<List<GuestListEntry>> ForEventAsync(Guid eventId, CancellationToken ct) =>
        db.GuestListEntries.Include(e => e.Guest).Where(e => e.EventId == eventId).ToListAsync(ct);
    public async Task AddAsync(GuestListEntry e, CancellationToken ct) => await db.GuestListEntries.AddAsync(e, ct);
}
