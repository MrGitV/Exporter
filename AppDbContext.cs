using System.Data.Entity;

namespace Exporter
{
    // Represents database context configuration
    public class AppDbContext : DbContext
    {
        public AppDbContext(string connectionString) : base(connectionString) { }

        public DbSet<DataRecord> Records { get; set; }
    }
}
