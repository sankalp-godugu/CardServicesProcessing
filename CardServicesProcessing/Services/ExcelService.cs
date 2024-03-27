using ClosedXML.Excel;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReimbursementReporting.DataAccess.Interfaces;
using ReimbursementReporting.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ReimbursementReporting.Services
{
    public static class ExcelService
    {
        public static async Task<IActionResult> ProcessCardServicesData(IConfiguration configuration, IDataLayer dataLayer, ILogger logger)
        {
            try
            {
                await Task.Run(async () =>
                {
                    string appConnectionString = configuration["ElevanceProdConn"] ?? Environment.GetEnvironmentVariable("ElevanceProdConn");

                    logger?.LogInformation("********* Member PD Orders => Genesys Contact List Execution Started **********");

                    logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");


                    logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

                    // Define your SQL connection string here
                    string connectionString = Environment.GetEnvironmentVariable("SQLConnection");

                    // Your SQL query/script
                    string sqlQuery = SQLConstants.Query;

                    using (SqlConnection connection = new(connectionString))
                    {
                        System.Collections.Generic.IEnumerable<dynamic> results = await connection.QueryAsync(sqlQuery);
                        System.Collections.Generic.List<dynamic> dataTable = results.AsList();

                        if (dataTable.Count != 0)
                        {
                            using XLWorkbook workbook = new();
                            IXLWorksheet worksheet = workbook.AddWorksheet("Query Results");
                            _ = worksheet.Cell(1, 1).InsertTable(dataTable);

                            // Define your path here. Example: saving to the desktop
                            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"QueryResults-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                            workbook.SaveAs(path);

                            logger.LogInformation($"Excel file has been saved to {path}");
                        }
                        else
                        {
                            logger.LogInformation("No data returned from query.");
                        }
                    }

                    return new OkObjectResult(null);
                });

                return new OkObjectResult("Task of processing PD Orders in Genesys has been allocated to azure function and see logs for more information about its progress...");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed with an exception with message: {ex.Message}");
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}