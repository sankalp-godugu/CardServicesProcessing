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
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;

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
                            SheetName = CardServicesConstants.Nations.SheetName,
                            SheetPrev = CardServicesConstants.Nations.SheetPrev,
                            SheetCurr = CardServicesConstants.Nations.SheetCurr,
                            SheetIndex = CardServicesConstants.Nations.SheetIndex
                        },
                        new()
                        {
                            SheetName = CardServicesConstants.Elevance.SheetName,
                            SheetPrev = CardServicesConstants.Elevance.SheetPrev,
                            SheetCurr = CardServicesConstants.Elevance.SheetCurr,
                            SheetIndex = CardServicesConstants.Elevance.SheetIndex
                        }
                    ];

                    await ProcessReports(config, dataLayer, log, reportInfo);

                    log.LogInformation($"Opening the Excel file at {CardServicesConstants.FilePathCurr}...");
                    Stopwatch sw = Stopwatch.StartNew();
                    //ExcelService.OpenExcel(CardServicesConstants.FilePathCurr, log);
                    sw.Stop();
                    log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                    log.LogInformation($"Building Email...");
                    sw.Restart();
                    _ = SendEmail(log);
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
                log.LogInformation($"****************** {settings.SheetName} ******************");
                
                Stopwatch sw = new();
                string conn = GetConnectionString(config, $"{settings.SheetName}ProdConn");

                log.LogInformation($"Querying for cases...");
                sw.Start();
                // Check if data exists in cache
                if (!CacheManager.Cache.TryGetValue(settings.SheetName, out IEnumerable<CardServicesResponse> response))
                {
                    // Data not found in cache, fetch from source and store in cache
                    DynamicParameters parameters = new();
                    parameters.Add("@addressTypeCode", "PERM");
                    parameters.Add("@caseCategoryId", 1);
                    parameters.Add("@createdYear", 2024);
                    parameters.Add("@isActive", 1);
                    parameters.Add("@isProcessEligible", 1);
                    parameters.Add("@caseTopicId", 24);
                    parameters.Add("@closedYear", 2024);
                    response = await dataLayer.QueryAsyncCSS<CardServicesResponse>(conn, log, parameters);
                    _ = CacheManager.Cache.Set(settings.SheetName, response, TimeSpan.FromDays(1));
                }
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"Processing missing/invalid data...");
                sw.Restart();
                DataTable tblCurr = DataService.ValidateCases(response);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                //log.LogInformation($"Cross-referencing data with 2023 Manual Reimbursements Report...");
                //sw.Restart();
                //DataProcessingService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2023SrcFilePath, tblCurr);
                //sw.Stop();
                //log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                //log.LogInformation($"Cross-referencing data with 2024 Manual Reimbursements Report...");
                //sw.Restart();
                //DataProcessingService.FillMissingInfoFromManualReimbursementReport(CardServicesConstants.ManualReimbursements2024SrcFilePath, tblCurr);
                //sw.Stop();
                //log.LogInformation($"ElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"Getting Reimbursement Product Names by Case Number...");
                sw.Restart();
                IEnumerable<ReimbursementItem> reimbursementItems = await dataLayer.QueryAsync<ReimbursementItem>(conn, SqlConstantsReimbursementItems.GetProductNames, log);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"Populating Missing Wallet Names by Checking Reimbursed Products...");
                sw.Restart();
                DataService.FillInMissingWallets(tblCurr, reimbursementItems);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"Removing Duplicates...");
                sw.Restart();
                DataService.RemoveDuplicates(tblCurr, ColumnNames.CaseTicketNumber);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"Writing to Excel and applying filters...");
                sw.Restart();
                ExcelService.ApplyFiltersAndSaveReport(tblCurr, CardServicesConstants.FilePathCurr, settings.SheetCurr, settings.SheetIndex, log);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");
            }
        }

        public static int SendEmail(ILogger log)
        {
            string smtpServer = Environment.GetEnvironmentVariable("smtpServer");
            int smtpPort = int.Parse(Environment.GetEnvironmentVariable("smtpPort"));
            string smtpUsername = Environment.GetEnvironmentVariable("smtpUsername");
            string smtpPassword = Environment.GetEnvironmentVariable("smtpPassword");
            string fromAddress = EmailConstants.DoNotReply;
            string toAddress = EmailConstants.SabriAttalla;// EmailConstants.DavidDandridge;
            string subject = CardServicesConstants.Subject;

            using SmtpClient client = new(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true,
                Timeout = LimitConstants.Timeout
            };

            string body = GetBody();

            MailMessage message = new(fromAddress, toAddress, subject, body)
            {
                IsBodyHtml = false
            };
            //message.Headers.Add("In-Reply-To", "BN8PR15MB26738CB9A4FEFD4454B3D0639ED72@BN8PR15MB2673.namprd15.prod.outlook.com");
            //message.Headers.Add("References", "BN8PR15MB26738CB9A4FEFD4454B3D0639ED72@BN8PR15MB2673.namprd15.prod.outlook.com");
            //message.Headers.Add("In-Reply-To", "BN8PR15MB2673CF16E4FF3CCF733640149ED72@BN8PR15MB2673.namprd15.prod.outlook.com");
            //message.Headers.Add("References", "BN8PR15MB2673CF16E4FF3CCF733640149ED72@BN8PR15MB2673.namprd15.prod.outlook.com");
            //message.CC.Add(EmailConstants.DaveDandridge);
            //message.CC.Add(EmailConstants.MargaretAnnTapia);
            //message.CC.Add(EmailConstants.AustinStephensTest);

            Attachment attachment = new(CardServicesConstants.FilePathCurr);
            message.Attachments.Add(attachment);

            int numAttempts = 0;
            while (numAttempts < LimitConstants.MaxAttempts)
            {
                try
                {
                    log.LogInformation($"Sending email to Operations...");
                    client.Send(message);
                    log.LogInformation("Email sent successfully.");
                    break;
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Failed to send email: {ex.Message}");
                }
                numAttempts++;
                Thread.Sleep(LimitConstants.SleepTime);
            }

            return 0;
        }

        public static string GetBody()
        {
            return @$"
            Hi Dave,

            Please see attached reimbursement report for this week.

            Kind regards,
            Sankalp Godugu
            ";
        }

        private static string GetConnectionString(IConfiguration config, string key)
        {
            string defaultConn = "Data Source=tcp:nbappproddr.database.windows.net;Initial Catalog=NBAPP_PROD;Authentication=Active Directory Interactive;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadOnly;MultiSubnetFailover=False;MultipleActiveResultSets=true";

            return config[key] ?? Environment.GetEnvironmentVariable(key) ?? defaultConn;
        }

        private class Report
        {
            public string SheetName { get; set; }
            public string SheetPrev { get; set; }
            public string SheetCurr { get; set; }
            public int SheetIndex { get; set; }
        }

        private static async Task Test()
        {
            // Download the Excel file from the SharePoint link
            string serverFileUrl = @"https://nationshearingllc-my.sharepoint.com/:x:/r/personal/mtapia_nationsbenefits_com/_layouts/15/doc2.aspx?sourcedoc=%7B60d2b296-9492-4cf5-ad89-27bb8b4c4b24%7D&action=edit&wdorigin=BrowserReload.Sharing.ServerTransfer&wdexp=TEAMS-TREATMENT&wdhostclicktime=1712151738068&wdenableroaming=1&wdodb=1&wdlcid=en-US&wdredirectionreason=Force_SingleStepBoot&wdinitialsession=4387785d-830d-c980-5510-e324be4eb0cd&wdrldsc=6&wdrldc=2&wdrldr=FileOpenUserUnauthorized%2CDeploymentInvalidEditSess";
            string webUrl = "https://nationshearingllc-my.sharepoint.com/:x:/r/personal/mtapia_nationsbenefits_com";
            string fileUrl = serverFileUrl;
            string accessToken = "your-access-token"; // Ensure you acquire an OAuth token as per SharePoint's requirements
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string requestUrl = $"{webUrl}/_api/web/getfilebyserverrelativeurl('{fileUrl}')/$value";
            HttpResponseMessage response = await client.GetAsync(requestUrl, HttpCompletionOption.ResponseHeadersRead);
            byte[] fileContent = await response.Content.ReadAsByteArrayAsync();
            //return File(fileContent, "application/octet-stream", "your-download-file-name");
        }
    }
}
