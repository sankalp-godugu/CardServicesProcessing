using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReimbursementReporting.DataAccess.Interfaces;
using ReimbursementReporting.Models.Response;
using ReimbursementReporting.Shared;
using System;
using System.Threading.Tasks;

namespace ReimbursementReporting
{
    public static class CaseProcessor
    {
        public static async Task<IActionResult> ProcessCases(IConfiguration config, IDataLayer dataLayer, ILogger log)
        {
            try
            {
                await Task.Run(async () =>
                {
                    string connString = config["ElevanceProdConn"] ?? Environment.GetEnvironmentVariable("ElevanceProdConn");

                    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                    log?.LogInformation("********* Member PD Orders => Genesys Contact List Execution Started **********");

                    // 1. Execute query to get all cases
                    var response = await dataLayer.QueryAsync<CardServicesResponse>(SQLConstants.Query, connString);

                    var temp = "";
                });

                return new OkObjectResult("Task of processing PD Orders in Genesys has been allocated to azure function and see logs for more information about its progress...");
            }
            catch (Exception ex)
            {
                log?.LogError($"Failed with an exception with message: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
