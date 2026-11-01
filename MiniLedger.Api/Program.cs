using MiniLedger.Api.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Ledger>();
builder.Services.AddOpenApi();
var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (LedgerException ex)
    {
        int status = ex.Error switch
        {
            LedgerError.InvalidAmount => StatusCodes.Status400BadRequest,
            LedgerError.SameAccount => StatusCodes.Status400BadRequest,
            LedgerError.NotFound => StatusCodes.Status404NotFound,
            LedgerError.InsufficientFunds => StatusCodes.Status409Conflict,
            LedgerError.IdempotencyConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        await Results.Problem(detail: ex.Message, statusCode: status).ExecuteAsync(context);
    }
    catch (ArgumentException ex)
    {
        await Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Miniledger"));
}

app.MapGet("/accounts/{id:guid}", (Guid id, Ledger ledger) => Results.Ok(ledger.Get(id)));

app.MapPost("/accounts", (OpenAccountRequest request, Ledger ledger) =>
{
    var account = ledger.Open(request.Owner, request.InitialDeposit);
    return Results.Created($"/accounts/{account.Id}", account);
});

app.MapPost("/transfers", (TransferRequest request, Ledger ledger) =>
{
    var transaction = ledger.Transfer(request.FromAccountId, request.ToAccountId, request.Amount, request.IdempotencyKey);
    return Results.Ok(transaction);
});

app.MapGet("/accounts/{id:guid}/transactions", (Guid id, Ledger ledger, int page = 1, int pageSize = 20) =>
{
    if (page <= 0 || pageSize <= 0 || pageSize > 100 || page > int.MaxValue / pageSize)
    {
        return Results.Problem(detail: "Page and pageSize must be positive integers", statusCode: StatusCodes.Status400BadRequest);
    }
    ledger.Get(id);

    var transactions = ledger.GetTransactions(id)
        .OrderByDescending(t => t.At)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToList();

    return Results.Ok(transactions);
});

app.Run();

record OpenAccountRequest(string Owner, decimal InitialDeposit);
record TransferRequest(Guid FromAccountId, Guid ToAccountId, decimal Amount, string IdempotencyKey);