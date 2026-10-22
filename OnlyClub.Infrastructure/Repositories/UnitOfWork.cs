using OnlyClub.Application.Ports;

namespace OnlyClub.Infrastructure.Repositories;

public class UnitOfWork(OnlyClubDbContext db) : IUnitOfWork
{
    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public Task BeginTransactionAsync(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public async Task CommitAsync(CancellationToken ct) { await db.Database.CommitTransactionAsync(ct); }
    public async Task RollbackAsync(CancellationToken ct) { if (db.Database.CurrentTransaction is not null) await db.Database.RollbackTransactionAsync(ct); }
}
