using Microsoft.EntityFrameworkCore;
using ShopFloor.Domain.Lines;
using ShopFloor.Domain.Machines;
using ShopFloor.Domain.WorkOrders;

namespace ShopFloor.Api.Data;

public sealed class ShopFloorDbContext(DbContextOptions<ShopFloorDbContext> options) : DbContext(options)
{
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();

    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ShopFloorDbContext).Assembly);
    }
}
