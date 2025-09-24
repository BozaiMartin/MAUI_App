using SQLite;
using System.IO;

namespace KmKiolvasasMaui.Adatbazis
{
    public class Adatbazis_Kezelo
    {
        private readonly SQLiteAsyncConnection _db;

        public Adatbazis_Kezelo()
        {
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "kmadatok.db");
            _db = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InicializalasAsync()
        {
            await _db.CreateTableAsync<KiolvasottAdat>();
            await _db.CreateTableAsync<PalyaszamInfo>();
        }

        public async Task MentesAsync(KiolvasottAdat adat)
        {
            await _db.InsertAsync(adat);
        }

        public async Task<List<KiolvasottAdat>> LekerdezesAsync()
        {
            return await _db.Table<KiolvasottAdat>().ToListAsync();
        }
    }
}
