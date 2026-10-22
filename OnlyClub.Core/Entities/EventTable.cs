using OnlyClub.Core.Enums;
namespace OnlyClub.Core.Entities;

public class EventTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int Number { get; set; }
    public int Capacity { get; set; }
    public decimal MinSpend { get; set; }
    public decimal Deposit { get; set; }
    public TableStatus Status { get; set; } = TableStatus.Free;
}
