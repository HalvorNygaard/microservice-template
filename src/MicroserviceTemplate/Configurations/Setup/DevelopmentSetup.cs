using ModernMicroservice.Infrastructure.Data;
using Scalar.AspNetCore;

namespace ModernMicroservice.Configurations.Setup;

internal static class DevelopmentSetup
{
    internal static async Task<WebApplication> ConfigureDevelopmentSetupAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        await app.ConfigureDevAsync();

        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }
}
