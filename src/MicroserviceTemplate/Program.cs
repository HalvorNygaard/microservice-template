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

builder.AddMicroserviceDefaults();
builder.AddApplicationData();

builder.Services.AddTasks();

WebApplication app = builder.Build();

app.UseMicroserviceDefaults();
app.UseRateLimiter();

await app.ConfigureDevelopmentSetupAsync();

app.MapTasks();

await app.RunAsync();
