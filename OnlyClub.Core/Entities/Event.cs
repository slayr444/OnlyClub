namespace OnlyClub.Core.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public TimeSpan HoldUntil { get; set; } = new(0, 30, 0);
    public int GuestListLimit { get; set; }
    public int MinAge { get; set; } = 18;
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public List<EventTable> Tables { get; set; } = [];
    public DateTime HoldUntilDate => StartsAt.Date.AddDays(1).Add(HoldUntil);
}
