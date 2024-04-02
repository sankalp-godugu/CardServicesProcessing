using CardServicesProcessor.DataAccess.Interfaces;
using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
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
        public async Task<List<T>> ExecuteReader<T>(string procedureName, Dictionary<string, object> parameters, string connectionString, ILogger logger)
        {
            logger.LogInformation($"Started calling stored procedure {procedureName} with parameters: {string.Join(", ", parameters.Select(p => $"{p.Key} = {p.Value}"))}");
            List<T> list = [];
            using (SqlConnection sqlConnection = new(connectionString))
            {
                await sqlConnection.OpenAsync();
                try
                {
                    using SqlCommand sqlCommand = new();
                    sqlCommand.CommandTimeout = GetSqlCommandTimeout(logger);
                    sqlCommand.Connection = sqlConnection;
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.CommandText = procedureName;

                    if (parameters.Count > 0)
                    {
                        sqlCommand.Parameters.AddRange([.. GetSqlParameters(parameters)]);
                    }
                    SqlDataReader dataReader = await sqlCommand.ExecuteReaderAsync();

                    list = DataReaderMapToList<T>(dataReader, logger);
                }
                catch (Exception ex)
                {
                    logger.LogError($"{procedureName} failed with exception: {ex.Message}");
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
            logger.LogInformation($"Ended calling stored procedure {procedureName}");
            return list;
        }

        public async Task<IEnumerable<T>> QueryAsyncCustom<T>(string connectionString, object? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                _ = await connection.ExecuteAsync(SQLConstantsCardServices.DropAllCSCases, transaction: transaction);
                _ = await connection.ExecuteAsync(SQLConstantsCardServices.SelectIntoAllCSCases, transaction: transaction);
                _ = await connection.ExecuteAsync(SQLConstantsCardServices.DropTblMemberInsuranceMax, transaction: transaction);
                _ = await connection.ExecuteAsync(SQLConstantsCardServices.SelectIntoMemberInsuranceMax, transaction: transaction);
                _ = await connection.ExecuteAsync(SQLConstantsCardServices.DropTblReimbursementAmount, transaction: transaction);
                _ = await connection.ExecuteAsync(SQLConstantsCardServices.SelectIntoTblReimbursementAmount, transaction: transaction);
                IEnumerable<T> result = await connection.QueryAsync<T>(SQLConstantsCardServices.SelectCases, transaction: transaction, commandTimeout: 0);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.DisposeAsync();
                // Handle exceptions (e.g., logging, error handling)
                Console.WriteLine($"Error executing SQL query: {ex.Message}");
                throw;
            }
        }

        public async Task<(IEnumerable<RawData>, IEnumerable<MemberMailingInfo>, IEnumerable<MemberCheckReimbursement>)> QueryMultipleAsyncCustom(string connectionString)
        {
            /*DynamicParameters parameters = new();
            parameters.Add("@ApprovedStatus", "Approved");
            DateTime fromDateTime = DateTime.UtcNow.AddDays(-7);
            parameters.Add("@FromDate", fromDateTime, DbType.DateTime);
            string debugQuery = SqlConstantsCheckIssuance.Query;
            foreach (var parameter in parameters.ParameterNames)
            {
                var value = parameters.Get<object>(parameter);
                debugQuery = debugQuery.Replace($"@{parameter}", value.ToString());
            }
            */

            using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

            try
            {
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.DropReimbursementPayments, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.DropReimbursementAddress1, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.DropReimbursementAddress2, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.DropReimbursementAddress3, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.DropTempFinal, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.DropReimbursementFinal, transaction: transaction);

                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.SelectIntoReimbursementPayments, transaction: transaction);

                _ = connection.Database == Databases.Elevance
                    ? await connection.ExecuteAsync(SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_ELV, transaction: transaction)
                    : await connection.ExecuteAsync(SqlConstantsCheckIssuance.SelectIntoReimbursementAddress1_NAT, transaction: transaction);

                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.SelectIntoReimbursementAddress2, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.SelectIntoReimbursementAddress3, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.SelectIntoTempFinal, transaction: transaction);
                _ = await connection.ExecuteAsync(SqlConstantsCheckIssuance.SelectIntoReimbursementFinal, transaction: transaction);

                IEnumerable<RawData> rawData = await connection.QueryAsync<RawData>(SqlConstantsCheckIssuance.SelectRawData, transaction: transaction);
                IEnumerable<MemberMailingInfo> memberMailingInfo = await connection.QueryAsync<MemberMailingInfo>(SqlConstantsCheckIssuance.SelectMemberMailingInfo, transaction: transaction);
                IEnumerable<MemberCheckReimbursement> memberCheckReimbursements = await connection.QueryAsync<MemberCheckReimbursement>(SqlConstantsCheckIssuance.SelectMemberCheckReimbursement, transaction: transaction);

                return (rawData, memberMailingInfo, memberCheckReimbursements);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Handle exceptions (e.g., logging, error handling)
                Console.WriteLine($"Error executing SQL query: {ex.Message}");
                throw;
            }
        }

        #region Private Methods

        /// <summary>
        /// Gets the sql command timeout.
        /// </summary>
        /// <returns>Returns the sql command timeout.</returns>
        private static int GetSqlCommandTimeout(ILogger logger)
        {
            try
            {
                string? sqlCommandTimeOut = Environment.GetEnvironmentVariable("SQLCommandTimeOut");
                return !string.IsNullOrEmpty(sqlCommandTimeOut) && int.TryParse(sqlCommandTimeOut, out int parsedValue) ? parsedValue : 300;
            }
            catch (Exception ex)
            {
                logger.LogError($"TimeOut with Exception: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Converts from data reader to a collection of generic objects.
        /// </summary>
        /// <typeparam name="T">The type of the object to return.</typeparam>
        /// <param name="dataReader">The DataReader.</param>
        /// <returns>Returns the parsed object from the data reader.</returns>
        private static List<T> DataReaderMapToList<T>(SqlDataReader dataReader, ILogger logger)
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
                logger.LogError($"Error occured while parsing the List to table with Exception: {ex.Message}");
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

        #endregion
    }
}
