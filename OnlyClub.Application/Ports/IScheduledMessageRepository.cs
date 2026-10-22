using OnlyClub.Core.Entities;
namespace OnlyClub.Application.Ports;

public interface IScheduledMessageRepository
{
    Task AddAsync(ScheduledMessage m, CancellationToken ct);
    Task<List<ScheduledMessage>> GetDueAsync(DateTime now, int limit, CancellationToken ct);
    Task<bool> HasRecentMarketingAsync(Guid guestId, DateTime since, CancellationToken ct);
    Task CancelRelatedAsync(Guid relatedId, CancellationToken ct);
    Task<List<ScheduledMessage>> ForGuestAsync(Guid guestId, CancellationToken ct);
}
