using Microsoft.EntityFrameworkCore;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;

namespace OnlyClub.Infrastructure.Repositories;

public class EventRepository(OnlyClubDbContext db) : IEventRepository
{
    public Task<Event?> GetAsync(Guid id, CancellationToken ct) =>
        db.Events.Include(e => e.Tables).FirstOrDefaultAsync(e => e.Id == id, ct);
    public Task<List<Event>> ListAsync(CancellationToken ct) =>
        db.Events.Include(e => e.Tables).OrderBy(e => e.StartsAt).ToListAsync(ct);
    public async Task AddAsync(Event e, CancellationToken ct) => await db.Events.AddAsync(e, ct);
}
