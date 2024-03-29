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
                // Check if data exists in cache
                if (!cache.TryGetValue(settings.SheetName, out IEnumerable<CardServicesResponse> response))
                {
                    // Data not found in cache, fetch from source and store in cache
                    response = await dataLayer.QueryAsyncCustom<CardServicesResponse>(conn);
                    _ = cache.Set(settings.SheetName, response, TimeSpan.FromDays(1));
                }
                sw.Stop();
                log.LogDebug($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogDebug("Processing missing/invalid data...");
                sw.Restart();
                DataTable tblCurr = DataManipulationService.ValidateCases(response);
                sw.Stop();
                log.LogDebug($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                /*log.LogDebug("Reading data from last report sent to Elevance...");
                sw.Restart();
                DataTable? tblPrev = DataManipulationService.ReadPrevYTDExcelToDataTable(CardServicesConstants.FilePathPrev, settings.SheetPrev);
                sw.Stop();
                log.LogDebug($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogDebug("Populating missing data from previous 2024 report...");
                sw.Restart();
                DataManipulationService.FillMissingDataFromPrevReport(tblCurr, tblPrev);
                sw.Stop();
                log.LogDebug($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");*/

                log.LogDebug($"Reading 2023 Manual Reimbursements Report and Filling In Missing Info...");
                sw.Restart();
                DataManipulationService.FillMissingInfoFromManualReimbursementReport(settings.ManualReimbursements2023SrcFilePath, tblCurr);
                sw.Stop();
                log.LogDebug($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogDebug($"Reading 2024 Manual Reimbursements Report and Filling In Missing Info...");
                sw.Restart();
                //DataManipulationService.FillMissingInfoFromManualReimbursementReport(settings.ManualReimbursements2024SrcFilePath, tblCurr);
                sw.Stop();
                log.LogDebug($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");

                log.LogDebug($"Writing to Excel and applying filters...");
                sw.Restart();
                ExcelService.ApplyFiltersAndSaveReport(tblCurr, CardServicesConstants.FilePathCurr, settings.SheetDraft, settings.SheetDraftIndex);
                sw.Stop();
                log.LogDebug($"Elapsed time in seconds: {sw.Elapsed.TotalSeconds}");
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
            public string ManualReimbursements2023SrcFilePath { get; set; } = @"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Manual Adjustments 2023.xlsx";
            public string ManualReimbursements2024SrcFilePath { get; set; } = @"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Manual Reimbursements.xlsx";
        }
    }
}
