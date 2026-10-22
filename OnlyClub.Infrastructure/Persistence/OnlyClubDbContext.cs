using Microsoft.EntityFrameworkCore;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Infrastructure;

public class OnlyClubDbContext(DbContextOptions<OnlyClubDbContext> options) : DbContext(options)
{
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Promoter> Promoters => Set<Promoter>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventTable> EventTables => Set<EventTable>();
    public DbSet<TableBooking> Bookings => Set<TableBooking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<GuestListEntry> GuestListEntries => Set<GuestListEntry>();
    public DbSet<PromoterTouch> PromoterTouches => Set<PromoterTouch>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<CommissionLine> CommissionLines => Set<CommissionLine>();
    public DbSet<ScheduledMessage> ScheduledMessages => Set<ScheduledMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Guest>().HasIndex(g => g.TelegramId).IsUnique();
        mb.Entity<Promoter>().HasIndex(p => p.Code).IsUnique();
        mb.Entity<Payment>().HasIndex(p => p.ProviderRef).IsUnique();

        mb.Entity<EventTable>()
            .HasOne(t => t.Event).WithMany(e => e.Tables)
            .HasForeignKey(t => t.EventId).OnDelete(DeleteBehavior.Cascade);

        mb.Entity<TableBooking>()
            .HasOne(b => b.Guest).WithMany().HasForeignKey(b => b.GuestId);
        mb.Entity<TableBooking>()
            .HasOne(b => b.EventTable).WithMany().HasForeignKey(b => b.EventTableId);

        mb.Entity<TableBooking>().HasIndex(b => b.EventTableId).IsUnique()
            .HasFilter($"\"Status\" IN ({(int)BookingStatus.Held},{(int)BookingStatus.Confirmed},{(int)BookingStatus.Seated})");

        mb.Entity<GuestListEntry>().HasIndex(e => e.EventId).HasDatabaseName("IX_GuestList_Event");
        mb.Entity<GuestListEntry>().HasIndex(e => new { e.EventId, e.GuestId })
            .HasFilter($"\"Status\" NOT IN ({(int)GuestListStatus.Cancelled},{(int)GuestListStatus.NoShow})")
            .IsUnique();

        mb.Entity<Commission>().HasIndex(c => new { c.EventId, c.PromoterId }).IsUnique();
        mb.Entity<ScheduledMessage>().HasIndex(m => new { m.Status, m.ScheduledAt });
        mb.Entity<AuditLog>().HasIndex(a => new { a.EntityType, a.EntityId });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder cfg)
    {
        cfg.Properties<decimal>().HavePrecision(18, 2);
    }
}