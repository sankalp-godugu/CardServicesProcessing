﻿using CardServicesProcessor.DataAccess.Interfaces;
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
                    List<Report> reportInfo =
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
                    //ExcelService.OpenExcel(CheckIssuanceConstants.FilePathCurr, log);
                    sw.Stop();
                    log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                    log.LogInformation($"Sending Email to Finance...");
                    sw.Restart();
                    SendEmail(log);
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

        private static async Task ProcessReports(IConfiguration config, IDataLayer dataLayer, ILogger log, List<Report> reportInfo)
        {
            ExcelService.DeleteWorkbook(CheckIssuanceConstants.FilePathCurr);

            foreach (Report report in reportInfo)
            {
                Stopwatch sw = new();

                log.LogInformation($"{report.SheetName} > Processing data");

                //TODO: replace with PROD string
                string conn = GetConnString(config, $"{report.SheetName}ProdConn");

                log.LogInformation($"{report.SheetName} > Getting all approved reimbursements");
                sw.Start();
                if (!CacheManager.Cache.TryGetValue($"{report.SheetName}CheckIssuance", out CheckIssuance? dataCurr))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@caseTopicId", 24);
                    parameters.Add("@approvedStatus", "Approved");
                    parameters.Add("@transactionStatus", "success");
                    parameters.Add("@fromDate", DateTime.Now.AddDays(-7).Date);
                    parameters.Add("@isCheckSent", 0);
                    dataCurr = await dataLayer.QueryReimbursements<CheckIssuance>(conn, log, parameters);
                    _ = CacheManager.Cache.Set($"{report.SheetName}CheckIssuance", dataCurr, TimeSpan.FromDays(1));
                }
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");

                log.LogInformation($"{report.SheetName} > Adding data to Excel");
                sw.Restart();
                ExcelService.AddToExcel<CheckIssuance>(dataCurr);
                sw.Stop();
                log.LogInformation($"TotalElapsedTime: {sw.Elapsed.TotalSeconds} sec");
            }
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

        private class Report
        {
            public required string SheetName { get; set; }
            public readonly Dictionary<int, string> Sheets = CheckIssuanceConstants.SheetIndexToNameMap;
        }

        public static int SendEmail(ILogger log)
        {
            string smtpServer = Environment.GetEnvironmentVariable("smtpServer");
            int smtpPort = int.Parse(Environment.GetEnvironmentVariable("smtpPort"));
            string smtpUsername = Environment.GetEnvironmentVariable("smtpUsername");
            string smtpPassword = Environment.GetEnvironmentVariable("smtpPassword");
            string fromAddress = EmailConstants.SankalpGodugu;
            string toAddress = EmailConstants.SankalpGodugu;// EmailConstants.XiaosenSun;
            string subject = CheckIssuanceConstants.Subject;

            using SmtpClient client = new(smtpServer, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                Timeout = EmailConstants.Timeout
            };

            string body = GetBody();

            MailMessage message = new(fromAddress, toAddress, subject, body)
            {
                IsBodyHtml = false
            };
            //message.CC.Add(EmailConstants.MichaelDucker);
            //message.CC.Add(EmailConstants.DaveDandridge);
            //message.CC.Add(EmailConstants.MargaretAnnTapia);
            //message.CC.Add(EmailConstants.VijayanRayan);

            Attachment attachment = new(CheckIssuanceConstants.FilePathCurr);
            message.Attachments.Add(attachment);

            int numAttempts = 0;
            while (numAttempts < LimitConstants.MaxAttempts)
            {
                try
                {
                    log.LogInformation($"Sending email to Client Services...");
                    client.Send(message);
                    log.LogInformation("Email sent successfully.");
                    break;
                }
                catch (SmtpException)
                {
                    log.LogInformation($"Sending email timed out.");
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

            Please see attached reimbursement report for this week:
            {CheckIssuanceConstants.FilePathCurr}

            Kind regards,
            Sankalp Godugu
            ";
        }
    }
}
