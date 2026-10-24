using Microsoft.EntityFrameworkCore;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(OnlyClubDbContext db)
    {
        if (await db.Events.AnyAsync()) return;

        var manager = new Guest { TelegramId = 100000001, Name = "Manager", Role = Role.Manager };
        var guard = new Guest { TelegramId = 100000002, Name = "Guard", Role = Role.Guard };
        var promoterGuest = new Guest { TelegramId = 100000003, Name = "Promoter", Role = Role.Promoter };
        var guest = new Guest { TelegramId = 100000004, Name = "Guest", Role = Role.Guest };
        db.Guests.AddRange(manager, guard, promoterGuest, guest);

        var promoter = new Promoter { GuestId = promoterGuest.Id, Name = "Promoter", Code = "demo" };
        db.Promoters.Add(promoter);

        var secretRoom = new Event
        {
            Title = "Secret Room",
            StartsAt = DateTime.UtcNow.AddDays(3).Date.AddHours(23),
            GuestListLimit = 80,
            MinAge = 21
        };
        for (int i = 1; i <= 4; i++)
        {
            secretRoom.Tables.Add(new EventTable
            {
                Number = i,
                Capacity = 4 + i,
                MinSpend = 100000,   
                Deposit = 30000            
            });
        }

        var pacha = new Event
        {
            Title = "Pacha Ibiza",
            StartsAt = DateTime.UtcNow.AddDays(5).Date.AddHours(22),
            GuestListLimit = 300,
            MinAge = 21
        };
        for (int i = 1; i <= 6; i++)
        {
            pacha.Tables.Add(new EventTable
            {
                Number = i,
                Capacity = 4 + (i % 3) * 2,
                MinSpend = 250000,                
                Deposit = 120000
            });
        }

        var jimmyz = new Event
        {
            Title = "Jimmy'z Monte-Carlo",
            StartsAt = DateTime.UtcNow.AddDays(7).Date.AddHours(23),
            GuestListLimit = 150,
            MinAge = 21
        };
        for (int i = 1; i <= 5; i++)
        {
            jimmyz.Tables.Add(new EventTable
            {
                Number = i,
                Capacity = 6,
                MinSpend = 300000,      
                Deposit = 150000
            });
        }

        var neonNights = new Event
        {
            Title = "Neon Nights",
            StartsAt = DateTime.UtcNow.AddDays(2).Date.AddHours(22),
            GuestListLimit = 200,
            MinAge = 18
        };
        for (int i = 1; i <= 8; i++)
        {
            neonNights.Tables.Add(new EventTable
            {
                Number = i,
                Capacity = 2 + i % 3 * 2,
                MinSpend = 5000 + i * 1000,
                Deposit = 2000 + i * 500
            });
        }

        db.Events.AddRange(secretRoom, pacha, jimmyz, neonNights);
        await db.SaveChangesAsync();
    }
}