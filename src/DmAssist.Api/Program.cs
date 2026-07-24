using System.Text.Json;
using System.Text.Json.Serialization;
using DmAssist.Api;
using DmAssist.Api.Dtos;
using DmAssist.Dice;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRandomSource, SystemRandomSource>();
builder.Services.AddSingleton<DiceRoller>();
builder.Services.AddSingleton<IRollLog, InMemoryRollLog>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DiceExpressionExceptionHandler>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

app.UseExceptionHandler();

// POST /api/rolls — roll and log. The server stamps Source=Dm/Visibility=Shown; the client
// supplies only expression and (optional) reason. See design decision D1.
app.MapPost("/api/rolls", (RollRequestDto request, DiceRoller roller, IRollLog log) =>
{
    var rollRequest = new RollRequest(
        request.Expression,
        RollSource.Dm,
        request.Reason,
        RollVisibility.Shown
    );

    var result = roller.Roll(rollRequest);
    log.Add(result);
    return Results.Ok(result.ToDto());
});

// GET /api/rolls — the log, newest first, at most InMemoryRollLog.Capacity entries.
app.MapGet("/api/rolls", (IRollLog log) =>
{
    var rolls = log.Snapshot();
    var dtos = rolls.Select(r => r.ToDto()).ToList();
    return Results.Ok(dtos);
});

// DELETE /api/rolls — clears the log. No confirmation, per spec.
app.MapDelete("/api/rolls", (IRollLog log) =>
{
    log.Clear();
    return Results.NoContent();
});

app.Run();

// Exposes the implicit Program class to DmAssist.Api.Tests' WebApplicationFactory<Program>.
public partial class Program { }
