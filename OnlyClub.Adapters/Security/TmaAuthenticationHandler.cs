using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlyClub.Application.Ports;
using OnlyClub.Core.Enums;

namespace OnlyClub.Adapters.Security;

public sealed class TmaAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Tma";

    private readonly TmaInitDataValidator _validator;
    private readonly IGuestRepository _guests;

    public TmaAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TmaInitDataValidator validator,
        IGuestRepository guests)
        : base(options, logger, encoder) // В .NET 8 этот конструктор обязателен
    {
        _validator = validator;
        _guests = guests;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var values))
            return AuthenticateResult.NoResult();

        var header = values.ToString();
        if (!header.StartsWith("tma ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var initData = header["tma ".Length..].Trim();

        if (!_validator.TryValidate(initData, out var tgUser) || tgUser is null)
            return AuthenticateResult.Fail("Invalid Telegram initData");

        var display = string.Join(' ', new[] { tgUser.FirstName, tgUser.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(display))
            display = tgUser.Username ?? $"Guest {tgUser.Id}";

        var guest = await _guests.GetOrCreateAsync(tgUser.Id, display, Role.Guest, Context.RequestAborted);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, guest.Id.ToString()),
            new(ClaimTypes.Name, guest.Name),
            new(ClaimTypes.Role, guest.Role.ToString()),
            new("telegram_id", guest.TelegramId.ToString()),
        };
        if (!string.IsNullOrEmpty(tgUser.Username))
            claims.Add(new Claim("telegram_username", tgUser.Username));

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}