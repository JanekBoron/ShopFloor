using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFloor.Domain.Lines;
using ShopFloor.Domain.WorkOrders;

namespace ShopFloor.Api.Data.Configurations;

internal sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    private const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("work_orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Number).HasMaxLength(WorkOrder.NumberMaxLength).IsRequired();
        builder.HasIndex(o => o.Number)
            .IsUnique()
            .HasDatabaseName(DatabaseConstraints.WorkOrderNumberUnique);

        builder.HasOne<ProductionLine>()
            .WithMany()
            .HasForeignKey(o => o.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.ProductCode).HasMaxLength(WorkOrder.ProductCodeMaxLength).IsRequired();
        builder.Property(o => o.PlannedQuantity).IsRequired();

        // Stored as text ("InProgress") so the data stays readable in SQL and survives reordering the enum
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength)
            .IsRequired();

        // Supports listing a line's work orders, optionally filtered by status
        builder.HasIndex(o => new { o.LineId, o.Status });

        // Partial unique index: at most one in-progress work order per line, enforced by the database
        builder.HasIndex(o => o.LineId)
            .IsUnique()
            .HasFilter($"\"Status\" = '{nameof(WorkOrderStatus.InProgress)}'")
            .HasDatabaseName(DatabaseConstraints.OneInProgressWorkOrderPerLine);

        builder.Property(o => o.CreatedAt).IsRequired();
    }
}
