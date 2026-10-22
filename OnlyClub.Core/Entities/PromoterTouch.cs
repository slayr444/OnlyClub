namespace OnlyClub.Core.Entities;

public class PromoterTouch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuestId { get; set; }
    public Guid PromoterId { get; set; }
    public Promoter Promoter { get; set; } = null!;
    public DateTime TouchedAt { get; set; } = DateTime.UtcNow;
}
