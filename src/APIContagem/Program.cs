using APIContagem;
using APIContagem.Data;
using APIContagem.Tracing;
using Grafana.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ContagemContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("BaseContagem"),
        o => o.UseNodaTime());
});

var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
        serviceVersion: OpenTelemetryExtensions.ServiceVersion);
builder.Services.AddOpenTelemetry()
    .WithTracing((traceBuilder) =>
    {
        traceBuilder
            .AddSource(OpenTelemetryExtensions.ServiceName)
            .SetResourceBuilder(resourceBuilder)
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
            .AddConsoleExporter()
            .UseGrafana();
    });

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeFormattedMessage = true;
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AttachLogsToActivityEvent();
    options.AddConsoleExporter();
    options.UseGrafana();
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ContagemRepository>();
builder.Services.AddSingleton<Contador>();

builder.Services.AddCors();

var app = builder.Build();

app.MapOpenApi();

app.UseAuthorization();

app.MapControllers();

app.Run();