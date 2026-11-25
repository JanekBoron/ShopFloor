using ShopFloor.Domain.WorkOrders;

namespace ShopFloor.Domain.Tests.WorkOrders;

public class WorkOrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 6, 0, 0, TimeSpan.Zero);
    private static readonly Guid LineId = Guid.CreateVersion7();

    [Fact]
    public void New_work_order_is_planned()
    {
        var order = WorkOrder.Create(" wo-1 ", LineId, " bottle-05l ", 1000, Now);

        Assert.Equal("WO-1", order.Number);
        Assert.Equal("BOTTLE-05L", order.ProductCode);
        Assert.Equal(1000, order.PlannedQuantity);
        Assert.Equal(WorkOrderStatus.Planned, order.Status);
        Assert.Null(order.StartedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_rejects_non_positive_quantity(int quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkOrder.Create("WO-1", LineId, "P1", quantity, Now));
    }

    [Fact]
    public void Planned_order_can_be_started_and_completed()
    {
        var order = WorkOrder.Create("WO-1", LineId, "P1", 10, Now);

        order.Start(Now.AddMinutes(5));
        order.Complete(Now.AddHours(2));

        Assert.Equal(WorkOrderStatus.Completed, order.Status);
        Assert.Equal(Now.AddMinutes(5), order.StartedAt);
        Assert.Equal(Now.AddHours(2), order.CompletedAt);
    }

    [Fact]
    public void Order_cannot_be_started_twice()
    {
        var order = WorkOrder.Create("WO-1", LineId, "P1", 10, Now);
        order.Start(Now);

        var ex = Assert.Throws<DomainException>(() => order.Start(Now));

        Assert.Contains("InProgress", ex.Message);
    }

    [Fact]
    public void Planned_order_cannot_be_completed()
    {
        var order = WorkOrder.Create("WO-1", LineId, "P1", 10, Now);

        Assert.Throws<DomainException>(() => order.Complete(Now));
    }

    [Fact]
    public void Planned_and_in_progress_orders_can_be_cancelled()
    {
        var planned = WorkOrder.Create("WO-1", LineId, "P1", 10, Now);
        var inProgress = WorkOrder.Create("WO-2", LineId, "P1", 10, Now);
        inProgress.Start(Now);

        planned.Cancel(Now.AddMinutes(1));
        inProgress.Cancel(Now.AddMinutes(1));

        Assert.Equal(WorkOrderStatus.Cancelled, planned.Status);
        Assert.Equal(WorkOrderStatus.Cancelled, inProgress.Status);
        Assert.Equal(Now.AddMinutes(1), planned.CancelledAt);
    }

    [Fact]
    public void Completed_order_cannot_be_cancelled_or_restarted()
    {
        var order = WorkOrder.Create("WO-1", LineId, "P1", 10, Now);
        order.Start(Now);
        order.Complete(Now);

        Assert.Throws<DomainException>(() => order.Cancel(Now));
        Assert.Throws<DomainException>(() => order.Start(Now));
    }
}
