using OnlyClub.Core.Entities;
namespace OnlyClub.Application.Ports;

public interface IAuditRepository { Task AddAsync(AuditLog log, CancellationToken ct); }
