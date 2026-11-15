using System.Security.Cryptography;

namespace KmKiolvasasMaui.Adatbazis
{
    public static class JelszoKezelo
    {
        // 32 byte = 256 bit
        private const int SaltMeret = 32;

        // 32 byte = 256 bit
        private const int HashMeret = 32;

        // PBKDF2 ismétlésszám
        private const int Iteraciok = 50_000;

        private static readonly HashAlgorithmName HashAlgoritmus = HashAlgorithmName.SHA512;

        
        public static (string Hash, string Salt) HashKeszites(string jelszo)
        {
            if (string.IsNullOrWhiteSpace(jelszo))
                throw new ArgumentException("A jelszó nem lehet üres.", nameof(jelszo));

            byte[] salt = RandomNumberGenerator.GetBytes(SaltMeret);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password: jelszo, salt: salt, iterations: Iteraciok, hashAlgorithm: HashAlgoritmus, outputLength: HashMeret);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        public static bool Ellenorzes(string jelszo, string taroltHash, string taroltSalt)
        {
            if (string.IsNullOrWhiteSpace(jelszo))
                return false;

            if (string.IsNullOrWhiteSpace(taroltHash))
                return false;

            if (string.IsNullOrWhiteSpace(taroltSalt))
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(taroltSalt);
                byte[] vartHash = Convert.FromBase64String(taroltHash);
                byte[] kapottHash = Rfc2898DeriveBytes.Pbkdf2(password: jelszo, salt: salt, iterations: Iteraciok, hashAlgorithm: HashAlgoritmus, outputLength: vartHash.Length);

                return CryptographicOperations.FixedTimeEquals(vartHash, kapottHash);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static bool JelszoLejart(DateTime jelszoLejarat)
        {
            if (jelszoLejarat.Kind == DateTimeKind.Unspecified)
                jelszoLejarat = DateTime.SpecifyKind(jelszoLejarat, DateTimeKind.Utc);

            return DateTime.UtcNow >= jelszoLejarat.ToUniversalTime();
        }

        public static bool JelszoCsereKotelezo(int frissit, DateTime jelszoLejarat)
        {
            return frissit == 1 || JelszoLejart(jelszoLejarat);
        }

        public static DateTime UjLejarat(int napok = 90)
        {
            if (napok <= 0)
                throw new ArgumentOutOfRangeException(nameof(napok), "A lejárati időnek pozitívnak kell lennie.");

            return DateTime.UtcNow.AddDays(napok);
        }

        public static bool JelszoMegfelelo(string jelszo, out string hiba)
        {
            if (string.IsNullOrWhiteSpace(jelszo))
            {
                hiba = "A jelszó nem lehet üres.";
                return false;
            }

            if (jelszo.Length < 8)
            {
                hiba = "A jelszónak legalább 8 karakter hosszúnak kell lennie.";
                return false;
            }

            if (!jelszo.Any(char.IsLetter))
            {
                hiba = "A jelszónak tartalmaznia kell legalább egy betűt.";
                return false;
            }

            if (!jelszo.Any(char.IsDigit))
            {
                hiba = "A jelszónak tartalmaznia kell legalább egy számot.";
                return false;
            }

            hiba = string.Empty;
            return true;
        }
    }
}