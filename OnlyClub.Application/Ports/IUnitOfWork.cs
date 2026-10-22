namespace OnlyClub.Application.Ports;

public interface IUnitOfWork { Task SaveAsync(CancellationToken ct); Task BeginTransactionAsync(CancellationToken ct); Task CommitAsync(CancellationToken ct); Task RollbackAsync(CancellationToken ct); }
