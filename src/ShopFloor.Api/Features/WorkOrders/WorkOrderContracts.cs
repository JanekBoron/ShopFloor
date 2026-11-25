using System.ComponentModel.DataAnnotations;
using ShopFloor.Domain.WorkOrders;

namespace ShopFloor.Api.Features.WorkOrders;

public sealed record CreateWorkOrderRequest(
    [Required, StringLength(WorkOrder.NumberMaxLength)] string Number,
    Guid LineId,
    [Required, StringLength(WorkOrder.ProductCodeMaxLength)] string ProductCode,
    [Range(1, int.MaxValue)] int PlannedQuantity);

public sealed record WorkOrderResponse(
    Guid Id,
    string Number,
    Guid LineId,
    string ProductCode,
    int PlannedQuantity,
    WorkOrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt)
{
    public static WorkOrderResponse From(WorkOrder order) => new(
        order.Id,
        order.Number,
        order.LineId,
        order.ProductCode,
        order.PlannedQuantity,
        order.Status,
        order.CreatedAt,
        order.StartedAt,
        order.CompletedAt,
        order.CancelledAt);
}
