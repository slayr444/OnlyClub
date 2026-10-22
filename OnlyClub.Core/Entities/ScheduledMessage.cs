using OnlyClub.Core.Enums;
namespace OnlyClub.Core.Entities;

public class ScheduledMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ScheduledMessageType Type { get; set; }
    public Guid GuestId { get; set; }
    public Guid? RelatedId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public ScheduledMessageStatus Status { get; set; } = ScheduledMessageStatus.Pending;
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
