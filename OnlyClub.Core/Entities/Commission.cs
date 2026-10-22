using OnlyClub.Core.Common;
using OnlyClub.Core.Enums;

namespace OnlyClub.Core.Entities;

public class Commission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid PromoterId { get; set; }
    public Promoter Promoter { get; set; } = null!;
    public CommissionStatus Status { get; set; } = CommissionStatus.Pending;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<CommissionLine> Lines { get; set; } = [];

    public void Recalculate(IEnumerable<CommissionLine> lines)
    {
        Lines.Clear();
        Lines.AddRange(lines);
        TotalAmount = Lines.Sum(l => l.Amount);
    }

    public void Approve()
    {
        if (Status != CommissionStatus.Pending)
            throw new DomainException($"Cannot approve commission in status {Status}. Only Pending can be approved.");

        Status = CommissionStatus.Approved;
    }

    public void Pay()
    {
        if (Status != CommissionStatus.Approved)
            throw new DomainException($"Cannot pay commission in status {Status}. Only Approved can be paid.");

        Status = CommissionStatus.Paid;
    }
}