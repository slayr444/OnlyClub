using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;

namespace OnlyClub.Infrastructure.Repositories;

public class AuditRepository(OnlyClubDbContext db) : IAuditRepository
{
    public async Task AddAsync(AuditLog log, CancellationToken ct) => await db.AuditLogs.AddAsync(log, ct);
}
