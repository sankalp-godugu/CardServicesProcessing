using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.DataAccess.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        _ = services.AddMemoryCache();
        _ = services.AddApplicationInsightsTelemetryWorkerService();
        _ = services.ConfigureFunctionsApplicationInsights();
        _ = services.AddSingleton<IDataLayer, DataLayer>();
        /*_ = services.AddSingleton<IDataLayer>((s) =>
        {
            var log = s.GetRequiredService<ILogger<DataLayer>>();
            return new DataLayer(log);
        });*/
    })
    /*.ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        //logging.AddConsoleFormatter(options => options.IncludeScopes = true);
        logging.SetMinimumLevel(LogLevel.Debug);
    })*/
    .Build();

host.Run();
