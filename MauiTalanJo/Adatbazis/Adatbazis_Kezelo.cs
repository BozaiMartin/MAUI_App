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
        public async Task<bool> EllenorizDuplikatumAsync(DateTime datum, int palyaszam, int napiKm, int osszKm)
        {
            DateTime kezdet = datum.Date;
            DateTime veg = datum.Date.AddDays(1);

            IdeiglenesAdat? letezo = await _db.Table<IdeiglenesAdat>()
                .Where(x => x.Palyaszam == palyaszam && x.Datum >= kezdet && x.Datum < veg)
                .FirstOrDefaultAsync();

            if (letezo == null)
                return false;

            if (letezo.Napi_km != napiKm || letezo.Ossz_km != osszKm)
                return false;

            return true;
        }


        #endregion

        public async Task TorolKiolvasottAdatokatAsync()
        {
            try
            {
                await _db.DeleteAllAsync<KiolvasottAdat>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hiba a KiolvasottAdat tábla ürítése közben: " + ex.Message);
                throw;
            }
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
