using System.Net;

namespace OnlyClub.Adapters;

public sealed class TelegramSendException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public TelegramSendException(string message, HttpStatusCode? statusCode = null, Exception? inner = null)
        : base(message, inner) => StatusCode = statusCode;
}
