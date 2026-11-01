namespace MiniLedger.Api.Domain;

public class Ledger
{
    private readonly Dictionary<Guid, Account> _accounts = new();
    private readonly List<Transaction> _transactions = new();
    private readonly Dictionary<string, Transaction> _byIdempotencyKey = new();
    private readonly object _gate = new();

    public Account Open(string owner, decimal initialDeposit = 0)
    {
        lock(_gate)
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
    }

    public Account Get(Guid id)
    {
        lock(_gate)
        {
            if (!_accounts.TryGetValue(id, out var account))
            {
                throw new LedgerException(LedgerError.NotFound, $"Account {id} not found");
            }
            return account;
        }
    }

    public Transaction Transfer(Guid from, Guid to, decimal amount, string idempotencyKey)
    {
        lock (_gate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
            if (amount <= 0)
            {
                throw new LedgerException(LedgerError.InvalidAmount, "Transfer amount must be positive");
            }
            if (from == to)
            {
                throw new LedgerException(LedgerError.SameAccount, "Cannot transfer to the same account");
            }
            if (!_accounts.TryGetValue(from, out var fromAccount))
            {
                throw new LedgerException(LedgerError.NotFound, $"Account {from} not found");
            }
            if (!_accounts.TryGetValue(to, out var toAccount))
            {
                throw new LedgerException(LedgerError.NotFound, $"Account {to} not found");
            }
            if (_byIdempotencyKey.TryGetValue(idempotencyKey, out var existingTransaction))
            {
                if (existingTransaction.From != from || existingTransaction.To != to || existingTransaction.Amount != amount)
                {
                    throw new LedgerException(LedgerError.IdempotencyConflict, "Idempotency key conflict: transaction details do not match");
                }
                return existingTransaction;
            }
            if (fromAccount.Balance < amount)
            {
                throw new LedgerException(LedgerError.InsufficientFunds, "Insufficient funds for transfer");
            }

            fromAccount.Debit(amount);
            toAccount.Credit(amount);
            var transaction = new Transaction(Guid.NewGuid(), from, to, amount, DateTimeOffset.UtcNow, idempotencyKey);
            _transactions.Add(transaction);
            _byIdempotencyKey.Add(idempotencyKey, transaction);
            return transaction;
        }
    }

    public IReadOnlyList<Transaction> GetTransactions(Guid accountId)
    {
        lock (_gate)
        {
            var result = _transactions
                .Where(t => t.From == accountId || t.To == accountId)
                .ToList();
            return result;
        }
    }
}

