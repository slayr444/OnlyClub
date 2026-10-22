using OnlyClub.Core.Entities;
namespace OnlyClub.Application.Ports;

public interface ICommissionRepository
{
    Task<Commission?> GetAsync(Guid id, CancellationToken ct);
    Task<Commission?> GetForPromoterEventAsync(Guid promoterId, Guid eventId, CancellationToken ct);
    Task<List<Commission>> ForEventAsync(Guid eventId, CancellationToken ct);
    Task AddAsync(Commission c, CancellationToken ct);
}
