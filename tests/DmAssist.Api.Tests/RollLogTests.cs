using System.Net;
using System.Net.Http.Json;
using DmAssist.Api;
using DmAssist.Api.Dtos;

namespace DmAssist.Api.Tests;

/// <summary>
/// The roll log's retention & ordering policy, owned by <see cref="InMemoryRollLog"/> and
/// exercised only through the HTTP surface: bounded at <see cref="InMemoryRollLog.Capacity"/>,
/// newest-first, clearable, and left untouched by a failed roll.
/// </summary>
public class RollLogTests : IClassFixture<DmAssistApiFactory>
{
    private readonly HttpClient _client;

    public RollLogTests(DmAssistApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private Task ResetLogAsync() => _client.DeleteAsync("/api/rolls");

    private async Task<RollResultDto> RollAsync(string expression)
    {
        var response = await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto(expression, null));
        return (await response.Content.ReadFromJsonAsyncWithApiSettings<RollResultDto>())!;
    }

    private async Task<List<RollResultDto>> GetLogAsync() =>
        (await (await _client.GetAsync("/api/rolls")).Content.ReadFromJsonAsyncWithApiSettings<List<RollResultDto>>())!;

    [Fact]
    public async Task Get_AfterClear_ReturnsEmptyArray()
    {
        await ResetLogAsync();

        var response = await _client.GetAsync("/api/rolls");
        var log = await response.Content.ReadFromJsonAsyncWithApiSettings<List<RollResultDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(log!);
    }

    [Fact]
    public async Task Get_ReturnsRollsNewestFirst()
    {
        await ResetLogAsync();

        var first = await RollAsync("1d6");
        var second = await RollAsync("1d8");

        var log = await GetLogAsync();

        Assert.Equal(second.Id, log[0].Id);
        Assert.Equal(first.Id, log[1].Id);
    }

    [Fact]
    public async Task Get_LogIsCappedAtCapacity()
    {
        await ResetLogAsync();

        for (var i = 0; i < InMemoryRollLog.Capacity + 10; i++)
        {
            await RollAsync("1d6");
        }

        var log = await GetLogAsync();

        Assert.Equal(InMemoryRollLog.Capacity, log.Count);
    }

    [Fact]
    public async Task Get_WhenOverCapacity_DropsOldestNotNewest()
    {
        await ResetLogAsync();

        RollResultDto? mostRecent = null;
        for (var i = 0; i < InMemoryRollLog.Capacity + 1; i++)
        {
            mostRecent = await RollAsync("1d6");
        }

        var log = await GetLogAsync();

        Assert.Equal(mostRecent!.Id, log[0].Id);
    }

    [Fact]
    public async Task Delete_ClearsTheLog()
    {
        await RollAsync("1d6");

        var deleteResponse = await _client.DeleteAsync("/api/rolls");
        var log = await GetLogAsync();

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Post_InvalidExpression_LeavesLogUnchanged()
    {
        await ResetLogAsync();
        await RollAsync("1d6");

        await _client.PostAsJsonAsync("/api/rolls", new RollRequestDto("0d6", null)); // invalid; must not log

        var log = await GetLogAsync();

        Assert.Single(log);
    }
}
