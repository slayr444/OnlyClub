using OnlyClub.Application.Ports;
using OnlyClub.Core.Entities;
using OnlyClub.Core.Enums;

namespace OnlyClub.Application.Services;

public class ReminderScheduler(IScheduledMessageRepository msgs, IGuestRepository guests, IBookingRepository bookings, ITelegramBot bot, IUnitOfWork uow)
{
    public async Task<int> ProcessDueAsync(CancellationToken ct)
    {
        var due = await msgs.GetDueAsync(DateTime.UtcNow, 50, ct);
        foreach (var m in due)
        {
            try
            {
                if (m.Type == ScheduledMessageType.NextEventAnnouncement && await msgs.HasRecentMarketingAsync(m.GuestId, DateTime.UtcNow.AddDays(-7), ct))
                { m.Status = ScheduledMessageStatus.Cancelled; continue; }
                await SendAsync(m, ct);
                m.Status = ScheduledMessageStatus.Sent; m.SentAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                m.Attempts++; m.LastError = ex.Message;
                if (m.Attempts >= 3) m.Status = ScheduledMessageStatus.Failed;
                else m.ScheduledAt = DateTime.UtcNow.AddSeconds(30 * Math.Pow(2, m.Attempts));
            }
        }
        if (due.Count > 0) await uow.SaveAsync(ct);
        return due.Count;
    }

    private async Task SendAsync(ScheduledMessage m, CancellationToken ct)
    {
        var guest = await guests.GetAsync(m.GuestId, ct) ?? throw new Exception("Guest missing");
        var text = m.Type switch
        {
            ScheduledMessageType.UnpaidHoldReminder => "⏰ Your table hold expires soon. Please pay to confirm.",
            ScheduledMessageType.BookingConfirmation => "✅ Booking confirmed! Your QR ticket is ready.",
            ScheduledMessageType.DayBeforeReminder => "🎉 Your event is tomorrow! Confirm or cancel your visit.",
            ScheduledMessageType.NextEventAnnouncement => "🍸 Next event is live — see you soon!",
            _ => "Reminder"
        };
        await bot.SendAsync(guest.TelegramId, text, ct);
    }
}