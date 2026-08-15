using Lendora.Infrastructure.Identity;
using Lendora.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lendora.Infrastructure.Extensions;

public static class WebApplicationExtensions
{
    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();

        var loanProductSeeder = scope.ServiceProvider.GetRequiredService<LoanProductSeeder>();
        await loanProductSeeder.SeedAsync();
    }
}
