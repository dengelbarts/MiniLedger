namespace MiniLedger.Api.Domain;

public record Transaction(Guid Id, Guid From, Guid To, decimal Amount, DateTimeOffset At, string IdempotencyKey);