using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Services;
using CardServicesProcessor.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace CardServicesProcessor
{
    public static class CaseProcessor
    {
        public static async Task<IActionResult> ProcessCases(IConfiguration config, IDataLayer dataLayer, ILogger log)
        {
            try
            {
                await Task.Run(async () =>
                {
                    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                    log.LogInformation("********* Member PD Orders => Genesys Contact List Execution Started **********");

                    string elvConn = config["ElevanceProdConn"] ?? Environment.GetEnvironmentVariable("ElevanceProdConn");
                    string nationsConn = config["NationsProdConn"] ?? Environment.GetEnvironmentVariable("NationsProdConn");

                    // 1. Execute query to get all cases
                    await ProcessReport(elvConn, dataLayer, config, log, FileConstants.CurrReportFilePath, FileConstants.Elevance.SheetPrev, FileConstants.Elevance.SheetDraft, 2);
                    await ProcessReport(nationsConn, dataLayer, config, log, FileConstants.CurrReportFilePath, FileConstants.Nations.SheetPrev, FileConstants.Nations.SheetDraft, 5);

                    log.LogInformation("Opening the Excel file...");
                    ExcelService.OpenExcel(FileConstants.CurrReportFilePath);
                });

                return new OkObjectResult("Reimbursement report processing completed successfully.");
            }
            catch (Exception ex)
            {
                log?.LogError($"Failed with an exception with message: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }

        private static async Task ProcessReport(string conn, IDataLayer dataLayer, IConfiguration config, ILogger log, string reportFilePath, string sheetPrev, string sheetDraft, int pos)
        {
            string manualAdjustmentsSrcFilePath =
                    //@"https://nationshearingllc-my.sharepoint.com/:x:/g/personal/mtapia_nationsbenefits_com/EZay0mCSlPVMrYknu4tMSyQBPJQQq0pjGnOeR45l5R07fw?e=9a6N0x&wdOrigin=TEAMS-MAGLEV.undefined_ns.rwc&wdExp=TEAMS-TREATMENT&wdhostclicktime=1710943414625&web=1";
            @"C:\Users\Sankalp.Godugu\Downloads\Manual Adjustments 2023.xlsx";
            string[] sheets = ["2023 Reimbursements-completed", "2023 Reimbursements-working"];

            bool isElv = sheetPrev.Contains("Elevance");

            log.LogInformation("Executing query...");
            IEnumerable<CardServicesResponse> response = await dataLayer.QueryAsync<CardServicesResponse>(SQLConstants.Query, conn);

            log.LogInformation("Processing missing/invalid data...");
            DataTable tblCurr = DataService.ValidateCases(response);

            log.LogInformation("Reading data from last report sent to Elevance...");
            DataTable? tblPrev = DataService.ReadPrevYTDExcelToDataTable(FileConstants.FilePathPrev, sheetPrev);

            log.LogInformation("Populating missing data from previous 2024 report...");
            DataTable tblFinal = ExcelService.FillMissingDataFromPrevReport(tblCurr, tblPrev);

            log.LogInformation("Populating missing wallets from Manual Reimbursements 2023 Report...");
            ExcelService.FillMissingWallet(manualAdjustmentsSrcFilePath, tblFinal);

            log.LogInformation("Writing to Excel and applying filters...");
            ExcelService.ApplyFiltersAndSaveReport(tblFinal, reportFilePath, sheetDraft, pos);
        }
    }
}
