using OnlyClub.Core.Entities;
namespace OnlyClub.Application.Ports;

public interface IPromoterRepository
{
    Task<Promoter?> GetByCodeAsync(string code, CancellationToken ct);
    Task<Promoter?> GetByGuestIdAsync(Guid guestId, CancellationToken ct);
    Task<Promoter?> GetAsync(Guid id, CancellationToken ct);
    Task AddTouchAsync(PromoterTouch t, CancellationToken ct);
    Task<PromoterTouch?> GetFirstTouchAsync(Guid guestId, DateTime windowStart, DateTime windowEnd, CancellationToken ct);
}
