using Microsoft.Extensions.Logging;

namespace CardServicesProcessor.DataAccess.Interfaces
{
    // Interface for data layer.
    public interface IDataLayer
    {
        public Task<List<T>> ExecuteReader<T>(string procedureName, Dictionary<string, object> parameters, string connectionString, ILogger logger);
        public Task<IEnumerable<T>> QueryAsync<T>(string query, string connectionString, object? parameters = null);
    }
}
