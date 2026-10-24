using Microsoft.EntityFrameworkCore;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(OnlyClubDbContext db)
    {
        if (await db.Events.AnyAsync()) return;

        var manager = await GetOrCreateGuestAsync(db, 100000001, "Manager", Role.Manager);
        var guard = await GetOrCreateGuestAsync(db, 100000002, "Guard", Role.Guard);
        var promoterGuest = await GetOrCreateGuestAsync(db, 100000003, "Promoter", Role.Promoter);
        var guest = await GetOrCreateGuestAsync(db, 100000004, "Guest", Role.Guest);

        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync();

        var promoter = await db.Promoters.FirstOrDefaultAsync(p => p.GuestId == promoterGuest.Id);
        if (promoter == null)
        {
            promoter = new Promoter { GuestId = promoterGuest.Id, Name = "Promoter", Code = "demo" };
            db.Promoters.Add(promoter);
        }

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

    private static async Task<Guest> GetOrCreateGuestAsync(OnlyClubDbContext db, long telegramId, string name, Role role)
    {
        var guest = await db.Guests.FirstOrDefaultAsync(g => g.TelegramId == telegramId);
        if (guest == null)
        {
            guest = new Guest { TelegramId = telegramId, Name = name, Role = role };
            db.Guests.Add(guest);
        }
        return guest;
    }
}