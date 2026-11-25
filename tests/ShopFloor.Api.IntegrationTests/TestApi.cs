using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShopFloor.Api.Features.Lines;

namespace ShopFloor.Api.IntegrationTests;

/// <summary>Helpers shared by the endpoint tests.</summary>
internal static class TestApi
{
    /// <summary>Same JSON settings as the API: web defaults plus enums as strings.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Tests share one database, so every test creates its own uniquely named data
    public static string UniqueCode(string prefix = "T") => $"{prefix}-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    public static async Task<LineResponse> CreateLineAsync(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/lines", new CreateLineRequest(UniqueCode("L"), "Test line"), Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LineResponse>(Json, Ct))!;
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;
}
