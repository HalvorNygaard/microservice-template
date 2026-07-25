using ModernMicroservice.Common.Http;
using ModernMicroservice.Configurations.Setup;
using ModernMicroservice.Features.Tasks;
using ModernMicroservice.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddApplicationProblemDetails();
builder.Services.AddApplicationRequestTimeouts(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

builder.AddMicroserviceDefaults();
builder.AddApplicationData();

WebApplication app = builder.Build();

app.UseMicroserviceDefaults();

await app.ConfigureDevelopmentSetupAsync();

app.MapTasks();

await app.RunAsync();
