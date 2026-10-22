using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;
namespace OnlyClub.Application.Ports;

public interface IGuestRepository
{
    Task<Guest?> GetByTelegramIdAsync(long tg, CancellationToken ct);
    Task<Guest?> GetAsync(Guid id, CancellationToken ct);
    Task<Guest> GetOrCreateAsync(long tg, string name, Role role, CancellationToken ct);
}
