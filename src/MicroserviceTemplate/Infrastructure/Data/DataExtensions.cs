using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace MicroserviceTemplate.Infrastructure.Data;

public static class DataExtensions
{
    public static IHostApplicationBuilder AddApplicationData(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<ApplicationDbContext>("postgresdb", configureDbContextOptions: options =>
        {
            options.EnableDetailedErrors(builder.Environment.IsDevelopment());
            options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());

            options.UseNpgsql(npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
        });

        builder.AddRedisDistributedCache("cache");

        return builder;
    }

    public static async Task ConfigureDevAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await app.EnsureDatabaseMigrationsAsync();
    }

    public static async Task EnsureDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
