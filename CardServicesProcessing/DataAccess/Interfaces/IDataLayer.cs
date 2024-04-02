using CardServicesProcessor.Models.Response;
using Microsoft.Extensions.Logging;

namespace CardServicesProcessor.DataAccess.Interfaces
{
    // Interface for data layer.
    public interface IDataLayer
    {
        public Task<List<T>> ExecuteReader<T>(string procedureName, Dictionary<string, object> parameters, string connectionString, ILogger log);
        public Task<IEnumerable<T>> QueryAsyncCustom<T>(string connectionString, ILogger log, object? parameters = null);
        public Task<(IEnumerable<RawData>, IEnumerable<MemberMailingInfo>, IEnumerable<MemberCheckReimbursement>)> QueryMultipleAsyncCustom(string connectionString, ILogger log);
    }
}
