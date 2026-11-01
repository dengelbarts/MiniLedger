using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MiniLedger.Tests;

// Integration tests: boot the whole app in memory and talk to it over HTTP.
// All tests in this class share one app (and one singleton Ledger), so each test
// opens its own accounts and every transfer uses a fresh idempotency key.
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private record AccountDto(Guid Id, decimal Balance);

    private async Task<AccountDto> OpenAsync(string owner, decimal deposit)
    {
        var response = await _client.PostAsJsonAsync("/accounts", new { owner, initialDeposit = deposit });
        return (await response.Content.ReadFromJsonAsync<AccountDto>())!;
    }

    private Task<HttpResponseMessage> TransferAsync(Guid from, Guid to, decimal amount) =>
        _client.PostAsJsonAsync("/transfers", new
        {
            fromAccountId = from,
            toAccountId = to,
            amount,
            idempotencyKey = Guid.NewGuid().ToString()
        });

    [Fact]
    public async Task PostAccount_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new { owner = "Alice", initialDeposit = 100 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostAccount_NegativeDeposit_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/accounts", new { owner = "Alice", initialDeposit = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAccount_Unknown_Returns404()
    {
        var response = await _client.GetAsync($"/accounts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Transfer_Valid_Returns200AndMovesMoney()
    {
        var alice = await OpenAsync("Alice", 100);
        var bob = await OpenAsync("Bob", 0);

        var response = await TransferAsync(alice.Id, bob.Id, 30);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var aliceAfter = await _client.GetFromJsonAsync<AccountDto>($"/accounts/{alice.Id}");
        Assert.Equal(70m, aliceAfter!.Balance);
    }

    [Fact]
    public async Task Transfer_Overdraft_Returns409()
    {
        var alice = await OpenAsync("Alice", 10);
        var bob = await OpenAsync("Bob", 0);

        var response = await TransferAsync(alice.Id, bob.Id, 1000);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
