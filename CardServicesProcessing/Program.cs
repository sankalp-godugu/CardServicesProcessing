using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.DataAccess.Services;
using CardServicesProcessor.Shared;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        _ = services.AddMemoryCache();
        IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        CacheManager.Initialize(memoryCache);
        _ = services.AddApplicationInsightsTelemetryWorkerService();
        _ = services.ConfigureFunctionsApplicationInsights();
        _ = services.AddSingleton<IDataLayer, DataLayer>();
    })
    .Build();
host.Run();
