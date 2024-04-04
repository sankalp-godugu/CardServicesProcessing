using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace CardServicesProcessor.DataAccess.Services
{
    /// <summary>
    /// Data Layer.
    /// </summary>
    public class DataLayer : IDataLayer
    {
        public async Task<List<T>> ExecuteReader<T>(string procedureName, Dictionary<string, object> parameters, string connectionString, ILogger log)
        {
            log.LogInformation($"Started calling stored procedure {procedureName} with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key} = {p.Value}"))}");
            List<T> list = [];

            using (SqlConnection sqlConnection = new(connectionString))
            {
                await sqlConnection.OpenAsync();

                try
                {
                    using SqlCommand sqlCommand = new()
                    {
                        CommandTimeout = GetSqlCommandTimeout(log),
                        Connection = sqlConnection,
                        CommandType = CommandType.StoredProcedure,
                        CommandText = procedureName
                    };

                    if (parameters.Count > 0)
                    {
                        sqlCommand.Parameters.AddRange([.. GetSqlParameters(parameters)]);
                    }

                    SqlDataReader dataReader = await sqlCommand.ExecuteReaderAsync();
                    list = DataReaderMapToList<T>(dataReader, log);
                }
                catch (Exception ex)
                {
                    log.LogError($"{procedureName} failed with exception: {ex.Message}");
                }
            }

            log.LogInformation($"Ended calling stored procedure {procedureName}");
            return list;
        }

        public async Task<IEnumerable<T>> QueryAsyncCustom<T>(string connectionString, ILogger log, object? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();

            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);

            try
            {
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.DropAllCSCases, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.SelectIntoAllCSCases, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.DropTblMemberInsuranceMax, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.SelectIntoMemberInsuranceMax, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.DropTblReimbursementAmount, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.SelectIntoTblReimbursementAmount, "ElapsedTime", transaction, log);

                IEnumerable<T> result = await QuerySqlAndLogMetricAsync<T>(connection, SQLConstantsCardServices.SelectCases, "ElapsedTime", transaction, log);
                return result;
            }
            catch (SqlException ex)
            {
                log.LogError(ex, $"SqlException occurred:\n" +
                $"Message: {ex.Message}\n" +
                $"Procedure: {ex.Procedure}\n" +
                $"Line Number: {ex.LineNumber}\n" +
                $"Server: {ex.Server}\n" +
                $"Class: {ex.Class}\n" +
                $"Number: {ex.Number}\n" +
                $"State: {ex.State}\n" +
                $"Source: {ex.Source}\n" +
                $"StackTrace: {ex.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                log.LogError($"Exception occurred: {ex.Message}");
                throw;
            }
        }

        public async Task<CheckIssuance> QueryMultipleAsyncCustom<T>(string connectionString, ILogger log)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();

            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);

            try
            {
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementPayments, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress1, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress2, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress3, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropTempFinal, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementFinal, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementPayments, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, connection.Database == Databases.Nations ? SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_NAT : SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_ELV, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementAddress2, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementAddress3, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoTempFinal, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementFinal, "ElapsedTime", transaction, log);

                IEnumerable<RawData> rawData = await QuerySqlAndLogMetricAsync<RawData>(connection, SqlConstantsCheckIssuance.SelectRawData, "ElapsedTime", transaction, log);
                IEnumerable<MemberMailingInfo> memberMailingInfo = await QuerySqlAndLogMetricAsync<MemberMailingInfo>(connection, SqlConstantsCheckIssuance.SelectMemberMailingInfo, "ElapsedTime", transaction, log);
                IEnumerable<MemberCheckReimbursement> memberCheckReimbursements = await QuerySqlAndLogMetricAsync<MemberCheckReimbursement>(connection, SqlConstantsCheckIssuance.SelectMemberCheckReimbursement, "ElapsedTime", transaction, log);

                return new CheckIssuance
                {
                    RawData = rawData,
                    MemberMailingInfos = memberMailingInfo,
                    MemberCheckReimbursements = memberCheckReimbursements
                };
            }
            catch (SqlException ex)
            {
                log.LogError(ex, $"SqlException occurred:\n" +
                $"Message: {ex.Message}\n" +
                $"Procedure: {ex.Procedure}\n" +
                $"Line Number: {ex.LineNumber}\n" +
                $"Server: {ex.Server}\n" +
                $"Class: {ex.Class}\n" +
                $"Number: {ex.Number}\n" +
                $"State: {ex.State}\n" +
                $"Source: {ex.Source}\n" +
                $"StackTrace: {ex.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                log.LogError($"Exception occurred: {ex.Message}");
                throw;
            }
        }

        private static int GetSqlCommandTimeout(ILogger log)
        {
            try
            {
                string? sqlCommandTimeOut = Environment.GetEnvironmentVariable("SQLCommandTimeOut");
                return !string.IsNullOrEmpty(sqlCommandTimeOut) && int.TryParse(sqlCommandTimeOut, out int parsedValue) ? parsedValue : 300;
            }
            catch (Exception ex)
            {
                log.LogError($"TimeOut with Exception: {ex.Message}");
                return -1;
            }
        }

        private static List<SqlParameter> GetSqlParameters(Dictionary<string, object> parameters)
        {
            return parameters.Select(sp => new SqlParameter(sp.Key, sp.Value)).ToList();
        }

        public static async Task ExecuteSqlAndLogMetricAsync(IDbConnection connection, string sqlCommand, string metricName, IDbTransaction transaction, ILogger log)
        {
            Stopwatch sw = Stopwatch.StartNew();
            _ = await connection.ExecuteAsync(sqlCommand, transaction: transaction);
            ILoggerExtensions.LogMetric(log, metricName, sw.Elapsed.TotalSeconds, null);
        }

        public static async Task<IEnumerable<T>> QuerySqlAndLogMetricAsync<T>(IDbConnection connection, string sqlCommand, string metricName, IDbTransaction transaction, ILogger log)
        {
            Stopwatch sw = Stopwatch.StartNew();
            IEnumerable<T> result = await connection.QueryAsync<T>(sqlCommand, transaction: transaction);
            ILoggerExtensions.LogMetric(log, metricName, sw.Elapsed.TotalSeconds, null);
            return result;
        }

        private static List<T> DataReaderMapToList<T>(SqlDataReader dataReader, ILogger log)
        {
            List<T> list = [];

            try
            {
                T obj;
                while (dataReader.Read())
                {
                    obj = Activator.CreateInstance<T>();
                    if (obj != null)
                    {
                        foreach (PropertyInfo property in obj.GetType().GetProperties())
                        {
                            if (property != null && !Equals(dataReader[property.Name], DBNull.Value))
                            {
                                property.SetValue(obj, dataReader[property.Name], null);
                            }
                        }
                        list.Add(obj);
                    }
                }
                return list;
            }
            catch (Exception ex)
            {
                log.LogError($"Error occured while parsing the List to table with Exception: {ex.Message}");
                return list;
            }
        }
    }
}
