using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ShopFloor.Api.Features.Lines;

namespace ShopFloor.Api.IntegrationTests.Lines;

public class LineEndpointsTests(ShopFloorApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Created_line_can_be_fetched_by_id_and_is_listed()
    {
        var code = UniqueCode();

        var createResponse = await _client.PostAsJsonAsync("/api/lines", new CreateLineRequest(code.ToLowerInvariant(), "Bottling line"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<LineResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(created);
        Assert.Equal(code, created.Code);
        Assert.NotNull(createResponse.Headers.Location);

        var fetched = await _client.GetFromJsonAsync<LineResponse>(createResponse.Headers.Location, TestContext.Current.CancellationToken);
        Assert.Equal(created, fetched);

        var all = await _client.GetFromJsonAsync<List<LineResponse>>("/api/lines", TestContext.Current.CancellationToken);
        Assert.Contains(all!, l => l.Id == created.Id);
    }

    [Fact]
    public async Task Creating_a_line_with_an_existing_code_returns_conflict()
    {
        var code = UniqueCode();
        await _client.PostAsJsonAsync("/api/lines", new CreateLineRequest(code, "First"), TestContext.Current.CancellationToken);

        var response = await _client.PostAsJsonAsync("/api/lines", new CreateLineRequest(code.ToLowerInvariant(), "Second"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.Equal("Line code already exists", problem?.Title);
    }

    [Theory]
    [InlineData("", "Line")]
    [InlineData("L1", "")]
    [InlineData("THIS-CODE-IS-WAY-TOO-LONG", "Line")]
    public async Task Invalid_request_returns_validation_problem(string code, string name)
    {
        var response = await _client.PostAsJsonAsync("/api/lines", new CreateLineRequest(code, name), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotEmpty(problem!.Errors);
    }

    [Fact]
    public async Task Unknown_line_returns_not_found()
    {
        var response = await _client.GetAsync($"/api/lines/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_check_reports_healthy_database()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    // Tests share one database, so every line gets its own code
    private static string UniqueCode() => $"T-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
}
