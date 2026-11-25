using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ShopFloor.Api.Features.Machines;
using static ShopFloor.Api.IntegrationTests.TestApi;

namespace ShopFloor.Api.IntegrationTests.Machines;

public class MachineEndpointsTests(ShopFloorApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Created_machine_is_listed_under_its_line_and_can_be_fetched()
    {
        var line = await _client.CreateLineAsync();

        var response = await _client.PostAsJsonAsync($"/api/lines/{line.Id}/machines", new CreateMachineRequest("filler-1", "Bottle filler", 1.5), Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.ReadAsync<MachineResponse>();
        Assert.Equal("FILLER-1", created.Code);
        Assert.Equal(line.Id, created.LineId);
        Assert.Equal(1.5, created.IdealCycleTimeSeconds);

        var fetched = await _client.GetFromJsonAsync<MachineResponse>(response.Headers.Location, Json, Ct);
        Assert.Equal(created, fetched);

        var lineMachines = await _client.GetFromJsonAsync<List<MachineResponse>>($"/api/lines/{line.Id}/machines", Json, Ct);
        Assert.Equal([created], lineMachines);
    }

    [Fact]
    public async Task Machine_code_must_be_unique_within_a_line_only()
    {
        var line = await _client.CreateLineAsync();
        var otherLine = await _client.CreateLineAsync();
        await _client.PostAsJsonAsync($"/api/lines/{line.Id}/machines", new CreateMachineRequest("M1", "First", 1), Ct);

        var duplicate = await _client.PostAsJsonAsync($"/api/lines/{line.Id}/machines", new CreateMachineRequest("m1", "Second", 1), Ct);
        var sameCodeOtherLine = await _client.PostAsJsonAsync($"/api/lines/{otherLine.Id}/machines", new CreateMachineRequest("M1", "Other", 1), Ct);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("Machine code already exists", (await duplicate.ReadAsync<ProblemDetails>()).Title);
        Assert.Equal(HttpStatusCode.Created, sameCodeOtherLine.StatusCode);
    }

    [Fact]
    public async Task Unknown_line_returns_not_found()
    {
        var unknownLine = Guid.NewGuid();

        var create = await _client.PostAsJsonAsync($"/api/lines/{unknownLine}/machines", new CreateMachineRequest("M1", "Machine", 1), Ct);
        var list = await _client.GetAsync($"/api/lines/{unknownLine}/machines", Ct);

        Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, list.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3601)]
    public async Task Invalid_cycle_time_returns_validation_problem(double seconds)
    {
        var line = await _client.CreateLineAsync();

        var response = await _client.PostAsJsonAsync($"/api/lines/{line.Id}/machines", new CreateMachineRequest("M1", "Machine", seconds), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("IdealCycleTimeSeconds", (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }
}
