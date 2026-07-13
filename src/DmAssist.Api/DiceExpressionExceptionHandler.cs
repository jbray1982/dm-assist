using System.Text.Json;
using DmAssist.Dice;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DmAssist.Api;

/// <summary>
/// Maps <see cref="DiceExpressionException"/> to a <c>400 application/problem+json</c> response
/// carrying <c>code</c> and <c>position</c> extensions, so a caller gets a machine-readable error
/// without parsing message text. Every other exception type is left for the default handler
/// (returns false), matching ASP.NET Core's <see cref="IExceptionHandler"/> chain-of-responsibility
/// contract.
/// </summary>
internal sealed class DiceExpressionExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DiceExpressionException diceEx)
            return ValueTask.FromResult(false);

        var codeString = diceEx.Code switch
        {
            DiceErrorCode.InvalidSyntax => "invalidSyntax",
            DiceErrorCode.NonPositiveDiceCount => "nonPositiveDiceCount",
            DiceErrorCode.NonPositiveSides => "nonPositiveSides",
            DiceErrorCode.SelectorTooLarge => "selectorTooLarge",
            DiceErrorCode.DiceCapExceeded => "diceCapExceeded",
            DiceErrorCode.SidesCapExceeded => "sidesCapExceeded",
            DiceErrorCode.DivisionByZero => "divisionByZero",
            _ => throw new InvalidOperationException($"Unknown DiceErrorCode: {diceEx.Code}")
        };

        var problemDetails = new ProblemDetails
        {
            Type = "https://example.com/dice-expression-error",
            Title = "Invalid dice expression",
            Status = StatusCodes.Status400BadRequest,
            Detail = diceEx.Message
        };

        problemDetails.Extensions.Add("code", codeString);
        if (diceEx.Position.HasValue)
            problemDetails.Extensions.Add("position", diceEx.Position.Value);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/problem+json";

        var json = JsonSerializer.Serialize(problemDetails);
        return new ValueTask<bool>(
            httpContext.Response.WriteAsync(json, cancellationToken)
                .ContinueWith(_ => true)
        );
    }
}
