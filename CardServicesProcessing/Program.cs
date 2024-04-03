using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.DataAccess.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        _ = services.AddMemoryCache();
        _ = services.AddApplicationInsightsTelemetryWorkerService();
        _ = services.ConfigureFunctionsApplicationInsights();
        _ = services.AddSingleton<IDataLayer, DataLayer>();
    })
    .Build();
host.Run();
