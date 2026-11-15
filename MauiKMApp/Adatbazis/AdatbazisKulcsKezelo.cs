using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace KmKiolvasasMaui.Adatbazis
{
    public static class AdatbazisKulcsKezelo
    {
        private static readonly ConcurrentDictionary<string, string> _memoriaCache = new();

        public static async Task<string> KulcsAsync(string adatbazisNev)
        {
            if (string.IsNullOrWhiteSpace(adatbazisNev))
                throw new ArgumentException("Az adatbázis neve nem lehet üres.", nameof(adatbazisNev));

            string kulcsNev = $"db_key_{adatbazisNev.ToLowerInvariant()}";

            // RAM cache
           
            if (_memoriaCache.TryGetValue(kulcsNev, out string? cacheKulcs))
                return cacheKulcs;

            // SecureStorage
            
            string? kulcs = await SecureStorage.Default.GetAsync(kulcsNev);


            if (!string.IsNullOrWhiteSpace(kulcs))
            {
                _memoriaCache[kulcsNev] = kulcs;

                return kulcs;
            }

            string adatbazisPath = Path.Combine(FileSystem.AppDataDirectory, adatbazisNev);


            if (File.Exists(adatbazisPath))
                throw new InvalidOperationException($"A(z) {adatbazisNev} adatbázis létezik, " + "de a titkosítási kulcsa nem található.");
            
            // Új DB és új kulcs
            
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