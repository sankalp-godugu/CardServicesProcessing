using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using static Dapper.SqlMapper;

namespace CardServicesProcessor.DataAccess.Services
{
    /// <summary>
    /// Data Layer.
    /// </summary>
    public class DataLayer : IDataLayer
    {

        /// <summary>
        /// Executes a query and returns the collection of objects from the SQL database.
        /// </summary>
        /// <typeparam name="T">The type of the object to return.</typeparam>
        /// <param name="procedureName">The name of the stored procedure to execute.</param>
        /// <param name="parameters">The dictionary of SQL parameters to pass to the stored procedure.</param>
        /// <param name="connectionString">Connection string</param>
        /// <returns>The collection of objects returned from the executed query.</returns>
        public async Task<List<T>> ExecuteReader<T>(string procedureName, Dictionary<string, object> parameters, string connectionString, ILogger log)
        {
            log.LogInformation($"Started calling stored procedure {procedureName} with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key} = {p.Value}"))}");
            List<T> list = [];
            using (SqlConnection sqlConnection = new(connectionString))
            {
                await sqlConnection.OpenAsync();
                try
                {
                    using SqlCommand sqlCommand = new();
                    sqlCommand.CommandTimeout = GetSqlCommandTimeout(log);
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = procedureName;

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
                finally
                {
                    sqlConnection.Close();
                }
            }
            log.LogInformation($"Ended calling stored procedure {procedureName}");
            return list;
        }

        public async Task<IEnumerable<T>> QueryAsyncCustom<T>(string connectionString, ILogger log, object? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

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
                // Handle SQL-related exceptions
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
                Console.WriteLine($"Exception occurred: {ex.Message}");
                throw;
            }
        }

        public async Task<(IEnumerable<RawData>, IEnumerable<MemberMailingInfo>, IEnumerable<MemberCheckReimbursement>)> QueryMultipleAsyncCustom(string connectionString, ILogger log)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementPayments, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress1, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress2, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementAddress3, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropTempFinal, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.DropReimbursementFinal, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementPayments, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection,
                    connection.Database == Databases.Nations ? SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_NAT : SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_ELV,
                    "ElapsedTime",
                    transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementAddress2, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementAddress3, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoTempFinal, "ElapsedTime", transaction, log);
                await ExecuteSqlAndLogMetricAsync(connection, SqlConstantsCheckIssuance.SelectIntoReimbursementFinal, "ElapsedTime", transaction, log);

                var rawData = await QuerySqlAndLogMetricAsync<RawData>(connection, SqlConstantsCheckIssuance.SelectRawData, "ElapsedTime", transaction, log);
                var memberMailingInfo = await QuerySqlAndLogMetricAsync<MemberMailingInfo>(connection, SqlConstantsCheckIssuance.SelectMemberMailingInfo, "ElapsedTime", transaction, log);
                var memberCheckReimbursements = await QuerySqlAndLogMetricAsync<MemberCheckReimbursement>(connection, SqlConstantsCheckIssuance.SelectMemberCheckReimbursement, "ElapsedTime", transaction, log);

                return (rawData, memberMailingInfo, memberCheckReimbursements);
            }
            catch (SqlException ex)
            {
                // Handle SQL-related exceptions
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
                Console.WriteLine($"Exception occurred: {ex.Message}");
                throw;
            }
        }

        #region Private Methods

        /// <summary>
        /// Gets the sql command timeout.
        /// </summary>
        /// <returns>Returns the sql command timeout.</returns>
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

        /// <summary>
        /// Converts from data reader to a collection of generic objects.
        /// </summary>
        /// <typeparam name="T">The type of the object to return.</typeparam>
        /// <param name="dataReader">The DataReader.</param>
        /// <returns>Returns the parsed object from the data reader.</returns>
        private static List<T> DataReaderMapToList<T>(SqlDataReader dataReader, ILogger log)
        {
            List<T> list = [];
            try
            {
                T obj = default!;
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

        /// <summary>
        /// Gets the sql parameters.
        /// </summary>
        /// <param name="parameters">The dictionary of SQL parameters.</param>
        /// <returns>Returns the collection of sql parameters.</returns>
        private static List<SqlParameter> GetSqlParameters(Dictionary<string, object> parameters)
        {
            return parameters.Select(sp => new SqlParameter(sp.Key, sp.Value)).ToList();
        }

        public static async Task ExecuteSqlAndLogMetricAsync(IDbConnection connection, string sqlCommand, string metricName, IDbTransaction transaction, ILogger log)
        {
            var sw = Stopwatch.StartNew();
            await connection.ExecuteAsync(sqlCommand, transaction: transaction);
            ILoggerExtensions.LogMetric(log, metricName, sw.Elapsed.TotalSeconds, null);
        }

        public static async Task<IEnumerable<T>> QuerySqlAndLogMetricAsync<T>(IDbConnection connection, string sqlCommand, string metricName, IDbTransaction transaction, ILogger log)
        {
            var sw = Stopwatch.StartNew();
            var result = await connection.QueryAsync<T>(sqlCommand, transaction: transaction);
            ILoggerExtensions.LogMetric(log, metricName, sw.Elapsed.TotalSeconds, null);
            return result;
        }

        #endregion
    }
}
