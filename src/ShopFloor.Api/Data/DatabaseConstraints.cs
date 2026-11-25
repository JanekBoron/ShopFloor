using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ShopFloor.Api.Data;

/// <summary>
/// Names of unique indexes the endpoints react to. Naming them explicitly lets an endpoint
/// tell which rule a unique violation broke.
/// </summary>
public static class DatabaseConstraints
{
    public const string MachineCodeUniquePerLine = "ix_machines_line_id_code";
    public const string WorkOrderNumberUnique = "ix_work_orders_number";
    public const string OneInProgressWorkOrderPerLine = "ix_work_orders_one_in_progress_per_line";

    /// <summary>True when saving failed because of a unique index, optionally a specific one.</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception, string? constraintName = null) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && (constraintName is null || postgres.ConstraintName == constraintName);
}
