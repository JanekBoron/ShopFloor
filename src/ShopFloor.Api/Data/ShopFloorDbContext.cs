using Microsoft.EntityFrameworkCore;
using ShopFloor.Domain.Lines;

namespace ShopFloor.Api.Data;

public sealed class ShopFloorDbContext(DbContextOptions<ShopFloorDbContext> options) : DbContext(options)
{
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopFloorDbContext).Assembly);
    }
}
