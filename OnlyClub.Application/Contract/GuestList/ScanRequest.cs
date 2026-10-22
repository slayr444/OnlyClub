namespace OnlyClub.Application.Contract.GuestList;

public record ScanRequest(
    string Qr,
    bool AgeVerified);