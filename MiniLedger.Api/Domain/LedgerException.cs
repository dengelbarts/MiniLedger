namespace MiniLedger.Api.Domain;

public enum LedgerError { InvalidAmount, SameAccount, NotFound, InsufficientFunds, IdempotencyConflict }

public class LedgerException : Exception
{
    public LedgerError Error { get; }

    public LedgerException(LedgerError error, string message) : base(message)
    {
        Error = error;
    }
}