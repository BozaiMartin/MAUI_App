using KmKiolvasasMaui.Models;
using Microsoft.EntityFrameworkCore;

namespace KmKiolvasasMaui.Data
{
    public class PalyaszamDbContext : DbContext
    {
        public DbSet<Palyaszam> Palyaszamok { get; set; }

        private string _dbPath;

        public PalyaszamDbContext()
        {
            string folder = FileSystem.AppDataDirectory;
            _dbPath = Path.Combine(folder, "palyaszamok.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={_dbPath}");
        }
    }
}
