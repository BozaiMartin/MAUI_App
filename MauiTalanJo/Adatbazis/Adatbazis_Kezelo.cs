using SQLite;

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
            await _db.CreateTableAsync<IdeiglenesAdat>();
        }
        #region IdeiglenesAdat
        public async Task MentIdeiglenesAsync(IdeiglenesAdat adat)
        {
            await _db.InsertAsync(adat);
        }

        public async Task<List<IdeiglenesAdat>> LekerdezesIdeiglenesAsync()
        {
            return await _db.Table<IdeiglenesAdat>().ToListAsync();
        }
        public async Task TorolIdeiglenesAsync()
        {
            await _db.DeleteAllAsync<IdeiglenesAdat>();
        }
        #endregion


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
