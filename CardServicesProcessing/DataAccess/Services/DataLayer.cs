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
                    log.LogInformation($"{procedureName} failed with exception: {ex.Message}");
                }
            }

            log.LogInformation($"Ended calling stored procedure {procedureName}");
            return list;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string connectionString, string query, ILogger log, object? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();

            // Execute the query asynchronously
            var result = await connection.QueryAsync<T>(query);

            return result;
        }

        public async Task<IEnumerable<T>> QueryAsyncCustom<T>(string connectionString, ILogger log, object? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();

            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);

            try
            {
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.DropAllCSCases, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.SelectIntoAllCSCases, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.DropTblMemberInsuranceMax, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.SelectIntoMemberInsuranceMax, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.DropTblReimbursementAmount, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SQLConstantsCardServices.SelectIntoTblReimbursementAmount, transaction, log);

                IEnumerable<T> result = await QuerySqlAndLogMetricAsync<T>(connection, SQLConstantsCardServices.SelectCases, transaction, log);
                return result;
            }
            catch (SqlException ex)
            {
                log.LogInformation($"SqlException occurred: " +
                $"\nMessage: {ex.Message}\n" +
                $"\nProcedure: {ex.Procedure}\n" +
                $"\nLine Number: {ex.LineNumber}\n" +
                $"\nServer: {ex.Server}\n" +
                $"\nClass: {ex.Class}\n" +
                $"\nNumber: {ex.Number}\n" +
                $"\nState: {ex.State}\n" +
                $"\nSource: {ex.Source}\n" +
                $"\nStackTrace: {ex.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                log.LogInformation($"Exception occurred: {ex.Message}");
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
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementPayments, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress1, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress2, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress3, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropTempFinal, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementFinal, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementPayments, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, connection.Database == Databases.Nations ? SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_NAT : SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_ELV, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementAddress2, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementAddress3, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoTempFinal, transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementFinal, transaction, log);

                IEnumerable<RawData> rawData = await QuerySqlAndLogMetricAsync<RawData>(connection, SqlConstantsCheckIssuance.SelectRawData, transaction, log);
                IEnumerable<MemberMailingInfo> memberMailingInfo = await QuerySqlAndLogMetricAsync<MemberMailingInfo>(connection, SqlConstantsCheckIssuance.SelectMemberMailingInfo, transaction, log);
                IEnumerable<MemberCheckReimbursement> memberCheckReimbursements = await QuerySqlAndLogMetricAsync<MemberCheckReimbursement>(connection, SqlConstantsCheckIssuance.SelectMemberCheckReimbursement, transaction, log);

                return new CheckIssuance
                {
                    RawData = rawData,
                    MemberMailingInfos = memberMailingInfo,
                    MemberCheckReimbursements = memberCheckReimbursements
                };
            }
            catch (SqlException ex)
            {
                log.LogInformation($"SqlException occurred: " +
                $"\nMessage: {ex.Message}\n" +
                $"\nProcedure: {ex.Procedure}\n" +
                $"\nLine Number: {ex.LineNumber}\n" +
                $"\nServer: {ex.Server}\n" +
                $"\nClass: {ex.Class}\n" +
                $"\nNumber: {ex.Number}\n" +
                $"\nState: {ex.State}\n" +
                $"\nSource: {ex.Source}\n" +
                $"\nStackTrace: {ex.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                log.LogInformation($"Exception occurred: {ex.Message}");
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
                log.LogInformation($"TimeOut with Exception: {ex.Message}");
                return -1;
            }
        }

        private static List<SqlParameter> GetSqlParameters(Dictionary<string, object> parameters)
        {
            return parameters.Select(sp => new SqlParameter(sp.Key, sp.Value)).ToList();
        }

        public static async Task ExecuteSqlAndLogMetricAsync(IDbConnection connection, string sqlCommand, IDbTransaction transaction, ILogger log)
        {
            Stopwatch sw = Stopwatch.StartNew();
            _ = await connection.ExecuteAsync(sqlCommand, transaction: transaction);
            log.LogInformation($"{nameof(sqlCommand)} > {sw.Elapsed.TotalSeconds} sec");
        }

        public static async Task<IEnumerable<T>> QuerySqlAndLogMetricAsync<T>(IDbConnection connection, string sqlCommand, IDbTransaction transaction, ILogger log)
        {
            Stopwatch sw = Stopwatch.StartNew();
            IEnumerable<T> result = await connection.QueryAsync<T>(sqlCommand, transaction: transaction);
            log.LogInformation($"{nameof(sqlCommand)} > {sw.Elapsed.TotalSeconds} sec");
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
                log.LogInformation($"Error occured while parsing the List to table with Exception: {ex.Message}");
                return list;
            }
        }
    }
}
