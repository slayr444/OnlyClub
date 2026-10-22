namespace OnlyClub.Application.Contract.Events;

public record TableDto(
    Guid Id,
    int Number,
    int Capacity,
    decimal MinSpend,
    decimal Deposit,
    string Status);