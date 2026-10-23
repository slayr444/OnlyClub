using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using OnlyClub.Application.Ports;

namespace OnlyClub.Adapters;

public sealed class HmacQrSigner : IQrSigner
{
    private const byte CurrentVersion = 1;
    private const int MaxTokenLength = 512;

    private readonly byte[] _key;
    private readonly TimeProvider _time;
    private readonly TimeSpan _lifetime;

    public HmacQrSigner(IConfiguration config, TimeProvider time)
    {
        var secret = config["Qr:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Qr:Secret is not configured.");

        _key = Encoding.UTF8.GetBytes(secret);
        if (_key.Length < 32)
            throw new InvalidOperationException("Qr:Secret must be at least 32 bytes (256 bits) of entropy.");

        var lifetimeDays = int.TryParse(config["Qr:LifetimeDays"], out var d) && d > 0 ? d : 30;
        _lifetime = TimeSpan.FromDays(lifetimeDays);
        _time = time;
    }

    public string Sign(Guid entryId, Guid eventId)
    {
        var expiresAt = _time.GetUtcNow().Add(_lifetime).ToUnixTimeSeconds();
        var payload = $"{CurrentVersion}:{entryId:N}:{eventId:N}:{expiresAt}";
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signature = HMACSHA256.HashData(_key, payloadBytes);

        return Base64UrlEncode(payloadBytes) + "." + Base64UrlEncode(signature);
    }

    public bool TryParse(string token, out Guid entryId, out Guid eventId)
    {
        entryId = eventId = Guid.Empty;

        if (string.IsNullOrEmpty(token) || token.Length > MaxTokenLength)
            return false;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return false;

        byte[] payloadBytes, providedSignature;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
            providedSignature = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var expectedSignature = HMACSHA256.HashData(_key, payloadBytes);
        if (!CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature))
            return false;

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var fields = payload.Split(':');
        if (fields.Length != 4)
            return false;

        if (!byte.TryParse(fields[0], out var version) || version != CurrentVersion)
            return false;

        if (!Guid.TryParseExact(fields[1], "N", out var parsedEntry))
            return false;

        if (!Guid.TryParseExact(fields[2], "N", out var parsedEvent))
            return false;

        if (!long.TryParse(fields[3], out var expiresAtUnix))
            return false;

        if (_time.GetUtcNow().ToUnixTimeSeconds() > expiresAtUnix)
            return false;

        entryId = parsedEntry;
        eventId = parsedEvent;
        return true;
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
        => Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static byte[] Base64UrlDecode(string value) 
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
            case 0: break;
            default: throw new FormatException("Invalid base64url length.");
        }
        return Convert.FromBase64String(padded);
    }
}
