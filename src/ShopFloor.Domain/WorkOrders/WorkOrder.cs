namespace ShopFloor.Domain.WorkOrders;

/// <summary>
/// An order to produce a quantity of a product on a production line.
/// Allowed transitions: Planned → InProgress → Completed, and Planned or InProgress → Cancelled.
/// </summary>
public sealed class WorkOrder
{
    public const int NumberMaxLength = 30;
    public const int ProductCodeMaxLength = 50;

    // Used by EF Core
    private WorkOrder()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Business identifier from the planning system, unique, stored upper case (for example "WO-2026-0042").</summary>
    public string Number { get; private set; } = null!;

    public Guid LineId { get; private set; }

    public string ProductCode { get; private set; } = null!;

    public int PlannedQuantity { get; private set; }

    public WorkOrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public static WorkOrder Create(string number, Guid lineId, string productCode, int plannedQuantity, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(productCode);

        if (lineId == Guid.Empty)
        {
            throw new ArgumentException("Line id is required.", nameof(lineId));
        }

        var normalizedNumber = number.Trim().ToUpperInvariant();
        var normalizedProductCode = productCode.Trim().ToUpperInvariant();

        if (normalizedNumber.Length > NumberMaxLength)
        {
            throw new ArgumentException($"Number cannot be longer than {NumberMaxLength} characters.", nameof(number));
        }

        if (normalizedProductCode.Length > ProductCodeMaxLength)
        {
            throw new ArgumentException($"Product code cannot be longer than {ProductCodeMaxLength} characters.", nameof(productCode));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(plannedQuantity);

        return new WorkOrder
        {
            Id = Guid.CreateVersion7(createdAt),
            Number = normalizedNumber,
            LineId = lineId,
            ProductCode = normalizedProductCode,
            PlannedQuantity = plannedQuantity,
            Status = WorkOrderStatus.Planned,
            CreatedAt = createdAt,
        };
    }

    public void Start(DateTimeOffset at)
    {
        EnsureStatus(WorkOrderStatus.Planned, "start");
        Status = WorkOrderStatus.InProgress;
        StartedAt = at;
    }

    public void Complete(DateTimeOffset at)
    {
        EnsureStatus(WorkOrderStatus.InProgress, "complete");
        Status = WorkOrderStatus.Completed;
        CompletedAt = at;
    }

    public void Cancel(DateTimeOffset at)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
        {
            throw new DomainException($"Cannot cancel work order {Number} because it is {Status}.");
        }

        Status = WorkOrderStatus.Cancelled;
        CancelledAt = at;
    }

    private void EnsureStatus(WorkOrderStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new DomainException($"Cannot {action} work order {Number} because it is {Status}; it must be {expected}.");
        }
    }
}
