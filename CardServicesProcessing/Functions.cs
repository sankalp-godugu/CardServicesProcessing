using CardServicesProcessor.DataAccess.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardServicesProcessor
{
    public class Functions(IConfiguration configuration, IDataLayer dataLayer, ILogger<Functions> logger)
    {
        [Function("CasesProcessor")]
        public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _ = await CaseProcessor.ProcessCases(configuration, dataLayer, logger);
        }
    }
}
