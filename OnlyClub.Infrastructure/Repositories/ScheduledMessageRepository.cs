using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Infrastructure.Repositories;

public class ScheduledMessageRepository(OnlyClubDbContext db) : IScheduledMessageRepository
{
    public async Task AddAsync(ScheduledMessage m, CancellationToken ct) => await db.ScheduledMessages.AddAsync(m, ct);
    public Task<List<ScheduledMessage>> GetDueAsync(DateTime now, int limit, CancellationToken ct) =>
        db.ScheduledMessages.Where(m => m.Status == ScheduledMessageStatus.Pending && m.ScheduledAt <= now)
            .OrderBy(m => m.ScheduledAt).Take(limit).ToListAsync(ct);
    public Task<bool> HasRecentMarketingAsync(Guid guestId, DateTime since, CancellationToken ct) =>
        db.ScheduledMessages.AnyAsync(m => m.GuestId == guestId && m.Type == ScheduledMessageType.NextEventAnnouncement && m.SentAt >= since, ct);
    public async Task CancelRelatedAsync(Guid relatedId, CancellationToken ct)
    {
        var ms = await db.ScheduledMessages.Where(m => m.RelatedId == relatedId && m.Status == ScheduledMessageStatus.Pending).ToListAsync(ct);
        foreach (var m in ms) m.Status = ScheduledMessageStatus.Cancelled;
    }
    public Task<List<ScheduledMessage>> ForGuestAsync(Guid guestId, CancellationToken ct) =>
        db.ScheduledMessages.Where(m => m.GuestId == guestId).ToListAsync(ct);
}