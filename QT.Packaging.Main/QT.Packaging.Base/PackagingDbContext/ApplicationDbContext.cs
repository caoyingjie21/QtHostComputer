
using Microsoft.EntityFrameworkCore;

namespace QT.Packaging.Base.PackagingDbContext
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _dbPath;

        public ApplicationDbContext(string dbPath)
        {
            _dbPath = dbPath;
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
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
