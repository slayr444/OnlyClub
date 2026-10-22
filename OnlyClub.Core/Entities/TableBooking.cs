using OnlyClub.Core.Common;
using OnlyClub.Core.Enums;
namespace OnlyClub.Core.Entities;

public class TableBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventTableId { get; set; }
    public EventTable EventTable { get; set; } = null!;
    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;
    public int PartySize { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Held;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime HeldUntilAt { get; set; } = DateTime.UtcNow.AddMinutes(15);
    public decimal DepositPaid { get; set; }
    public string? QrToken { get; set; }
    public List<Payment> Payments { get; set; } = [];

    public void Confirm(string qr) { if (Status != BookingStatus.Held) throw new DomainException("Only Held can be confirmed"); Status = BookingStatus.Confirmed; QrToken = qr; EventTable.Status = TableStatus.Confirmed; }
    public void Seat() { if (Status != BookingStatus.Confirmed) throw new DomainException("Only Confirmed can be seated"); Status = BookingStatus.Seated; EventTable.Status = TableStatus.Seated; }
    public void Cancel() { if (Status is BookingStatus.Cancelled or BookingStatus.Seated) throw new DomainException("Cannot cancel"); Status = BookingStatus.Cancelled; EventTable.Status = TableStatus.Free; }
    public void MarkNoShow() { if (Status != BookingStatus.Confirmed) throw new DomainException("Only Confirmed can be NoShow"); Status = BookingStatus.NoShow; EventTable.Status = TableStatus.Free; }

    public decimal RefundAmount(DateTime startsAt, DateTime now, decimal fullHours = 48, decimal halfHours = 12)
    {
        var h = (startsAt - now).TotalHours;
        if (h > (double)fullHours) return DepositPaid;
        if (h > (double)halfHours) return DepositPaid * 0.5m;
        return 0m;
    }
}
