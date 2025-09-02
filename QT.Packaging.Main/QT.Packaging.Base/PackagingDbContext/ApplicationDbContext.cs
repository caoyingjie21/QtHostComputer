
using Microsoft.EntityFrameworkCore;

namespace QT.Packaging.Base.PackagingDbContext
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _dbPath;
    }
}
