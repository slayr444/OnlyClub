namespace OnlyClub.Application.Ports;

public interface ITelegramBot { Task SendAsync(long telegramId, string text, CancellationToken ct); }
