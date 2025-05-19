using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;

namespace Exporter
{
    // Handles database operations and data access
    public class DatabaseService
    {
        private readonly string connectionString;

        // Constructor that initializes the database connection string
        public DatabaseService(string server, string database)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = database,
                IntegratedSecurity = true,
                Pooling = true
            };

            connectionString = builder.ToString();
        }

        // Imports data in batches for better performance
        public void ImportData(IEnumerable<DataRecord> records, BackgroundWorker worker)
        {
            try
            {
                const int batchSize = 50000;
                int totalCount = 0;

                using (var context = new AppDbContext(connectionString))
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Configuration.ValidateOnSaveEnabled = false;

                    foreach (var batch in BatchRecords(records, batchSize))
                    {
                        if (worker.CancellationPending) return;

                        // Use separate context for each batch
                        using (var batchContext = new AppDbContext(connectionString))
                        {
                            batchContext.Configuration.AutoDetectChangesEnabled = false;
                            batchContext.Records.AddRange(batch);
                            batchContext.SaveChanges();
                        }

                        totalCount += batch.Count();
                        worker.ReportProgress(0, $"Processed {totalCount} records");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database import failed: {ex.Message}", ex);
            }
        }

        // Method to retrieve the current maximum ID from the Records table
        public int GetCurrentMaxId()
        {
            using (var context = new AppDbContext(connectionString))
            {
                return context.Records.Max(r => (int?)r.Id) ?? 0;
            }
        }

        // Method to delete records from the Records table that have an ID greater than the specified startId
        public void DeleteRecordsAfterId(int startId)
        {
            using (var context = new AppDbContext(connectionString))
            {
                context.Database.ExecuteSqlCommand("DELETE FROM Records WHERE Id > @p0", startId);
                context.SaveChanges();
            }
        }

        // Method for splitting records into batches
        private IEnumerable<IEnumerable<DataRecord>> BatchRecords(IEnumerable<DataRecord> records, int batchSize)
        {
            var batch = new List<DataRecord>(batchSize);
            foreach (var record in records)
            {
                batch.Add(record);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<DataRecord>(batchSize);
                }
            }
            if (batch.Any()) yield return batch;
        }

        // Retrieves filtered data using LINQ queries
        public List<DataRecord> GetFilteredData(ExportFilter filter)
        {
            using (var context = new AppDbContext(connectionString))
            {
                IQueryable<DataRecord> query = context.Records.AsNoTracking();

                // Apply filters dynamically
                if (filter.DateFrom.HasValue)
                    query = query.Where(r => r.Date >= filter.DateFrom.Value.Date);

                if (filter.DateTo.HasValue)
                    query = query.Where(r => r.Date <= filter.DateTo.Value.Date);

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

                return query.ToList();
            }
        }
    }
}