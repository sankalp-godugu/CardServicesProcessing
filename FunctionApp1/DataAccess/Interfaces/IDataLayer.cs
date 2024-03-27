using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReimbursementReporting.DataAccess.Interfaces
{
    // Interface for data layer.
    public interface IDataLayer
    {
        public Task<List<T>> ExecuteReader<T>(string procedureName, Dictionary<string, object> parameters, string connectionString, ILogger logger);
        public Task<IEnumerable<T>> QueryAsync<T>(string query, string connectionString, object parameters = null);
    }
}
