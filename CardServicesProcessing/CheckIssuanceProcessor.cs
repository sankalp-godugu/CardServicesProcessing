using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Services;
using CardServicesProcessor.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
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
                            SheetName = CheckIssuanceConstants.Elevance.SheetName,
                        },
                        new()
                        {
                            SheetName = CheckIssuanceConstants.Nations.SheetName,
                        }
                    ];

                    await ProcessReports(config, dataLayer, log, cache, reportInfo);

                    log.LogDebug("Opening the Excel file...");
                    Stopwatch sw = Stopwatch.StartNew();
                    ExcelService.OpenExcel(CardServicesConstants.FilePathCurr);
                    sw.Stop();
                    log.LogDebug("Elapsed time in seconds", sw.Elapsed.TotalSeconds);
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
            foreach (ReportInfo settings in reportSettings)
            {
                Stopwatch sw = new();

                log.LogDebug($"Processing data for: {settings.SheetName}...");
                string conn = GetConnectionString(config, $"{settings.SheetName}ProdConn");

                log.LogDebug("Getting all cases...");
                sw.Start();

                (IEnumerable<RawData>, IEnumerable<MemberMailingInfo>, IEnumerable<MemberCheckReimbursement>) data = new();
                
                // Check if data exists in cache
                if (!cache.TryGetValue($"{settings.SheetName}CheckIssuance", out IEnumerable<CardServicesResponse> response))
                {
                    // Data not found in cache, fetch from source and store in cache
                    data = await dataLayer.QueryMultipleAsyncCustom(conn);

                    _ = cache.Set($"{settings.SheetName}CheckIssuance", response, TimeSpan.FromDays(1));
                }
                sw.Stop();
                log.LogDebug("Elapsed time in seconds", sw.Elapsed.TotalSeconds);
                ExcelService.AddToExcel(data, CheckIssuanceConstants.FilePathCurr);
                log.LogDebug("Processing missing/invalid data...");
                sw.Restart();
                DataTable tblCurr = DataManipulationService.ValidateCases(response);
                sw.Stop();
                log.LogDebug("Elapsed time in seconds", sw.Elapsed.TotalSeconds);
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
            public readonly Dictionary<int, string> Sheets = CheckIssuanceConstants.Sheets;
        }
    }
}
