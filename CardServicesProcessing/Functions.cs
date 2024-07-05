using CardServicesProcessor.DataAccess.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardServicesProcessor
{
    public class Functions(IConfiguration configuration, IDataLayer dataLayer, ILogger<Functions> logger)
    {
        [Function("CardServicesReportProcessor")]
        //public async Task GenerateCardServicesReport([TimerTrigger("* * * * 1 *", RunOnStartup = true)] TimerInfo myTimer)
        public async Task GenerateCardServicesReport([HttpTrigger(AuthorizationLevel.Anonymous, "GET", "POST", Route = null)] HttpRequest req)
        {
            _ = await CaseProcessor.ProcessAllCases(configuration, dataLayer, logger);
        }

        // [Function("CheckIssuanceProcessor")]
        // //public async Task GenerateReimbursementCheckIssuance([TimerTrigger("* * * * 1 *", RunOnStartup = true)] TimerInfo myTimer)
        // public async Task GenerateReimbursementCheckIssuance([HttpTrigger(AuthorizationLevel.Anonymous, "GET", "POST", Route = null)] HttpRequest req)
        // {
        //     _ = await CheckIssuanceProcessor.ProcessCheckIssuance(configuration, dataLayer, logger);
        // }
    }
}
