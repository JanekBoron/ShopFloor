using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ShopFloor.Api.Data;
using ShopFloor.Api.Features.Lines;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<ShopFloorDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("ShopFloor")));

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ShopFloorDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ShopFloorDbContext>();
    await db.Database.MigrateAsync();
}

app.MapHealthChecks("/health");
app.MapLineEndpoints();

app.Run();

// Exposes Program to WebApplicationFactory in the integration tests
public partial class Program;
