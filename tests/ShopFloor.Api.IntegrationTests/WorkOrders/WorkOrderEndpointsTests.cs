using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ShopFloor.Api.Features.WorkOrders;
using ShopFloor.Domain.WorkOrders;
using static ShopFloor.Api.IntegrationTests.TestApi;

namespace ShopFloor.Api.IntegrationTests.WorkOrders;

public class WorkOrderEndpointsTests(ShopFloorApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Work_order_goes_from_planned_to_completed()
    {
        var line = await _client.CreateLineAsync();
        var order = await CreateOrderAsync(line.Id);
        Assert.Equal(WorkOrderStatus.Planned, order.Status);

        var started = await ChangeStatusAsync(order.Id, "start");
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal(WorkOrderStatus.InProgress, (await started.ReadAsync<WorkOrderResponse>()).Status);

        var completed = await ChangeStatusAsync(order.Id, "complete");
        var result = await completed.ReadAsync<WorkOrderResponse>();
        Assert.Equal(WorkOrderStatus.Completed, result.Status);
        Assert.NotNull(result.StartedAt);
        Assert.NotNull(result.CompletedAt);

        var fetched = await _client.GetFromJsonAsync<WorkOrderResponse>($"/api/work-orders/{order.Id}", Json, Ct);
        Assert.Equal(result, fetched);
    }

    [Fact]
    public async Task Only_one_work_order_can_be_in_progress_on_a_line()
    {
        var line = await _client.CreateLineAsync();
        var first = await CreateOrderAsync(line.Id);
        var second = await CreateOrderAsync(line.Id);
        await ChangeStatusAsync(first.Id, "start");

        var blocked = await ChangeStatusAsync(second.Id, "start");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("Line already has a work order in progress", (await blocked.ReadAsync<ProblemDetails>()).Title);

        await ChangeStatusAsync(first.Id, "complete");
        var allowed = await ChangeStatusAsync(second.Id, "start");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Orders_on_different_lines_can_run_at_the_same_time()
    {
        var first = await CreateOrderAsync((await _client.CreateLineAsync()).Id);
        var second = await CreateOrderAsync((await _client.CreateLineAsync()).Id);

        await ChangeStatusAsync(first.Id, "start");
        var response = await ChangeStatusAsync(second.Id, "start");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Not_allowed_status_change_returns_conflict()
    {
        var order = await CreateOrderAsync((await _client.CreateLineAsync()).Id);

        var response = await ChangeStatusAsync(order.Id, "complete");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Equal("Status change not allowed", problem.Title);
        Assert.Contains("Planned", problem.Detail);
    }

    [Fact]
    public async Task Cancelled_order_cannot_be_started()
    {
        var order = await CreateOrderAsync((await _client.CreateLineAsync()).Id);
        await ChangeStatusAsync(order.Id, "cancel");

        var response = await ChangeStatusAsync(order.Id, "start");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Work_order_number_must_be_unique()
    {
        var line = await _client.CreateLineAsync();
        var number = UniqueCode("WO");
        await CreateOrderAsync(line.Id, number);

        var response = await _client.PostAsJsonAsync("/api/work-orders", new CreateWorkOrderRequest(number.ToLowerInvariant(), line.Id, "P1", 10), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_line_returns_validation_problem()
    {
        var response = await _client.PostAsJsonAsync("/api/work-orders", new CreateWorkOrderRequest(UniqueCode("WO"), Guid.NewGuid(), "P1", 10), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("LineId", (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
    }

    [Fact]
    public async Task Unknown_work_order_returns_not_found()
    {
        var get = await _client.GetAsync($"/api/work-orders/{Guid.NewGuid()}", Ct);
        var start = await ChangeStatusAsync(Guid.NewGuid(), "start");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, start.StatusCode);
    }

    [Fact]
    public async Task Work_orders_can_be_filtered_by_line_and_status()
    {
        var line = await _client.CreateLineAsync();
        var planned = await CreateOrderAsync(line.Id);
        var running = await CreateOrderAsync(line.Id);
        await ChangeStatusAsync(running.Id, "start");
        await CreateOrderAsync((await _client.CreateLineAsync()).Id);

        var lineOrders = await _client.GetFromJsonAsync<List<WorkOrderResponse>>($"/api/work-orders?lineId={line.Id}", Json, Ct);
        var lineInProgress = await _client.GetFromJsonAsync<List<WorkOrderResponse>>($"/api/work-orders?lineId={line.Id}&status=InProgress", Json, Ct);

        Assert.Equal([running.Id, planned.Id], lineOrders!.Select(o => o.Id));
        Assert.Equal(running.Id, Assert.Single(lineInProgress!).Id);
    }

    private async Task<WorkOrderResponse> CreateOrderAsync(Guid lineId, string? number = null)
    {
        var response = await _client.PostAsJsonAsync("/api/work-orders", new CreateWorkOrderRequest(number ?? UniqueCode("WO"), lineId, "BOTTLE-05L", 1000), Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<WorkOrderResponse>();
    }

    private Task<HttpResponseMessage> ChangeStatusAsync(Guid id, string action) =>
        _client.PostAsync($"/api/work-orders/{id}/{action}", content: null, Ct);
}
