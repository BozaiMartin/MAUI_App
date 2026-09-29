using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace KmKiolvasasMaui.Adatbazis
{
    public static class AdatbazisKulcsKezelo
    {
        private static readonly ConcurrentDictionary<string, string> _memoriaCache = new();

        public static async Task<string> KulcsAsync(
            string adatbazisNev)
        {
            if (string.IsNullOrWhiteSpace(adatbazisNev))
                throw new ArgumentException("Az adatbázis neve nem lehet üres.", nameof(adatbazisNev));

            string kulcsNev = $"db_key_{adatbazisNev.ToLowerInvariant()}";

            // 1. RAM cache
           
            if (_memoriaCache.TryGetValue(kulcsNev, out string? cacheKulcs))
                return cacheKulcs;

            // 2. SecureStorage
            
            string? kulcs = await SecureStorage.Default.GetAsync(kulcsNev);


            if (!string.IsNullOrWhiteSpace(kulcs))
            {
                _memoriaCache[kulcsNev] = kulcs;

                return kulcs;
            }

            // 3. Megnézzük, létezik-e már a DB
            

            string adatbazisPath = Path.Combine(FileSystem.AppDataDirectory, adatbazisNev);


            if (File.Exists(adatbazisPath))
            {
                 // DB már létezik, de nincs meg a kulcs, nem generálunk új kulcsot.
                 // Egy új kulccsal a meglévő SQLCipher, adatbázis nem lenne olvasható.

                throw new InvalidOperationException($"A(z) {adatbazisNev} adatbázis létezik, " + "de a titkosítási kulcsa nem található.");
            }
            
            // 4. Új DB és új kulcs
            
            byte[] bytes = RandomNumberGenerator.GetBytes(32);

            kulcs = Convert.ToBase64String(bytes);

            await SecureStorage.Default.SetAsync(kulcsNev, kulcs);

            _memoriaCache[kulcsNev] = kulcs;

            return kulcs;
        }

#if DEBUG
        public static async Task<string> FejlesztoiKulcsAsync(string adatbazisNev)
        {
            return await KulcsAsync(adatbazisNev);
        }
#endif
    }
}