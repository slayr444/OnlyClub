using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace OnlyClub.Adapters.Security;

public sealed record TmaUser(
    long Id,
    string? FirstName,
    string? LastName,
    string? Username,
    string? LanguageCode);

public sealed class TmaInitDataValidator
{
    private readonly byte[] _secretKey;
    private readonly TimeSpan _maxAge;
    private readonly TimeProvider _time;

    public TmaInitDataValidator(IConfiguration config, TimeProvider time)
    {
        var token = config["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Telegram:BotToken is not configured.");

        _secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(token));

        var maxAgeHours = int.TryParse(config["Telegram:InitDataMaxAgeHours"], out var h) && h > 0 ? h : 24;
        _maxAge = TimeSpan.FromHours(maxAgeHours);
        _time = time;
    }

    public bool TryValidate(string initData, out TmaUser? user)
    {
        user = null;
        if (string.IsNullOrWhiteSpace(initData)) return false;

        var pairs = new List<KeyValuePair<string, string>>();
        foreach (var part in initData.Split('&'))
        {
            if (string.IsNullOrEmpty(part)) continue;
            var idx = part.IndexOf('=');
            if (idx <= 0) return false;
            var k = Uri.UnescapeDataString(part[..idx]);
            var v = Uri.UnescapeDataString(part[(idx + 1)..]);
            pairs.Add(new(k, v));
        }

        var hashEntry = pairs.FirstOrDefault(p => p.Key == "hash");
        if (string.IsNullOrEmpty(hashEntry.Key)) return false;

        var dataCheckString = string.Join('\n',
            pairs.Where(p => p.Key != "hash")
                 .OrderBy(p => p.Key, StringComparer.Ordinal)
                 .Select(p => $"{p.Key}={p.Value}"));

        var expected = HMACSHA256.HashData(_secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        byte[] provided;
        try { provided = Convert.FromHexString(hashEntry.Value); }
        catch (FormatException) { return false; }

        if (!CryptographicOperations.FixedTimeEquals(expected, provided)) return false;

        var authDateStr = pairs.FirstOrDefault(p => p.Key == "auth_date").Value;
        if (!long.TryParse(authDateStr, out var authDateUnix)) return false;

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix);
        var now = _time.GetUtcNow();
        if (now - authDate > _maxAge) return false;
        if (authDate - now > TimeSpan.FromMinutes(1)) return false;

        var userJson = pairs.FirstOrDefault(p => p.Key == "user").Value;
        if (string.IsNullOrEmpty(userJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(userJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("id", out var idEl) || !idEl.TryGetInt64(out var id)) return false;

            user = new TmaUser(
                id,
                root.TryGetProperty("first_name", out var fn) ? fn.GetString() : null,
                root.TryGetProperty("last_name", out var ln) ? ln.GetString() : null,
                root.TryGetProperty("username", out var un) ? un.GetString() : null,
                root.TryGetProperty("language_code", out var lc) ? lc.GetString() : null);
            return true;
        }
        catch (JsonException) { return false; }
    }
}