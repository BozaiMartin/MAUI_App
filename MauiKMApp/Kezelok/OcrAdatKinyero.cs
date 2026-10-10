using KmKiolvasasMaui.Adat_Szerkezet;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KmKiolvasasMaui.Kezelok
{
    public static class OcrAdatKinyero
    {
        public static OcrKinyertAdat AlapAdatokKinyerese(string ocrText)
        {
            OcrKinyertAdat eredmeny = new()
            {
                NyersOcrSzoveg =ocrText ?? ""
            };

            if (string.IsNullOrWhiteSpace(ocrText))
                return eredmeny;

            string normalizalt = Normalizal(ocrText);

            DatumKinyeres(normalizalt, eredmeny);

            PalyaszamKinyeres(normalizalt, eredmeny);

            return eredmeny;
        }

        public static int? KilometerErtekKinyerese(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return null;

            string normalizalt = Normalizal(ocrText);

            string[] sorok = normalizalt.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            foreach (string eredetiSor in sorok)
            {
                string sor = OsszehasonlitNormalizal(eredetiSor);
                if (!Hasonlit(sor, "MEGTETT"))
                    continue;

                int? ertek = KmKeresese(sor);

                if (ertek.HasValue)
                    return ertek;
            }

            for (int i = 0; i < sorok.Length - 1;  i++)
            {
                string aktualis = OsszehasonlitNormalizal(sorok[i]);

                if (!Hasonlit(aktualis, "MEGTETT"))
                    continue;

                string egyben = aktualis + " " + OsszehasonlitNormalizal(sorok[i + 1]);

                int? ertek = KmKeresese(egyben);

                if (ertek.HasValue)
                    return ertek;
            }

            MatchCollection kmTalalatok = Regex.Matches(normalizalt, @"(?<!\d)([0-9OIL]{1,7})\s*K\s*M\b", RegexOptions.IgnoreCase);

            List<int> ertekek = [];

            foreach (Match match in kmTalalatok)
            {
                int? ertek = SzamJavitasa(match.Groups[1].Value);

                if (ertek.HasValue)
                    ertekek.Add(ertek.Value);
            }

            ertekek = [.. ertekek.Distinct()];

            // Csak akkor fogadjuk el fallbackként,
            // ha pontosan egy km érték van.
            if (ertekek.Count == 1)
                return ertekek[0];

            return null;
        }

        private static int? KmKeresese(string szoveg)
        {
            Match match = Regex.Match(szoveg, @"(?<!\d)([0-9OIL]{1,7})\s*K\s*M\b", RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            return SzamJavitasa(match.Groups[1].Value);
        }

        private static int? SzamJavitasa(string szoveg)
        {
            if (string.IsNullOrWhiteSpace(szoveg))
                return null;

            string javitott = szoveg.ToUpperInvariant().Replace('O', '0').Replace('I', '1').Replace('L', '1');

            if (int.TryParse(javitott, out int ertek))
                return ertek;

            return null;
        }

        private static void DatumKinyeres(string szoveg, OcrKinyertAdat eredmeny)
        {
            Match match = Regex.Match(szoveg, @"\b(\d{4})[.\-/](\d{1,2})[.\-/](\d{1,2})\.?\b");

            if (!match.Success)
                return;

            if (!int.TryParse(match.Groups[1].Value, out int ev))
                return;

            if (!int.TryParse(match.Groups[2].Value, out int honap))
                return;

            if (!int.TryParse(match.Groups[3].Value, out int nap))
                return;

            try
            {
                eredmeny.Datum = new DateTime(ev, honap, nap);
            }
            catch
            {
                eredmeny.Datum = null;
            }
        }

        private static void PalyaszamKinyeres(string szoveg, OcrKinyertAdat eredmeny)
        {
            MatchCollection talalatok = Regex.Matches(szoveg, @"(?<!\d)(4\d{3})(?!\d)");

            foreach (Match match in talalatok)
            {
                if (int.TryParse(match.Groups[1].Value, out int palyaszam))
                {
                    eredmeny.Palyaszam = palyaszam;

                    return;
                }
            }
        }

        private static string Normalizal(string szoveg)
        {
            return szoveg.ToUpperInvariant().Replace("K.M", "KM").Replace("K M", "KM").Replace("K|M", "KM");
        }

        private static string OsszehasonlitNormalizal(string szoveg)
        {
            string normal = szoveg.Normalize(NormalizationForm.FormD);

            StringBuilder builder = new();

            foreach (char c in normal)
            {
                UnicodeCategory kategoria = CharUnicodeInfo.GetUnicodeCategory(c);

                if (kategoria != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }

            return builder.ToString().ToUpperInvariant();
        }

        private static bool Hasonlit(string sor, string keresett)
        {
            if (sor.Contains(keresett, StringComparison.OrdinalIgnoreCase))
                return true;

            string[] tokenek = Regex.Split(sor, @"\W+");

            foreach (string token in tokenek)
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                if (Levenshtein(token, keresett) <= 2)
                    return true;
            }

            return false;
        }

        private static int Levenshtein(string a, string b)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= b.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int koltseg = a[i - 1] ==  b[j - 1] ? 0 : 1;

                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + koltseg);
                }
            }

            return d[a.Length, b.Length];
        }
    }
}