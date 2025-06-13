using System.Data.Entity;

namespace Exporter
{
    public class AppDbContext : DbContext
    {
        // Constructor accepting a connection string to initialize the DbContext
        public AppDbContext(string connectionString)
            : base(connectionString)
        {
            Database.SetInitializer<AppDbContext>(null);
        }

        // DbSet representing the collection of DataRecord entities in the database
        public DbSet<DataRecord> Records { get; set; }

        // Configures the model and maps the DataRecord entity to the "DataRecords" table
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataRecord>().ToTable("DataRecords");
            base.OnModelCreating(modelBuilder);
        }
    }
}