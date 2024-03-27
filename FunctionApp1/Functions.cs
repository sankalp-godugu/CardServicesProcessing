using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReimbursementReporting.DataAccess.Interfaces;
using ReimbursementReporting.Services;

namespace FunctionApp1
{
    public class Functions(IConfiguration configuration, IDataLayer dataLayer, ILogger<Functions> logger)
    {
        [Function("CasesProcessor")]
        public async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _ = await ExcelService.ProcessCardServicesData(configuration, dataLayer, logger);
        }
    }
}
