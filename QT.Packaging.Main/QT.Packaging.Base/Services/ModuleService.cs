using Microsoft.EntityFrameworkCore;
using QT.Packaging.Base.PackagingDbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QT.Packaging.Base.Services
{
    public class ModuleService
    {
        private readonly string _dbPath;

        public ModuleService(string dbPath)
        {
            _dbPath = dbPath;
        }

        public async Task<List<ModuleConfig>> GetModulesAsync()
        {
            using var context = new ApplicationDbContext(_dbPath);
            return await context.ModuleConfigs
                .Where(m => m.IsPublished)
                .ToListAsync();
        }

        public async Task<ModuleConfig?> GetModuleAsync(Guid id)
        {
            using var context = new ApplicationDbContext(_dbPath);
            return await context.ModuleConfigs
                .Where(m => m.Id == id && m.IsPublished)
                .FirstOrDefaultAsync();
        }

        public async Task<ModuleConfig> AddModuleAsync(ModuleConfig module)
        {
            using var context = new ApplicationDbContext(_dbPath);
            context.ModuleConfigs.Add(module);
            await context.SaveChangesAsync();
            return module;
        }

        public async Task<bool> UpdateModuleAsync(ModuleConfig module)
        {
            using var context = new ApplicationDbContext(_dbPath);
            var existing = await context.ModuleConfigs.FindAsync(module.Id);
            if (existing == null)
                return false;

            context.Entry(existing).CurrentValues.SetValues(module);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteModuleAsync(Guid id)
        {
            using var context = new ApplicationDbContext(_dbPath);
            var module = await context.ModuleConfigs.FindAsync(id);
            if (module == null)
                return false;

            context.ModuleConfigs.Remove(module);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
