using MicroserviceTemplate.Infrastructure.Data;
using Scalar.AspNetCore;

namespace MicroserviceTemplate.Configurations.Setup;

public static class DevelopmentSetup
{
    public static async Task<WebApplication> ConfigureDevelopmentSetupAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            app.MapGet("/", () => TypedResults.Ok(new { service = app.Environment.ApplicationName }));
            return app;
        }

        await app.ConfigureDevAsync();

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("MicroserviceTemplate");
            options.WithTheme(ScalarTheme.BluePlanet);
        });
        app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

        return app;
    }
}
