using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Services;
using CardServicesProcessor.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CardServicesProcessor
{
    public static class CheckIssuanceProcessor
    {
        public static async Task<IActionResult> ProcessCheckIssuance(IConfiguration config, IDataLayer dataLayer, ILogger log, IMemoryCache cache)
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

                    await ProcessReports(config, dataLayer, log, cache, reportInfo);

                    log.LogInformation("Opening the Excel file at {FilePathCurr}...", CheckIssuanceConstants.FilePathCurr);
                    Stopwatch sw = Stopwatch.StartNew();
                    await ExcelService.OpenExcel(CheckIssuanceConstants.FilePathCurr);
                    sw.Stop();
                    ILoggerExtensions.LogMetric(log, "ElapsedTime", sw.Elapsed.TotalSeconds, null);
                });

                return new OkObjectResult("Reimbursement report processing completed successfully.");
            }
            catch (Exception ex)
            {
                log?.LogError($"Failed with an exception with message: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private static async Task ProcessReports(IConfiguration config, IDataLayer dataLayer, ILogger log, IMemoryCache cache, List<ReportInfo> reportSettings)
        {
            ExcelService.DeleteWorkbook(CheckIssuanceConstants.FilePathCurr);

            foreach (ReportInfo settings in reportSettings)
            {
                Stopwatch sw = new();

                log.LogInformation("Processing data for: {SheetName}", settings.SheetName);
                string conn = GetConnectionString(config, $"{settings.SheetName}ProdConn");

                log.LogInformation("Getting all approved reimbursements for: {SheetName}", settings.SheetName);
                sw.Start();
                if (!cache.TryGetValue($"{settings.SheetName}CheckIssuance", out CheckIssuance? dataCurr))
                {
                    dataCurr = await dataLayer.QueryMultipleAsyncCustom<CheckIssuance>(conn, log);
                    _ = cache.Set($"{settings.SheetName}CheckIssuance", dataCurr, TimeSpan.FromDays(1));
                }
                sw.Stop();
                ILoggerExtensions.LogMetric(log, "ElapsedTime", sw.Elapsed.TotalSeconds, null);

                log.LogInformation("Adding data to Excel for: {SheetName}", settings.SheetName);
                sw.Start();
                ExcelService.AddToExcel<CheckIssuance>(dataCurr);
                ILoggerExtensions.LogMetric(log, "ElapsedTime", sw.Elapsed.TotalSeconds, null);
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
