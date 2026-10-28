using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(ShopFloor.Api.IntegrationTests.ShopFloorApiFactory))]

namespace ShopFloor.Api.IntegrationTests;

/// <summary>
/// Starts the API against a real PostgreSQL instance running in a Docker container.
/// One container is shared by all tests in the assembly.
/// </summary>
public sealed class ShopFloorApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ShopFloor", _postgres.GetConnectionString());
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }
}
