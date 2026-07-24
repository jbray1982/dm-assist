using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DmAssist.Api.Tests;

/// <summary>
/// Shared in-process test host for the API, used by every HTTP-level test class.
/// Provides JsonSerializerOptions matching the API's configuration so tests can
/// deserialize responses with camelCase string enums.
/// </summary>
public sealed class DmAssistApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// JsonSerializerOptions configured to match the API's serialization settings,
    /// including camelCase string enum serialization. Use this when calling
    /// ReadFromJsonAsync on API responses.
    /// </summary>
    public static readonly JsonSerializerOptions ApiJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}

/// <summary>Extension methods for test HttpContent deserialization with API JSON options.</summary>
internal static class HttpContentTestExtensions
{
    /// <summary>
    /// Deserialize HTTP response content using the API's JSON options,
    /// supporting camelCase string enums.
    /// </summary>
    public static async Task<T?> ReadFromJsonAsyncWithApiSettings<T>(
        this HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return await content.ReadFromJsonAsync<T>(DmAssistApiFactory.ApiJsonOptions, cancellationToken);
    }
}
