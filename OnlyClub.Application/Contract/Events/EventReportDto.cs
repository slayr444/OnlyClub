namespace OnlyClub.Application.Contract.Events;

public record EventReportDto(
    int TotalBookings,
    Dictionary<string, int> ByStatus,
    decimal DepositsReceived,
    decimal DepositsCredited,
    decimal DepositsKept,
    decimal DepositsRefunded,
    int CheckedInCount);