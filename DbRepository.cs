using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Exporter
{
    public class DbRepository : IRepository
    {
        private string connectionString;

        // Sets the connection string for the database based on server and database names.
        public void SetConnectionString(string server, string database)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = true
            };
            connectionString = builder.ToString();
        }

        // Tests the database connection asynchronously.
        public async Task TestConnectionAsync(string server, string database)
        {
            var connStr = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = true
            }.ToString();

            using var connection = new SqlConnection(connStr);
            await connection.OpenAsync();
        }

        // Imports data records asynchronously into the database in batches.
        public async Task ImportDataAsync(IAsyncEnumerable<DataRecord> records, IProgress<string> progress, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Connection string is not initialized.");

            const int batchSize = 10000;
            var batch = new List<DataRecord>(batchSize);
            int totalCount = 0;

            using var context = new AppDbContext(connectionString);
            context.Configuration.AutoDetectChangesEnabled = false;
            context.Configuration.ValidateOnSaveEnabled = false;

            await foreach (var record in records.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                batch.Add(record);
                totalCount++;

                if (batch.Count >= batchSize)
                {
                    context.Records.AddRange(batch);
                    await context.SaveChangesAsync(cancellationToken);
                    foreach (var entry in context.ChangeTracker.Entries())
                        entry.State = EntityState.Detached;

                    progress?.Report($"Processed {totalCount} records");
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                context.Records.AddRange(batch);
                await context.SaveChangesAsync(cancellationToken);
                foreach (var entry in context.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }
                progress?.Report($"Total processed: {totalCount} records");
            }
        }

        // Retrieves filtered data records asynchronously from the database.
        public async Task<List<DataRecord>> GetFilteredDataAsync(ExportFilter filter)
        {
            using var context = new AppDbContext(connectionString);
            IQueryable<DataRecord> query = context.Records.AsNoTracking();

            if (filter.DateFrom.HasValue)
                query = query.Where(r => r.Date >= filter.DateFrom.Value);
            if (filter.DateTo.HasValue)
                query = query.Where(r => r.Date <= filter.DateTo.Value);
            if (!string.IsNullOrWhiteSpace(filter.FirstName))
                query = query.Where(r => r.FirstName.Contains(filter.FirstName));
            if (!string.IsNullOrWhiteSpace(filter.LastName))
                query = query.Where(r => r.LastName.Contains(filter.LastName));
            if (!string.IsNullOrWhiteSpace(filter.SurName))
                query = query.Where(r => r.SurName.Contains(filter.SurName));
            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(r => r.City == filter.City);
            if (!string.IsNullOrWhiteSpace(filter.Country))
                query = query.Where(r => r.Country == filter.Country);

            return await query.ToListAsync();
        }
    }
}
