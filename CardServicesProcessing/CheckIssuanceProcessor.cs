using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Services;
using CardServicesProcessor.Shared;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CardServicesProcessor
{
    public static class CheckIssuanceProcessor
    {
        public static async Task<IActionResult> ProcessCheckIssuance(IConfiguration config, IDataLayer dataLayer, ILogger log)
        {
            try
            {
                await Task.Run(async () =>
                {
                    List<ReportInfo> reportInfo =
                    [
                        new()
                        {
                            SheetName = CheckIssuanceConstants.Nations.SheetName,
                        },
                        new()
                        {
                            SheetName = CheckIssuanceConstants.Elevance.SheetName,
                        }
                    ];

                    await ProcessReports(config, dataLayer, log, reportInfo);

                    log.LogInformation($"Opening the Excel file at {CheckIssuanceConstants.FilePathCurr}...");
                    Stopwatch sw = Stopwatch.StartNew();
                    ExcelService.OpenExcel(CheckIssuanceConstants.FilePathCurr, log);
                    sw.Stop();
                    log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");
                });

                return new OkObjectResult("Reimbursement report processing completed successfully.");
            }
            catch (Exception ex)
            {
                log?.LogInformation($"Failed with an exception with message: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private static async Task ProcessReports(IConfiguration config, IDataLayer dataLayer, ILogger log, List<ReportInfo> reportSettings)
        {
            ExcelService.DeleteWorkbook(CheckIssuanceConstants.FilePathCurr);

            foreach (ReportInfo settings in reportSettings)
            {
                Stopwatch sw = new();

                log.LogInformation($"{settings.SheetName} > Processing data");
                string conn = GetConnectionString(config, $"{settings.SheetName}ProdConn");

                log.LogInformation($"{settings.SheetName} > Getting all approved reimbursements");
                sw.Start();
                if (!CacheManager.Cache.TryGetValue($"{settings.SheetName}CheckIssuance", out CheckIssuance? dataCurr))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@caseTicketStatusId", new int[] { 9,3 });
                    parameters.Add("@approvedStatus", "Approved");
                    dataCurr = await dataLayer.QueryMultipleAsyncCustom<CheckIssuance>(conn, log, parameters);
                    _ = CacheManager.Cache.Set($"{settings.SheetName}CheckIssuance", dataCurr, TimeSpan.FromDays(1));
                }
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"{settings.SheetName} > Adding data to Excel");
                sw.Start();
                ExcelService.AddToExcel<CheckIssuance>(dataCurr);
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");
            }
        }

        private static string GetConnectionString(IConfiguration config, string key)
        {
            string defaultConn = "Data Source=tcp:nbappproddr.database.windows.net;Initial Catalog=NBAPP_PROD;Authentication=Active Directory Interactive;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadOnly;MultiSubnetFailover=False;MultipleActiveResultSets=true";

            return config[key] ?? Environment.GetEnvironmentVariable(key) ?? defaultConn;
        }

        private class ReportInfo
        {
            public required string SheetName { get; set; }
            public readonly Dictionary<int, string> Sheets = CheckIssuanceConstants.SheetIndexToNameMap;
        }
    }
}
