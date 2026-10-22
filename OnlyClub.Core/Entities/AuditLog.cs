using OnlyClub.Core.Enums;
namespace OnlyClub.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorId { get; set; }
    public string ActorName { get; set; } = "";
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}