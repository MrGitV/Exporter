using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Exporter
{
    // Interface for repository handling database operations.
    public interface IRepository
    {
        void SetConnectionString(string server, string database);
        Task TestConnectionAsync(string server, string database);
        Task ImportDataAsync(IAsyncEnumerable<DataRecord> records, IProgress<string> progress, CancellationToken cancellationToken = default);
        Task<List<DataRecord>> GetFilteredDataAsync(ExportFilter filter);
    }
}