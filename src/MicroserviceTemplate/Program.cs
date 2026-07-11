using System.Text.Json.Serialization;
using MicroserviceTemplate.Common.Http;
using MicroserviceTemplate.Configurations.Setup;
using MicroserviceTemplate.Features.Tasks;
using MicroserviceTemplate.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddApplicationProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

builder.AddMicroserviceDefaults();
builder.AddApplicationData();

WebApplication app = builder.Build();

app.UseMicroserviceDefaults();

await app.ConfigureDevelopmentSetupAsync();

app.MapTasks();

await app.RunAsync();
