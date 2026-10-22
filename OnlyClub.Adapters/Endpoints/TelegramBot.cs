using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlyClub.Application.Ports;

namespace OnlyClub.Adapters;

public sealed class TelegramBot : ITelegramBot
{
    public const string HttpClientName = "telegram";
    private const int MaxMessageLength = 4096;

    private readonly string? _token;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<TelegramBot> _log;

    public TelegramBot(IConfiguration config, IHttpClientFactory http, ILogger<TelegramBot> log)
    {
        _http = http;
        _log = log;
        _token = config["Telegram:BotToken"];
    }

    public async Task SendAsync(long telegramId, string text, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrEmpty(_token))
        {
            _log.LogInformation("TG[stub] -> {Tg}: {Text}", telegramId, text);
            return;
        }

        if (text.Length > MaxMessageLength)
        {
            _log.LogWarning("Telegram message truncated for {Tg}: original length {Length}",
                telegramId, text.Length);
            text = text[..(MaxMessageLength - 3)] + "...";
        }

        var client = _http.CreateClient(HttpClientName);
        var url = $"https://api.telegram.org/bot{_token}/sendMessage";

        using var response = await client.PostAsJsonAsync(
            url,
            new { chat_id = telegramId, text, disable_web_page_preview = true },
            ct);

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);
        _log.LogError("Telegram send failed {Status} for {Tg}: {Body}",
            (int)response.StatusCode, telegramId, body);

        throw new TelegramSendException(
            $"Telegram sendMessage failed with status {(int)response.StatusCode}.",
            response.StatusCode);
    }
}