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
    public static class CaseProcessor
    {
        public static async Task<IActionResult> ProcessAllCases(IConfiguration config, IDataLayer dataLayer, ILogger log)
        {
            try
            {
                await Task.Run(async () =>
                {
                    List<ReportInfo> reportSettings =
                    [
                        new()
                        {
                            SheetName = CardServicesConstants.Nations.SheetName,
                            SheetPrev = CardServicesConstants.Nations.SheetPrev,
                            SheetRaw = CardServicesConstants.Nations.SheetRaw,
                            SheetDraft = CardServicesConstants.Nations.SheetDraft,
                            SheetFinal = CardServicesConstants.Nations.SheetFinal,
                            SheetDraftIndex = CardServicesConstants.Nations.SheetDraftIndex
                        },
                        new()
                        {
                            SheetName = CardServicesConstants.Elevance.SheetName,
                            SheetPrev = CardServicesConstants.Elevance.SheetPrev,
                            SheetRaw = CardServicesConstants.Elevance.SheetRaw,
                            SheetDraft = CardServicesConstants.Elevance.SheetDraft,
                            SheetFinal = CardServicesConstants.Elevance.SheetFinal,
                            SheetDraftIndex = CardServicesConstants.Elevance.SheetDraftIndex
                        }
                    ];

                    await ProcessReports(config, dataLayer, log, reportSettings);

                    log.LogInformation("Opening the Excel file at {FilePathCurr}...", CardServicesConstants.FilePathCurr);
                    Stopwatch sw = Stopwatch.StartNew();
                    await ExcelService.OpenExcel(CardServicesConstants.FilePathCurr);
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

        private static async Task ProcessReports(IConfiguration config, IDataLayer dataLayer, ILogger log, List<ReportInfo> reportSettings)
        {
            foreach (ReportInfo settings in reportSettings)
            {
                Stopwatch sw = new();

                string conn = GetConnectionString(config, $"{settings.SheetName}ProdConn");

                log.LogInformation("{SheetName} > Querying for cases...", settings.SheetName);
                sw.Start();
                // Check if data exists in cache
                if (!CacheManager.Cache.TryGetValue(settings.SheetName, out IEnumerable<CardServicesResponse> response))
                {
                    // Data not found in cache, fetch from source and store in cache
                    response = await dataLayer.QueryAsyncCustom<CardServicesResponse>(conn, log);
                    _ = CacheManager.Cache.Set(settings.SheetName, response, TimeSpan.FromDays(1));
                }
                sw.Stop();
                ILoggerExtensions.LogMetric(log, "ElapsedTime", sw.Elapsed.TotalSeconds, null);

                log.LogInformation("{SheetName} > Processing missing/invalid data...", settings.SheetName);
                sw.Restart();
                DataTable tblCurr = DataProcessingService.ValidateCases(response);
                sw.Stop();
                ILoggerExtensions.LogMetric(log, "ElapsedTime", sw.Elapsed.TotalSeconds, null);

                /*log.LogInformation("Reading data from last report sent to Elevance...");
                sw.Restart();
                DataTable? tblPrev = DataManipulationService.ReadPrevYTDExcelToDataTable(CardServicesConstants.FilePathPrev, settings.SheetPrev);
                sw.Stop();
                log.LogInformation($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogInformation("Populating missing data from previous 2024 report...");
                sw.Restart();
                DataManipulationService.FillMissingDataFromPrevReport(tblCurr, tblPrev);
                sw.Stop();
                log.LogInformation($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");*/

                log.LogInformation("{SheetName} > Cross-referencing data with 2023 Manual Reimbursements Report...", settings.SheetName);
                sw.Restart();
                DataProcessingService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2023SrcFilePath, tblCurr);
                sw.Stop();
                ILoggerExtensions.LogMetric(log, "ElapsedTime", sw.Elapsed.TotalSeconds, null);

                log.LogInformation("{settings.SheetName} > Cross-referencing data with 2024 Manual Reimbursements Report...", settings.SheetName);
                sw.Restart();
                //DataProcessingService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2024SrcFilePath, tblCurr);
                sw.Stop();
                ILoggerExtensions.LogMetric(log, "ElapsedTime", sw.Elapsed.TotalSeconds, null);

                log.LogInformation("{SheetName} > Writing to Excel and applying filters...", settings.SheetName);
                sw.Restart();
                ExcelService.ApplyFiltersAndSaveReport(tblCurr, CardServicesConstants.FilePathCurr, settings.SheetDraft, settings.SheetDraftIndex);
                sw.Stop();
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
            public required string SheetPrev { get; set; }
            public required string SheetRaw { get; set; }
            public required string SheetDraft { get; set; }
            public required string SheetFinal { get; set; }
            public int SheetDraftIndex { get; set; }
        }
    }
}
