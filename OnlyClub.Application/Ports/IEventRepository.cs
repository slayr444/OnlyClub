using OnlyClub.Core.Entities;
namespace OnlyClub.Application.Ports;

public interface IEventRepository
{
    Task<Event?> GetAsync(Guid id, CancellationToken ct);
    Task<List<Event>> ListAsync(CancellationToken ct);
    Task AddAsync(Event e, CancellationToken ct);
}
