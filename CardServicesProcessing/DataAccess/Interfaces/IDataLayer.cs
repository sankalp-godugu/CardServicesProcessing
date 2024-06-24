using CardServicesProcessor.Models.Response;
using Dapper;
using Microsoft.Extensions.Logging;

namespace CardServicesProcessor.DataAccess.Interfaces
{
    // Interface for data layer.
    public interface IDataLayer
    {
        public Task<List<T>> ExecuteReader<T>(string procedureName, Dictionary<string, object> parameters, string connectionString, ILogger log);
        public Task<IEnumerable<T>> QueryAsync<T>(string connectionString, string query, ILogger log, object? parameters = null);
        public Task<IEnumerable<T>> QueryAsyncCSS<T>(string connectionString, ILogger log, DynamicParameters? parameters = null);
        public Task<CheckIssuance> QueryReimbursements<T>(string connectionString, ILogger log, DynamicParameters? parameters = null);
    }
}
