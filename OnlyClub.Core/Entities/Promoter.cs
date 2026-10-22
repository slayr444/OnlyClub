namespace OnlyClub.Core.Entities;

public class Promoter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuestId { get; set; }
    public Guest Guest { get; set; } = null!;
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
}
