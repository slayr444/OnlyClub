using OnlyClub.Core.Entities;
namespace OnlyClub.Application.Ports;

public interface IGuestListRepository
{
    Task<GuestListEntry?> GetAsync(Guid id, CancellationToken ct);
    Task<GuestListEntry?> GetActiveForGuestAsync(Guid eventId, Guid guestId, CancellationToken ct);
    Task<int> CountAsync(Guid eventId, CancellationToken ct);
    Task<int> CountCompanionsAsync(Guid parentEntryId, CancellationToken ct);
    Task<List<GuestListEntry>> ForEventAsync(Guid eventId, CancellationToken ct);
    Task AddAsync(GuestListEntry e, CancellationToken ct);
}
