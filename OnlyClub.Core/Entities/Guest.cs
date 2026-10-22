using OnlyClub.Core.Enums;
namespace OnlyClub.Core.Entities;

public class Guest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long TelegramId { get; set; }
    public string Name { get; set; } = "";
    public Role Role { get; set; } = Role.Guest;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
