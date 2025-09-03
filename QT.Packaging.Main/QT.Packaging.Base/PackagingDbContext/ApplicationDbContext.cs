
using Microsoft.EntityFrameworkCore;

namespace QT.Packaging.Base.PackagingDbContext
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _dbPath;

        public ApplicationDbContext(string dbPath)
        {
            _dbPath = dbPath;
            var directoryPath = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            Database.EnsureCreated();
        }

        public DbSet<ModuleConfig> ModuleConfigs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={_dbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
