namespace OnlyClub.Application.Ports;
public interface IQrSigner { string Sign(Guid entryId, Guid eventId); bool TryParse(string token, out Guid entryId, out Guid eventId); }