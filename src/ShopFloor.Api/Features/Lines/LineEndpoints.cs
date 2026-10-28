using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShopFloor.Api.Data;
using ShopFloor.Domain.Lines;

namespace ShopFloor.Api.Features.Lines;

public static class LineEndpoints
{
    private const string GetLineRoute = "GetLine";

    public static IEndpointRouteBuilder MapLineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lines").WithTags("Production lines");

        group.MapGet("/", GetLines).WithName("GetLines");
        group.MapGet("/{id:guid}", GetLine).WithName(GetLineRoute);
        group.MapPost("/", CreateLine).WithName("CreateLine");

        return app;
    }

    private static async Task<Ok<List<LineResponse>>> GetLines(ShopFloorDbContext db, CancellationToken ct)
    {
        var lines = await db.ProductionLines
            .AsNoTracking()
            .OrderBy(l => l.Code)
            .Select(l => new LineResponse(l.Id, l.Code, l.Name, l.CreatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(lines);
    }

    private static async Task<Results<Ok<LineResponse>, NotFound>> GetLine(Guid id, ShopFloorDbContext db, CancellationToken ct)
    {
        var line = await db.ProductionLines
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new LineResponse(l.Id, l.Code, l.Name, l.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return line is null ? TypedResults.NotFound() : TypedResults.Ok(line);
    }

    private static async Task<Results<CreatedAtRoute<LineResponse>, Conflict<ProblemDetails>>> CreateLine(
        CreateLineRequest request,
        ShopFloorDbContext db,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        // PostgreSQL stores timestamps with microsecond precision, so the response must not carry more
        var now = timeProvider.GetUtcNow();
        now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMicrosecond));

        var line = ProductionLine.Create(request.Code, request.Name, now);

        if (await db.ProductionLines.AnyAsync(l => l.Code == line.Code, ct))
        {
            return CodeAlreadyExists(line.Code);
        }

        db.ProductionLines.Add(line);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another request created the same code between the check above and the insert
            return CodeAlreadyExists(line.Code);
        }

        return TypedResults.CreatedAtRoute(LineResponse.From(line), GetLineRoute, new { id = line.Id });
    }

    private static Conflict<ProblemDetails> CodeAlreadyExists(string code) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Line code already exists",
            Detail = $"A production line with code '{code}' already exists.",
        });
}
