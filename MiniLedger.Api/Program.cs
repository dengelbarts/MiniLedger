using MiniLedger.Api.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Ledger>();
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
            _ => StatusCodes.Status500InternalServerError
        };
        await Results.Problem(detail: ex.Message, statusCode: status).ExecuteAsync(context);
    }
    catch (ArgumentException ex)
    {
        await Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
    }
});

app.MapGet("/accounts/{id:guid}", (Guid id, Ledger ledger) => Results.Ok(ledger.Get(id)));
app.MapPost("/accounts", (OpenAccountRequest request, Ledger ledger) =>
{
    var account = ledger.Open(request.Owner, request.InitialDeposit);
    return Results.Created($"/accounts/{account.Id}", account);
});

app.Run();

record OpenAccountRequest(string Owner, decimal InitialDeposit);