using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFloor.Domain.Lines;
using ShopFloor.Domain.Machines;

namespace ShopFloor.Api.Data.Configurations;

internal sealed class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ToTable("machines");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        // Foreign key without navigation properties: a machine knows its line only by id
        builder.HasOne<ProductionLine>()
            .WithMany()
            .HasForeignKey(m => m.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.Code).HasMaxLength(Machine.CodeMaxLength).IsRequired();
        builder.HasIndex(m => new { m.LineId, m.Code })
            .IsUnique()
            .HasDatabaseName(DatabaseConstraints.MachineCodeUniquePerLine);

        builder.Property(m => m.Name).HasMaxLength(Machine.NameMaxLength).IsRequired();
        builder.Property(m => m.IdealCycleTime).IsRequired();
        builder.Property(m => m.CreatedAt).IsRequired();
    }
}
