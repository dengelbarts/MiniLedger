using MiniLedger.Api.Domain;

namespace MiniLedger.Tests;

public class LedgerTests
{
    [Fact]
    public void Transfer_Valid_MovesMoney()
    {        
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);
        var bob = ledger.Open("Bob");

        ledger.Transfer(alice.Id, bob.Id, 30m, "key-1");

        Assert.Equal(70m, alice.Balance);
        Assert.Equal(30m, bob.Balance);
    }

    [Fact]
    public void Transfer_ZeroAmount_Throws()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);
        var bob = ledger.Open("Bob");

        var ex = Assert.Throws<LedgerException>(() => ledger.Transfer(alice.Id, bob.Id, 0m, "key-1"));

        Assert.Equal(LedgerError.InvalidAmount, ex.Error);
    }

    [Fact]
    public void Transfer_SameAccount_Throws()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);

        var ex = Assert.Throws<LedgerException>(() => ledger.Transfer(alice.Id, alice.Id, 30m, "key-1"));

        Assert.Equal(LedgerError.SameAccount, ex.Error);
    }

    [Fact]
    public void Transfer_UnknownAccount_Throws()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);

        var ex = Assert.Throws<LedgerException>(() => ledger.Transfer(alice.Id, Guid.NewGuid(), 30m, "key-1"));

        Assert.Equal(LedgerError.NotFound, ex.Error);
    }

    [Fact]
    public void Transfer_Overdraft_ThrowsAndChangesNothing()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 50m);
        var bob = ledger.Open("Bob");

        var ex = Assert.Throws<LedgerException>(() => ledger.Transfer(alice.Id, bob.Id, 80m, "key-1"));

        Assert.Equal(LedgerError.InsufficientFunds, ex.Error);
        Assert.Equal(50m, alice.Balance);
        Assert.Equal(0m, bob.Balance);
    }

    [Fact]
    public void Transfer_SameKeyTwice_MovesMoneyOnce()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);
        var bob = ledger.Open("Bob");

        var first = ledger.Transfer(alice.Id, bob.Id, 30m, "key-1");
        var second = ledger.Transfer(alice.Id, bob.Id, 30m, "key-1");

        Assert.Same(first, second);
        Assert.Equal(70m, alice.Balance);
        Assert.Equal(30m, bob.Balance);

    }

    [Fact]
    public void Transfer_DecimalAmounts_StayExact()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);
        var bob = ledger.Open("Bob");

        ledger.Transfer(alice.Id, bob.Id, 0.1m, "key-1");
        ledger.Transfer(alice.Id, bob.Id, 0.2m, "key-2");
        // with double, 0.1 + 0.2 = 0.30000000000000004, but decimal is exact

        Assert.Equal(99.7m, alice.Balance);
        Assert.Equal(0.3m, bob.Balance);
    }

    [Fact]
    public void GetTransactions_ReturnsAccountHistory()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);
        var bob = ledger.Open("Bob");
        var carol = ledger.Open("Carol", 20m);

        ledger.Transfer(alice.Id, bob.Id, 30m, "key-1");
        ledger.Transfer(bob.Id, alice.Id, 10m, "key-2");
        ledger.Transfer(carol.Id, bob.Id, 20m, "key-3");

        var aliceTransactions = ledger.GetTransactions(alice.Id);

        Assert.Equal(2, aliceTransactions.Count);
    }

    [Fact]
    public void Transfer_SameKeyDifferentDetails_Throws()
    {
        var ledger = new Ledger();
        var alice = ledger.Open("Alice", 100m);
        var bob = ledger.Open("Bob");

        ledger.Transfer(alice.Id, bob.Id, 30m, "key-1");

        var ex = Assert.Throws<LedgerException>(() => ledger.Transfer(alice.Id, bob.Id, 40m, "key-1"));

        Assert.Equal(LedgerError.IdempotencyConflict, ex.Error);
    }
}