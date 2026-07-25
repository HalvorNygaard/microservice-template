using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace ModernMicroservice.Infrastructure.Data;

internal static class DataExtensions
{
    internal static IHostApplicationBuilder AddApplicationData(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<ApplicationDbContext>(
            "postgresdb",
            settings => settings.DisableRetry = true,
            options =>
            {
                options.EnableDetailedErrors(builder.Environment.IsDevelopment());
                options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
            });

        return builder;
    }

    internal static async Task ConfigureDevAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await app.EnsureDatabaseMigrationsAsync();
    }

    private static async Task EnsureDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
