using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Configurations.Options;
using MicroserviceTemplate.Configurations.Setup;
using MicroserviceTemplate.Features.Tasks;
using MicroserviceTemplate.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddApplicationRateLimiting(builder.Configuration);
builder.Services.AddOptions<CacheOptions>()
    .BindConfiguration(CacheOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<RateLimitingOptions>()
    .BindConfiguration(RateLimitingOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.AddMicroserviceDefaults();

builder.AddApplicationData();

builder.Services.AddTasks();

var app = builder.Build();

await app.ConfigureDevAsync();

app.UseMicroserviceDefaults();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("MicroserviceTemplate");
        options.WithTheme(ScalarTheme.BluePlanet);
    });
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}
else
{
    app.MapGet("/", () => TypedResults.Ok(new { service = app.Environment.ApplicationName }));
}

app.MapTasks();

await app.RunAsync();
