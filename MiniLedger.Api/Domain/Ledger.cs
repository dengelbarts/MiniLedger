namespace MiniLedger.Api.Domain;

public class Ledger
{
    private readonly Dictionary<Guid, Account> _accounts = new();
    private readonly List<Transaction> _transactions = new();
    private readonly Dictionary<string, Transaction> _byIdempotencyKey = new();

    public Account Open(string owner, decimal initialDeposit = 0)
    {
        if (initialDeposit < 0)
        {
            throw new LedgerException(LedgerError.InvalidAmount, "Initial deposit cannot be negative");
        }
        Account account = new(owner);
        if (initialDeposit > 0)
        {
            account.Credit(initialDeposit);
        }
        _accounts.Add(account.Id, account);
        return account;
    }

    public Account Get(Guid id)
    {
        if (!_accounts.TryGetValue(id, out var account))
        {
            throw new LedgerException(LedgerError.NotFound, $"Account {id} not found");
        }
        return account;
    }
}

