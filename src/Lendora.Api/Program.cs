using Lendora.Api.Extensions;
using Lendora.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

await app.SeedIdentityAsync();
app.UseApiPipeline();

app.Lifetime.ApplicationStarted.Register(() =>
    Log.Information("Lendora API started in {Environment}", app.Environment.EnvironmentName));

app.Lifetime.ApplicationStopping.Register(() =>
    Log.Information("Lendora API is stopping"));

app.Lifetime.ApplicationStopped.Register(() =>
    Log.Information("Lendora API stopped"));

try
{
    Log.Information("Starting Lendora API");
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Lendora API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
