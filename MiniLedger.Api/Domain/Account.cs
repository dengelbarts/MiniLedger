namespace MiniLedger.Api.Domain;

public class Account
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Owner { get; }
    public string Currency { get; } = "CHF";
    public decimal Balance { get; private set; }

    public Account(string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        Owner = owner;
        Balance = 0;
    }

    internal void Credit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Balance += amount;
    }

    internal void Debit(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (Balance < amount)
        {
            throw new InvalidOperationException("Insufficient funds");
        }
        Balance -= amount;
    }

}