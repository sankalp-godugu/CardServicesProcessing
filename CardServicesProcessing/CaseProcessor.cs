using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Services;
using CardServicesProcessor.Shared;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Net.Http.Headers;

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
                    List<Report> reportInfo =
                    [
                        new()
                        {
                            SheetName = CardServicesConstants.Elevance.SheetName,
                            SheetPrev = CardServicesConstants.Elevance.SheetPrev,
                            SheetRaw = CardServicesConstants.Elevance.SheetRaw,
                            SheetDraft = CardServicesConstants.Elevance.SheetDraft,
                            SheetFinal = CardServicesConstants.Elevance.SheetFinal,
                            SheetIndex = CardServicesConstants.Elevance.SheetFinalIndex
                        },
                        new()
                        {
                            SheetName = CardServicesConstants.Nations.SheetName,
                            SheetPrev = CardServicesConstants.Nations.SheetPrev,
                            SheetRaw = CardServicesConstants.Nations.SheetRaw,
                            SheetDraft = CardServicesConstants.Nations.SheetDraft,
                            SheetFinal = CardServicesConstants.Nations.SheetFinal,
                            SheetIndex = CardServicesConstants.Nations.SheetFinalIndex
                        }
                    ];

                     await ProcessReports(config, dataLayer, log, reportInfo);

                    log.LogInformation($"Opening the Excel file at {CardServicesConstants.FilePathCurr}...");
                    Stopwatch sw = Stopwatch.StartNew();
                    ExcelService.OpenExcel(CardServicesConstants.FilePathCurr, log);
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

        private static async Task ProcessReports(IConfiguration config, IDataLayer dataLayer, ILogger log, List<Report> reports)
        {
            foreach (Report settings in reports)
            {
                Stopwatch sw = new();
                string conn = GetConnectionString(config, $"{settings.SheetName}ProdConn");

                log.LogInformation($"{settings.SheetName} > Querying for cases...");
                sw.Start();
                // Check if data exists in cache
                if (!CacheManager.Cache.TryGetValue(settings.SheetName, out IEnumerable<CardServicesResponse> response))
                {
                    // Data not found in cache, fetch from source and store in cache
                    var parameters = new DynamicParameters();
                    parameters.Add("@caseCategoryId", 1);
                    parameters.Add("@isActive", 1);
                    parameters.Add("@addressTypeCode", "PERM");
                    parameters.Add("@caseTopicId", 24);
                    parameters.Add("@year", 2024);
                    parameters.Add("@year2", 2023);
                    parameters.Add("@closedYear", 2024);
                    parameters.Add("@carrierName", "Select Health");
                    parameters.Add("@isProcessEligible", 1);
                    response = await dataLayer.QueryAsyncCustom<CardServicesResponse>(conn, log, parameters);
                    _ = CacheManager.Cache.Set(settings.SheetName, response, TimeSpan.FromDays(1));
                }
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"{settings.SheetName} > Processing missing/invalid data...");
                sw.Restart();
                DataTable tblCurr = DataProcessingService.ValidateCases(response);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                //log.LogInformation($"{settings.SheetName} > Cross-referencing data with 2023 Manual Reimbursements Report...");
                //sw.Restart();
                //DataProcessingService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2023SrcFilePath, tblCurr);
                //sw.Stop();
                //log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                //log.LogInformation($"{settings.SheetName} > Cross-referencing data with 2024 Manual Reimbursements Report...");
                //sw.Restart();
                //DataProcessingService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2024SrcFilePath, tblCurr);
                //sw.Stop();
                //log.LogInformation($"ElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"{settings.SheetName} > Getting Reimbursement Product Names by Case Number...");
                sw.Restart();
                IEnumerable<ReimbursementItem> reimbursementItems = await dataLayer.QueryAsync<ReimbursementItem>(conn, SqlConstantsReimbursementItems.GetProductNames, log);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"{settings.SheetName} > Populating Missing Wallet Names by Checking Reimbursed Products...");
                sw.Restart();
                DataProcessingService.FillInMissingWallets(tblCurr, reimbursementItems);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"{settings.SheetName} > Removing Duplicates...");
                sw.Restart();
                DataProcessingService.RemoveDuplicates(tblCurr, ColumnNames.CaseTicketNumber);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"{settings.SheetName} > Writing to Excel and applying filters...");
                sw.Restart();
                ExcelService.ApplyFiltersAndSaveReport(tblCurr, CardServicesConstants.FilePathCurr, settings.SheetFinal, settings.SheetIndex);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");
            }
        }

        private static string GetConnectionString(IConfiguration config, string key)
        {
            string defaultConn = "Data Source=tcp:nbappproddr.database.windows.net;Initial Catalog=NBAPP_PROD;Authentication=Active Directory Interactive;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadOnly;MultiSubnetFailover=False;MultipleActiveResultSets=true";

            return config[key] ?? Environment.GetEnvironmentVariable(key) ?? defaultConn;
        }

        private class Report
        {
            public required string SheetName { get; set; }
            public required string SheetPrev { get; set; }
            public required string SheetRaw { get; set; }
            public required string SheetDraft { get; set; }
            public required string SheetFinal { get; set; }
            public int SheetIndex { get; set; }
        }

        private async static Task Test()
        {
            // Download the Excel file from the SharePoint link
            string userName = "sankalp.godugu@nationsbenefits.com";
            string password = "SG1234567$";
            string serverFileUrl = @"https://nationshearingllc-my.sharepoint.com/:x:/r/personal/mtapia_nationsbenefits_com/_layouts/15/doc2.aspx?sourcedoc=%7B60d2b296-9492-4cf5-ad89-27bb8b4c4b24%7D&action=edit&wdorigin=BrowserReload.Sharing.ServerTransfer&wdexp=TEAMS-TREATMENT&wdhostclicktime=1712151738068&wdenableroaming=1&wdodb=1&wdlcid=en-US&wdredirectionreason=Force_SingleStepBoot&wdinitialsession=4387785d-830d-c980-5510-e324be4eb0cd&wdrldsc=6&wdrldc=2&wdrldr=FileOpenUserUnauthorized%2CDeploymentInvalidEditSess";
            string downloadPath = @"C:\Users\Sankalp.Godugu\Downloads\ManualAdjustments2023.xlsx";

            var webUrl = "https://nationshearingllc-my.sharepoint.com/:x:/r/personal/mtapia_nationsbenefits_com";
            var fileUrl = serverFileUrl;
            var accessToken = "your-access-token"; // Ensure you acquire an OAuth token as per SharePoint's requirements
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var requestUrl = $"{webUrl}/_api/web/getfilebyserverrelativeurl('{fileUrl}')/$value";
            var response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead);
            var fileContent = await response.Content.ReadAsByteArrayAsync();
            //return File(fileContent, "application/octet-stream", "your-download-file-name");
        }
    }
}
