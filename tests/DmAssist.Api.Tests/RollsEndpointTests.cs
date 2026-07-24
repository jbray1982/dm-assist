using System.Net;
using System.Net.Http.Json;
using DmAssist.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DmAssist.Api.Tests;

/// <summary>
/// HTTP-level contract tests for <c>POST /api/rolls</c>: wire shape, attribution stamping
/// (design decision D1 — the server, not the caller, sets Source/Visibility), and the error
/// contract (ProblemDetails + <c>code</c>/<c>position</c> extensions).
/// </summary>
public class RollsEndpointTests : IClassFixture<DmAssistApiFactory>
{
    private readonly HttpClient _client;

    public RollsEndpointTests(DmAssistApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string RequireExtension(ProblemDetails problem, string key)
    {
        Assert.True(problem.Extensions.TryGetValue(key, out var value), $"ProblemDetails is missing extension '{key}'.");
        Assert.NotNull(value);
        return value!.ToString()!;
    }

    [Fact]
    public async Task Post_ValidExpression_Returns200WithNormalizedExpression()
    {
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("d20", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsyncWithApiSettings<RollResultDto>();
        Assert.NotNull(body);
        Assert.Equal("1d20", body!.Expression);
    }

    [Fact]
    public async Task Post_ValidExpression_StampsSourceDmAndVisibilityShown()
    {
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("1d6", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsyncWithApiSettings<RollResultDto>();
        Assert.NotEqual(Guid.Empty, body!.Id); // guards against a stub 500 coincidentally deserializing as defaults
        Assert.Equal(RollSourceDto.Dm, body.Source);
        Assert.Equal(RollVisibilityDto.Shown, body.Visibility);
    }

    [Fact]
    public async Task Post_WithReason_EchoesReasonInResult()
    {
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("1d6", "attack roll"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsyncWithApiSettings<RollResultDto>();
        Assert.Equal("attack roll", body!.Reason);
    }

    [Fact]
    public async Task Post_WithoutReason_ResultHasNullReason()
    {
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("1d6", null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsyncWithApiSettings<RollResultDto>();
        Assert.NotEqual(Guid.Empty, body!.Id); // guards against a stub 500 coincidentally deserializing as defaults
        Assert.Null(body.Reason);
    }

    [Theory]
    [InlineData("0d6", "nonPositiveDiceCount")]
    [InlineData("2d0", "nonPositiveSides")]
    [InlineData("2d6kh3", "selectorTooLarge")]
    [InlineData("1/0", "divisionByZero")]
    [InlineData("not a dice expression", "invalidSyntax")]
    public async Task Post_InvalidExpression_Returns400ProblemDetailsWithCode(string expression, string expectedCode)
    {
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto(expression, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(expectedCode, RequireExtension(problem!, "code"));
    }

    [Fact]
    public async Task Post_SyntaxError_ProblemDetailsIncludesPosition()
    {
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("1d", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        RequireExtension(problem!, "position");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"reason":"sneak attack"}""")]
    [InlineData("""{"expression":null}""")]
    [InlineData("""{"expression":""}""")]
    public async Task Post_MissingExpression_Returns400ProblemDetailsNot500(string rawJson)
    {
        using var content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/rolls", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("invalidSyntax", RequireExtension(problem!, "code"));
    }

    [Fact]
    public async Task Post_DiceCountBeyondIntRange_Returns400CapExceededNot500()
    {
        // Overflows int.Parse; must map to the cap error, not an unhandled OverflowException.
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("99999999999d6", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("diceCapExceeded", RequireExtension(problem!, "code"));
    }

    [Fact]
    public async Task Post_CapExceededError_PositionPointsAtOffendingNumber()
    {
        // Contract decision (review of #2): position is set whenever locatable, including cap
        // violations — the UI caret underlines the too-big number.
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("1001d6", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("diceCapExceeded", RequireExtension(problem!, "code"));
        RequireExtension(problem!, "position");
    }
}
