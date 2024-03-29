using CardServicesProcessor.DataAccess.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardServicesProcessor
{
    public class Functions(IConfiguration configuration, IDataLayer dataLayer, ILogger<Functions> logger, IMemoryCache cache)
    {
        [Function("CardServicesReportProcessor")]
        public async Task GenerateCardServicesReport([TimerTrigger("* * * * 1 *", RunOnStartup = true)] TimerInfo myTimer)
        {
            _ = await CaseProcessor.ProcessAllCases(configuration, dataLayer, logger, cache);
        }

        [Function("CheckIssuanceProcessor")]
        public async Task GenerateReimbursementCheckIssuance([TimerTrigger("* * * * 1 *", RunOnStartup = true)] TimerInfo myTimer)
        {
            //_ = await CheckIssuanceProcessor.ProcessCheckIssuance(configuration, dataLayer, logger, cache);
        }
    }
}
