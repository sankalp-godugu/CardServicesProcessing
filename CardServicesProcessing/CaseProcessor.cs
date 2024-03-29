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
        public static async Task<IActionResult> ProcessAllCases(IConfiguration config, IDataLayer dataLayer, ILogger log, IMemoryCache cache)
        {
            try
            {
                await Task.Run(async () =>
                {
                    List<ReportInfo> reportSettings =
                    [
                        new()
                        {
                            SheetName = CardServicesConstants.Elevance.SheetName,
                            SheetPrev = CardServicesConstants.Elevance.SheetPrev,
                            SheetRaw = CardServicesConstants.Elevance.SheetRaw,
                            SheetDraft = CardServicesConstants.Elevance.SheetDraft,
                            SheetFinal = CardServicesConstants.Elevance.SheetFinal,
                            SheetDraftIndex = CardServicesConstants.Elevance.SheetDraftIndex
                        },
                        new()
                        {
                            SheetName = CardServicesConstants.Nations.SheetName,
                            SheetPrev = CardServicesConstants.Nations.SheetPrev,
                            SheetRaw = CardServicesConstants.Nations.SheetRaw,
                            SheetDraft = CardServicesConstants.Nations.SheetDraft,
                            SheetFinal = CardServicesConstants.Nations.SheetFinal,
                            SheetDraftIndex = CardServicesConstants.Nations.SheetDraftIndex
                        }
                    ];

                    await ProcessReports(config, dataLayer, log, cache, reportSettings);

                    log.LogInformation("Opening the Excel file...");
                    Stopwatch sw = Stopwatch.StartNew();
                    ExcelService.OpenExcel(CardServicesConstants.FilePathCurr);
                    sw.Stop();
                    log.LogInformation("Elapsed time in seconds", sw.Elapsed.TotalSeconds);
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

                string conn = GetConnectionString(config, $"{settings.SheetName}ProdConn");

                log.LogInformation($"{settings.SheetName} > Querying for cases...");
                sw.Start();
                // Check if data exists in cache
                if (!cache.TryGetValue(settings.SheetName, out IEnumerable<CardServicesResponse> response))
                {
                    // Data not found in cache, fetch from source and store in cache
                    response = await dataLayer.QueryAsyncCustom<CardServicesResponse>(conn);
                    _ = cache.Set(settings.SheetName, response, TimeSpan.FromDays(1));
                }
                sw.Stop();
                log.LogInformation($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogInformation($"{settings.SheetName} > Processing missing/invalid data...");
                sw.Restart();
                DataTable tblCurr = DataProcessingService.ValidateCases(response);
                sw.Stop();
                log.LogInformation($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

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

                log.LogInformation($"{settings.SheetName} > Cross-referencing data with 2023 Manual Reimbursements Report...");
                sw.Restart();
                DataProcessingService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2023SrcFilePath, tblCurr);
                sw.Stop();
                log.LogInformation($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogInformation($"{settings.SheetName} > Cross-referencing data with 2024 Manual Reimbursements Report...");
                sw.Restart();
                //DataManipulationService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2024SrcFilePath, tblCurr);
                sw.Stop();
                log.LogInformation($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogInformation($"{settings.SheetName} > Writing to Excel and applying filters...");
                sw.Restart();
                ExcelService.ApplyFiltersAndSaveReport(tblCurr, CardServicesConstants.FilePathCurr, settings.SheetDraft, settings.SheetDraftIndex);
                sw.Stop();
                log.LogInformation($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");
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
