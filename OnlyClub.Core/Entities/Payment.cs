using OnlyClub.Core.Enums;
namespace OnlyClub.Core.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public TableBooking Booking { get; set; } = null!;
    public string ProviderRef { get; set; } = "";
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public bool MarkForRefund { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
