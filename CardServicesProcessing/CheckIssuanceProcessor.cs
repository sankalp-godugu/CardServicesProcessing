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
using System.Net;
using System.Net.Mail;

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
                    List<Client> clients =
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

                    await ProcessReports(config, dataLayer, log, clients);

                    log.LogInformation($"Opening the Excel file at {CheckIssuanceConstants.FilePathCurr}...");
                    Stopwatch sw = Stopwatch.StartNew();
                    //ExcelService.OpenExcel(CheckIssuanceConstants.FilePathCurr, log);
                    sw.Stop();
                    log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                    log.LogInformation($"Started Building Email...");
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

        private static async Task ProcessReports(IConfiguration config, IDataLayer dataLayer, ILogger log, List<Client> reportInfo)
        {
            ExcelService.DeleteWorkbook(CheckIssuanceConstants.FilePathCurr);

            foreach (Client report in reportInfo)
            {
                Stopwatch sw = new();

                log.LogInformation($"****************** {report.SheetName} **********************");

                //TODO: replace with PROD string
                string conn = GetConnString(config, $"{report.SheetName}ProdConn");

                log.LogInformation($"Getting all approved reimbursements...");
                sw.Start();
                //if (!CacheManager.Cache.TryGetValue($"{report.SheetName}CheckIssuance", out CheckIssuance? dataCurr))
                //{
                    DynamicParameters parameters = new();
                    parameters.Add("@caseTopicId", 24);
                    parameters.Add("@approvedStatus", "Approved");
                    parameters.Add("@transactionStatus", "success");
                    //parameters.Add("@fromDate", DateTime.Now.AddDays(-7).Date);
                    //parameters.Add("@isCheckSent", 0);
                    CheckIssuance dataCurr = await dataLayer.QueryReimbursements<CheckIssuance>(conn, log, parameters);
                    //_ = CacheManager.Cache.Set($"{report.SheetName}CheckIssuance", dataCurr, TimeSpan.FromDays(1));
                //}
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                //log.LogInformation($"Checking for duplicates...");
                //sw.Restart();

                //sw.Stop();
                //log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"Adding data to Excel...");
                sw.Restart();
                ExcelService.AddToExcel(dataCurr);
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
            string toAddress = EmailConstants.SabriAttalla;// EmailConstants.XiaosenSun;
            string subject = CheckIssuanceConstants.Subject;

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
            //message.CC.Add(EmailConstants.MichaelDucker);
            //message.CC.Add(EmailConstants.ScottParker);
            //message.CC.Add(EmailConstants.DaveDandridge);
            //message.CC.Add(EmailConstants.MargaretAnnTapia);
            //message.CC.Add(EmailConstants.VijayanRayan);
            //message.CC.Add(EmailConstants.AustinStephensTest);

            Attachment attachment = new(CheckIssuanceConstants.FilePathCurr);
            message.Attachments.Add(attachment);

            int numAttempts = 0;
            while (numAttempts < LimitConstants.MaxAttempts)
            {
                try
                {
                    log.LogInformation($"Sending email to Finance...");
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
            Hi Xiaosen,

            Please see attached reimbursement report for this week.

            Kind regards,
            Sankalp Godugu
            ";
        }

        private static string GetConnString(IConfiguration config, string key)
        {
            string defaultConn = "Data Source=tcp:nbappproddr.database.windows.net;Initial Catalog=NBAPP_PROD;Authentication=Active Directory Interactive;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadOnly;MultiSubnetFailover=False;MultipleActiveResultSets=true";

            return config[key] ?? Environment.GetEnvironmentVariable(key) ?? defaultConn;
        }

        private static string GetTestConnString(IConfiguration config, string key)
        {
            string defaultConn = "Data Source=tcp:nhonlineordersql.database.windows.net;Initial Catalog=NHCRM_TEST3;Authentication=Active Directory Interactive;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadOnly;MultiSubnetFailover=False;MultipleActiveResultSets=true";

            return config[key] ?? Environment.GetEnvironmentVariable(key) ?? defaultConn;
        }

        private class Client
        {
            public string? SheetName { get; set; }
            public readonly Dictionary<int, string> Sheets = CheckIssuanceConstants.SheetIndexToNameMap;
        }
    }
}
