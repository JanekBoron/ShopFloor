using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopFloor.Domain.Lines;

namespace ShopFloor.Api.Data.Configurations;

internal sealed class ProductionLineConfiguration : IEntityTypeConfiguration<ProductionLine>
{
    public void Configure(EntityTypeBuilder<ProductionLine> builder)
    {
        builder.ToTable("production_lines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Code).HasMaxLength(ProductionLine.CodeMaxLength).IsRequired();
        builder.HasIndex(l => l.Code).IsUnique();

        builder.Property(l => l.Name).HasMaxLength(ProductionLine.NameMaxLength).IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();
    }
}
