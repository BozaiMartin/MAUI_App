using KmKiolvasasMaui.Adat_Szerkezet;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KmKiolvasasMaui.Kezelok
{
    public static class RegisztraciosQrKezelo
    {
        private const string NyilvanosKulcs =
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEXbqUGnR6BDS7RtK3JoeNtpB5FRTjE6GPrdaEm4b3K1RGkwBM6s8VgyIP3hbIr9GKY6LLJq1ZAndmlgfyGU4M1g==";

        public static bool Ellenorzes(
            string qrTartalom,
            out RegisztraciosAdat? adat,
            out string hiba)
        {
            adat = null;
            hiba = "";

            if (string.IsNullOrWhiteSpace(qrTartalom))
            {
                hiba = "A QR-kód üres.";
                return false;
            }

            try
            {
                string[] reszek = qrTartalom.Split('.');

                if (reszek.Length != 2)
                {
                    hiba = "Érvénytelen regisztrációs QR-kód.";
                    return false;
                }

                byte[] adatBytes = Base64UrlDecode(reszek[0]);

                byte[] alairasBytes = Base64UrlDecode(reszek[1]);

                byte[] nyilvanosKulcsBytes =
                    Convert.FromBase64String(NyilvanosKulcs);

                using ECDsa ecdsa = ECDsa.Create();

                ecdsa.ImportSubjectPublicKeyInfo(
                    nyilvanosKulcsBytes,
                    out _);

                bool ervenyes = ecdsa.VerifyData(
                    adatBytes,
                    alairasBytes,
                    HashAlgorithmName.SHA256);

                if (!ervenyes)
                {
                    hiba = "A QR-kód aláírása érvénytelen.";
                    return false;
                }

                adat = JsonSerializer.Deserialize<RegisztraciosAdat>(
                    Encoding.UTF8.GetString(adatBytes));

                if (adat == null)
                {
                    hiba = "A regisztrációs adatok nem olvashatók.";
                    return false;
                }

                if (adat.Verzio != 1)
                {
                    hiba = "Nem támogatott regisztrációs QR-kód.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(adat.DolgozoSzam) ||
                    string.IsNullOrWhiteSpace(adat.DolgozoNev) ||
                    string.IsNullOrWhiteSpace(adat.Szervezet) ||
                    string.IsNullOrWhiteSpace(adat.Telephely))
                {
                    hiba = "Hiányos regisztrációs adatok.";
                    return false;
                }

                if (DateTime.UtcNow > adat.LejaratUtc.ToUniversalTime())
                {
                    hiba = "A regisztrációs QR-kód lejárt.";
                    return false;
                }

                return true;
            }
            catch
            {
                hiba = "A regisztrációs QR-kód nem feldolgozható.";
                return false;
            }
        }

        private static byte[] Base64UrlDecode(string adat)
        {
            string base64 = adat
                .Replace('-', '+')
                .Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2:
                    base64 += "==";
                    break;

                case 3:
                    base64 += "=";
                    break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}