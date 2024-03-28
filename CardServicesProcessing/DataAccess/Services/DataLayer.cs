using CardServicesProcessor.DataAccess.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;

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

        public async Task<IEnumerable<T>> QueryAsync<T>(string query, string connectionString, object? parameters = null)
        {
            using SqlConnection connection = new(connectionString);
            try
            {
                await connection.OpenAsync();

                return await connection.QueryAsync<T>(query, parameters);
            }
            catch (Exception ex)
            {
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
