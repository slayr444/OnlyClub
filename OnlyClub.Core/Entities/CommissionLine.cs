namespace OnlyClub.Core.Entities;

public class CommissionLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CommissionId { get; set; }
    public Guid? GuestId { get; set; }
    public string Rule { get; set; } = "";
    public decimal Amount { get; set; }
}
