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

            using SqlConnection sqlConnection = new(connectionString);

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

            log.LogInformation($"Ended calling stored procedure {procedureName}");
            return list;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string connectionString, string query, ILogger log, object? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();

            // Execute the query asynchronously
            IEnumerable<T> result = await connection.QueryAsync<T>(query);

            return result;
        }

        public async Task<IEnumerable<T>> QueryAsyncCSS<T>(string connectionString, ILogger log, DynamicParameters? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);

            try
            {
                string combinedSql = SQLConstantsCardServices.DropTblAllCases
                                    + SQLConstantsCardServices.DropTblMemberInsuranceMax
                                    + SQLConstantsCardServices.DropTblReimbursementAmount
                                    + SQLConstantsCardServices.SelectIntoTblAllCases
                                    + SQLConstantsCardServices.SelectIntoTblMemberInsuranceMax
                                    + SQLConstantsCardServices.SelectIntoTblReimbursementAmount
                                    + SQLConstantsCardServices.SelectFromTblCases;

                IEnumerable<T> result = await QuerySqlAndLogMetricAsync<T>(connection, combinedSql, transaction, log, parameters);
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

        public async Task<CheckIssuance> QueryReimbursements<T>(string connectionString, ILogger log, DynamicParameters? parameters)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);

            try
            {
                string combinedSql = SqlConstantsCheckIssuance.DropReimbursementPayments
                                    + SqlConstantsCheckIssuance.DropReimbursementAddress1
                                    + SqlConstantsCheckIssuance.DropReimbursementAddress2
                                    + SqlConstantsCheckIssuance.DropReimbursementAddress3
                                    + SqlConstantsCheckIssuance.DropTempFinal
                                    + SqlConstantsCheckIssuance.DropReimbursementFinal
                                    + SqlConstantsCheckIssuance.DropMemberMailingInfo
                                    + SqlConstantsCheckIssuance.DropMemberCheckReimbursement
                                    + SqlConstantsCheckIssuance.SelectIntoReimbursementPayments
                                    + connection.Database == Databases.Nations
                                        ? SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_NAT
                                        : SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_ELV
                                    + SqlConstantsCheckIssuance.SelectIntoReimbursementAddress2
                                    + SqlConstantsCheckIssuance.SelectIntoReimbursementAddress3
                                    + SqlConstantsCheckIssuance.SelectIntoTempFinal
                                    + SqlConstantsCheckIssuance.SelectIntoReimbursementFinal
                                    + SqlConstantsCheckIssuance.SelectIntoMemberMailingInfo
                                    + SqlConstantsCheckIssuance.SelectIntoMemberCheckReimbursement;

                await ExecuteSqlAndLogMetricAsync(connection, combinedSql, transaction, log, parameters);

                IEnumerable<RawData> rawData = await QuerySqlAndLogMetricAsync<RawData>(connection, SqlConstantsCheckIssuance.SelectRawData, transaction, log, parameters, nameof(SqlConstantsCheckIssuance.SelectRawData));

                IEnumerable<MemberMailingInfo> memberMailingInfo = await QuerySqlAndLogMetricAsync<MemberMailingInfo>(connection, SqlConstantsCheckIssuance.SelectIntoMemberMailingInfo, transaction, log, parameters, nameof(SqlConstantsCheckIssuance.SelectIntoMemberMailingInfo));

                IEnumerable<MemberCheckReimbursement> memberCheckReimbursements = await QuerySqlAndLogMetricAsync<MemberCheckReimbursement>(connection, SqlConstantsCheckIssuance.SelectIntoMemberCheckReimbursement, transaction, log, parameters, nameof(SqlConstantsCheckIssuance.SelectIntoMemberCheckReimbursement));

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

        public static async Task ExecuteSqlAndLogMetricAsync(IDbConnection connection, string sqlCommand, IDbTransaction transaction, ILogger log, DynamicParameters? parameters = null, string? queryName = null)
        {
            //log.LogInformation($"{queryName} > Running...");
            Stopwatch sw = Stopwatch.StartNew();
            _ = await connection.ExecuteAsync(sqlCommand, parameters, transaction);
            log.LogInformation($"{queryName} > {sw.Elapsed.TotalSeconds} sec");
        }

        public static async Task<IEnumerable<T>> QuerySqlAndLogMetricAsync<T>(IDbConnection connection, string sqlCommand, IDbTransaction transaction, ILogger log, DynamicParameters? parameters = null, string? queryName = null)
        {
            //log.LogInformation($"{queryName} > Running...");
            Stopwatch sw = Stopwatch.StartNew();
            IEnumerable<T> result = await connection.QueryAsync<T>(sqlCommand, parameters, transaction);
            log.LogInformation($"{queryName} > {sw.Elapsed.TotalSeconds} sec");
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
