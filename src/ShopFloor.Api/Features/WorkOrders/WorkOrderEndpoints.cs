using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopFloor.Api.Data;
using ShopFloor.Domain;
using ShopFloor.Domain.WorkOrders;

namespace ShopFloor.Api.Features.WorkOrders;

public static class WorkOrderEndpoints
{
    private const string GetWorkOrderRoute = "GetWorkOrder";

    public static IEndpointRouteBuilder MapWorkOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-orders").WithTags("Work orders");

        group.MapGet("/", GetWorkOrders).WithName("GetWorkOrders");
        group.MapGet("/{id:guid}", GetWorkOrder).WithName(GetWorkOrderRoute);
        group.MapPost("/", CreateWorkOrder).WithName("CreateWorkOrder");

        // Status changes are actions with their own rules, so each one gets its own endpoint
        group.MapPost("/{id:guid}/start", StartWorkOrder).WithName("StartWorkOrder");
        group.MapPost("/{id:guid}/complete", CompleteWorkOrder).WithName("CompleteWorkOrder");
        group.MapPost("/{id:guid}/cancel", CancelWorkOrder).WithName("CancelWorkOrder");

        return app;
    }

    private static async Task<Ok<List<WorkOrderResponse>>> GetWorkOrders(
        Guid? lineId,
        WorkOrderStatus? status,
        ShopFloorDbContext db,
        CancellationToken ct)
    {
        var query = db.WorkOrders.AsNoTracking();

        if (lineId is not null)
        {
            query = query.Where(o => o.LineId == lineId);
        }

        if (status is not null)
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

        return TypedResults.Ok(orders.Select(WorkOrderResponse.From).ToList());
    }

    private static async Task<Results<Ok<WorkOrderResponse>, NotFound>> GetWorkOrder(Guid id, ShopFloorDbContext db, CancellationToken ct)
    {
        var order = await db.WorkOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? TypedResults.NotFound() : TypedResults.Ok(WorkOrderResponse.From(order));
    }

    private static async Task<Results<CreatedAtRoute<WorkOrderResponse>, ValidationProblem, Conflict<ProblemDetails>>> CreateWorkOrder(
        CreateWorkOrderRequest request,
        ShopFloorDbContext db,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        // The line is part of the request body, so a missing line is invalid input (400), not a missing resource (404)
        if (!await db.ProductionLines.AnyAsync(l => l.Id == request.LineId, ct))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(CreateWorkOrderRequest.LineId)] = [$"Production line '{request.LineId}' does not exist."],
            });
        }

        var order = WorkOrder.Create(
            request.Number,
            request.LineId,
            request.ProductCode,
            request.PlannedQuantity,
            timeProvider.GetUtcNowForDatabase());

        if (await db.WorkOrders.AnyAsync(o => o.Number == order.Number, ct))
        {
            return NumberAlreadyExists(order.Number);
        }

        db.WorkOrders.Add(order);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(DatabaseConstraints.WorkOrderNumberUnique))
        {
            return NumberAlreadyExists(order.Number);
        }

        return TypedResults.CreatedAtRoute(WorkOrderResponse.From(order), GetWorkOrderRoute, new { id = order.Id });
    }

    private static Task<Results<Ok<WorkOrderResponse>, NotFound, Conflict<ProblemDetails>>> StartWorkOrder(
        Guid id, ShopFloorDbContext db, TimeProvider timeProvider, CancellationToken ct) =>
        ChangeStatus(id, db, order => order.Start(timeProvider.GetUtcNowForDatabase()), ct);

    private static Task<Results<Ok<WorkOrderResponse>, NotFound, Conflict<ProblemDetails>>> CompleteWorkOrder(
        Guid id, ShopFloorDbContext db, TimeProvider timeProvider, CancellationToken ct) =>
        ChangeStatus(id, db, order => order.Complete(timeProvider.GetUtcNowForDatabase()), ct);

    private static Task<Results<Ok<WorkOrderResponse>, NotFound, Conflict<ProblemDetails>>> CancelWorkOrder(
        Guid id, ShopFloorDbContext db, TimeProvider timeProvider, CancellationToken ct) =>
        ChangeStatus(id, db, order => order.Cancel(timeProvider.GetUtcNowForDatabase()), ct);

    /// <summary>
    /// Loads the order, applies the transition (the domain decides whether it is allowed)
    /// and saves it, keeping the "one in-progress order per line" rule.
    /// </summary>
    private static async Task<Results<Ok<WorkOrderResponse>, NotFound, Conflict<ProblemDetails>>> ChangeStatus(
        Guid id,
        ShopFloorDbContext db,
        Action<WorkOrder> transition,
        CancellationToken ct)
    {
        var order = await db.WorkOrders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            transition(order);
        }
        catch (DomainException ex)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Status change not allowed",
                Detail = ex.Message,
            });
        }

        if (order.Status == WorkOrderStatus.InProgress
            && await db.WorkOrders.AnyAsync(o => o.LineId == order.LineId && o.Status == WorkOrderStatus.InProgress && o.Id != order.Id, ct))
        {
            return LineBusy(order.LineId);
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(DatabaseConstraints.OneInProgressWorkOrderPerLine))
        {
            // Another request started an order on the same line between the check above and the save
            return LineBusy(order.LineId);
        }

        return TypedResults.Ok(WorkOrderResponse.From(order));
    }

    private static Conflict<ProblemDetails> NumberAlreadyExists(string number) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Work order number already exists",
            Detail = $"A work order with number '{number}' already exists.",
        });

    private static Conflict<ProblemDetails> LineBusy(Guid lineId) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Line already has a work order in progress",
            Detail = $"Production line '{lineId}' already has a work order in progress. Complete or cancel it first.",
        });
}
