using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopFloor.Api.Data;
using ShopFloor.Domain.Machines;

namespace ShopFloor.Api.Features.Machines;

public static class MachineEndpoints
{
    private const string GetMachineRoute = "GetMachine";

    public static IEndpointRouteBuilder MapMachineEndpoints(this IEndpointRouteBuilder app)
    {
        // Machines are created and listed in the context of their line...
        var lineMachines = app.MapGroup("/api/lines/{lineId:guid}/machines").WithTags("Machines");
        lineMachines.MapGet("/", GetLineMachines).WithName("GetLineMachines");
        lineMachines.MapPost("/", CreateMachine).WithName("CreateMachine");

        // ...but a single machine has its own address, so its id is enough to find it
        app.MapGet("/api/machines/{id:guid}", GetMachine).WithTags("Machines").WithName(GetMachineRoute);

        return app;
    }

    private static async Task<Results<Ok<List<MachineResponse>>, NotFound>> GetLineMachines(
        Guid lineId,
        ShopFloorDbContext db,
        CancellationToken ct)
    {
        if (!await db.ProductionLines.AnyAsync(l => l.Id == lineId, ct))
        {
            return TypedResults.NotFound();
        }

        var machines = await db.Machines
            .AsNoTracking()
            .Where(m => m.LineId == lineId)
            .OrderBy(m => m.Code)
            .ToListAsync(ct);

        return TypedResults.Ok(machines.Select(MachineResponse.From).ToList());
    }

    private static async Task<Results<Ok<MachineResponse>, NotFound>> GetMachine(Guid id, ShopFloorDbContext db, CancellationToken ct)
    {
        var machine = await db.Machines.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);

        return machine is null ? TypedResults.NotFound() : TypedResults.Ok(MachineResponse.From(machine));
    }

    private static async Task<Results<CreatedAtRoute<MachineResponse>, NotFound, Conflict<ProblemDetails>>> CreateMachine(
        Guid lineId,
        CreateMachineRequest request,
        ShopFloorDbContext db,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        if (!await db.ProductionLines.AnyAsync(l => l.Id == lineId, ct))
        {
            return TypedResults.NotFound();
        }

        var machine = Machine.Create(
            lineId,
            request.Code,
            request.Name,
            TimeSpan.FromSeconds(request.IdealCycleTimeSeconds),
            timeProvider.GetUtcNowForDatabase());

        if (await db.Machines.AnyAsync(m => m.LineId == lineId && m.Code == machine.Code, ct))
        {
            return CodeAlreadyExists(machine.Code);
        }

        db.Machines.Add(machine);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation(DatabaseConstraints.MachineCodeUniquePerLine))
        {
            return CodeAlreadyExists(machine.Code);
        }

        return TypedResults.CreatedAtRoute(MachineResponse.From(machine), GetMachineRoute, new { id = machine.Id });
    }

    private static Conflict<ProblemDetails> CodeAlreadyExists(string code) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Machine code already exists",
            Detail = $"The line already has a machine with code '{code}'.",
        });
}
