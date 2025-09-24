using KmKiolvasasMaui.Models;
using Microsoft.EntityFrameworkCore;

namespace KmKiolvasasMaui.Data
{
    public class AdatDbContext : DbContext
    {
        public DbSet<Adat> Adatok { get; set; }

        private string _dbPath;

        public AdatDbContext()
        {
            string folder = FileSystem.AppDataDirectory;
            _dbPath = Path.Combine(folder, "adatok.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Filename={_dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Adat>()
                .HasOne(a => a.Palyaszam)
                .WithMany()
                .HasForeignKey(a => a.PalyaszamId);
        }
    }
}
