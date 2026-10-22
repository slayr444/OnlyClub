using OnlyClub.Core.Common;
using OnlyClub.Core.Enums;
namespace OnlyClub.Core.Entities;

public class GuestListEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;
    public string Name { get; set; } = "";
    public Guid? ParentEntryId { get; set; }
    public GuestListEntry? ParentEntry { get; set; }
    public GuestListStatus Status { get; set; } = GuestListStatus.Registered;
    public bool AgeVerifiedByGuard { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public long? GuardTelegramId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Confirm() { if (Status != GuestListStatus.Registered) throw new DomainException("Only Registered can be confirmed"); Status = GuestListStatus.Confirmed; }
    public void CheckIn(long guardTg) { if (Status != GuestListStatus.Confirmed) throw new DomainException("Only Confirmed can check in"); Status = GuestListStatus.CheckedIn; CheckedInAt = DateTime.UtcNow; GuardTelegramId = guardTg; }
    public void Cancel() { if (Status is GuestListStatus.CheckedIn or GuestListStatus.Cancelled) throw new DomainException("Cannot cancel"); Status = GuestListStatus.Cancelled; }
}
