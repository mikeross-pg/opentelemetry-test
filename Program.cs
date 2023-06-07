using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {

        IConfiguration configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var otlpConfig = configuration.GetSection("Telemetry:Otlp");
        var telemetryConfig = configuration.GetSection("Telemetry");

        string serviceName = telemetryConfig["serviceName"] ?? "Not Configured";
        string serviceVersion = telemetryConfig["serviceVersion"] ?? "Not Configured";

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder.SetResourceBuilder(ResourceBuilder.CreateEmpty()
                        .AddService(serviceName, serviceVersion: serviceVersion).AddTelemetrySdk())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(c =>
                    {
                        c.RecordException = true;
                    })
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource(serviceName);
                builder.AddConsoleExporter();

                builder.AddOtlpExporter(otlpExporter => { otlpConfig.Bind(otlpExporter); });

            });

        services.AddSingleton<ILoggerProvider, OpenTelemetryLoggerProvider>();
        services.Configure<OpenTelemetryLoggerOptions>((openTelemetryLoggerOptions) =>
            {
                openTelemetryLoggerOptions.SetResourceBuilder(ResourceBuilder.CreateEmpty()
                    .AddService(serviceName, serviceVersion: serviceVersion).AddTelemetrySdk());
                openTelemetryLoggerOptions.IncludeFormattedMessage = true;
                openTelemetryLoggerOptions.ParseStateValues = true;
                openTelemetryLoggerOptions.IncludeScopes = true;
                openTelemetryLoggerOptions.AddConsoleExporter();
              
                openTelemetryLoggerOptions.AddOtlpExporter(otlpExporter => { otlpConfig.Bind(otlpExporter); });
                
            }
        );
    })
    .Build();

host.Run();
