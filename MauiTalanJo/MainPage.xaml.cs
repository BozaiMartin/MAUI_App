using Plugin.Maui.OCR;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Maui.Networking;
using KmKiolvasasMaui.Adatbazis;

namespace KmKiolvasasMaui
{
    public partial class MainPage : ContentPage
    {
        public static Adatbazis_Kezelo Adatbazis { get; private set; }
        public MainPage()
        {
            InitializeComponent();
            Adatbazis = new Adatbazis_Kezelo();
            Inicializal();
        }
        private async void Inicializal()
        {
            await Adatbazis.InicializalasAsync();
        }
        protected async override void OnAppearing()
        {
            base.OnAppearing();
            await OcrPlugin.Default.InitAsync();
        }
        private async void SelectBtn_Clicked(object sender, EventArgs e)
        {
            await KepFeldolgoz(async () => await MediaPicker.Default.PickPhotoAsync());
        }

        private async void PictureBtn_Clicked(object sender, EventArgs e)
        {
            await KepFeldolgoz(async () => await MediaPicker.Default.CapturePhotoAsync());
        }

        private async Task KepFeldolgoz(Func<Task<FileResult?>> kepValasztVagyKeszit)
        {
            try
            {
                bool vanNet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

                FileResult? kepEredmeny = await kepValasztVagyKeszit();
                if (kepEredmeny != null)
                {
                    using Stream imageAsStream = await kepEredmeny.OpenReadAsync();
                    byte[] imageAsBytes = new byte[imageAsStream.Length];
                    await imageAsStream.ReadAsync(imageAsBytes);
                    OcrResult? ocrResult = await OcrPlugin.Default.RecognizeTextAsync(imageAsBytes, true);

                    if (ocrResult.Success)
                    {
                        var adatok = OcrAdatokKinyeres(ocrResult.AllText);
                        // Ellenőrzés
                        List<string> hianyok = new();
                        if (string.IsNullOrWhiteSpace(adatok.datum)) hianyok.Add("Dátum");
                        if (string.IsNullOrWhiteSpace(adatok.palyaszam)) hianyok.Add("Pályaszám");
                        if (string.IsNullOrWhiteSpace(adatok.napiKm)) hianyok.Add("Napi km");
                        if (string.IsNullOrWhiteSpace(adatok.osszKm)) hianyok.Add("Összes km");

                        if (hianyok.Count != 0)
                        {
                            string msg = "A következő adatok hiányoznak: " +
                                         string.Join(", ", hianyok) +
                                         "\nKérlek, fényképezd újra!";
                            await DisplayAlert("Hiányzó adatok", msg, "OK");
                            return;
                        }

                        // Ha minden adat megvan → email küldés
                        string tabla =
                            $"Dátum: {adatok.datum}\n" +
                            $"Pályaszám: {adatok.palyaszam}\n\n" +
                            $"Napi km: {adatok.napiKm} km\n" +
                            $"Összes km: {adatok.osszKm} km";

                        if (vanNet)
                        {
                            await EmailKuld.KuldesAsync(
                                "bozaimartin@gmail.com",
                                "Kiolvasott adatok",
                                tabla);

                            await DisplayAlert("Siker", "Az adatok kiolvasva és az email elküldve.", "OK");

                            KiolvasottAdat adat = new KiolvasottAdat
                            {
                                Datum = DateTime.Parse(adatok.datum),
                                Palyaszam = int.Parse(adatok.palyaszam),
                                Napi_km = int.Parse(adatok.napiKm),
                                Ossz_km = int.Parse(adatok.osszKm),
                                Email_kuldve = true
                            };
                            await Adatbazis.MentesAsync(adat);
                        }
                        else
                        {
                            KiolvasottAdat adat = new KiolvasottAdat
                            {
                                Datum = DateTime.Parse(adatok.datum),
                                Palyaszam = int.Parse(adatok.palyaszam),
                                Napi_km = int.Parse(adatok.napiKm),
                                Ossz_km = int.Parse(adatok.osszKm),
                                Email_kuldve = false
                            };
                            await Adatbazis.MentesAsync(adat);
                            await DisplayAlert("Offline mentés", "Nincs internetkapcsolat, az adatokat mentettük az adatbázisba.", "OK");
                        }



                        await DisplayAlert("Siker", "Az adatok kiolvasva és az email megnyitva küldéshez.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"Hiba történt: {ex.Message}", "OK");
            }
        }

        private (string datum, string palyaszam, string napiKm, string osszKm) OcrAdatokKinyeres(string ocrText)
        {
            string datum = "";
            string palyaszam = "";
            string napiKm = "";
            string osszKm = "";

            if (string.IsNullOrWhiteSpace(ocrText))
                return (datum, palyaszam, napiKm, osszKm);

            // 1) Előfeldolgozás: nagybetű + gyakori OCR-hibák javítása token-szinten
            string pre = ocrText.ToUpperInvariant();

            Dictionary<string, string> fixes = new Dictionary<string, string>
            {
                // km variánsok
                {"KLM", "KM"}, {"K1M", "KM"}, {"K I M", "KM"}, {"K|M", "KM"}, {"KIR", "KM"}, {"K1R","KM"},
                // összes / rosszul olvasott O hiány
                {"SSZES", "OSSZES"}, {"OSZES", "OSSZES"},
                // egyéb gyakori rövidítések/javítások
                {"MEGTET", "MEGTETT"}, {"MEGTETTIT", "MEGTETT"}, {"MEGTETT IT", "MEGTETT UT"}, {"MEGTETTUT", "MEGTETT UT"}
            };

            foreach (var kv in fixes)
                pre = Regex.Replace(pre, @"\b" + Regex.Escape(kv.Key) + @"\b", kv.Value, RegexOptions.IgnoreCase);

            // sorokra bontás
            string[] lines = pre.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToArray();

            // 2) Dátum keresés (szigorú mintával: YYYY.MM.DD.)
            var dm = Regex.Match(pre, @"\b\d{4}\.\d{2}\.\d{2}\.");
            if (dm.Success)
                datum = dm.Value.Trim();
            else datum = DateTime.Today.ToString("yyyy.MM.dd.");

            // 3) Pályaszám: első "tiszta" 3-5 jegyű szám a sorok között
            foreach (var sor in lines)
                {
                    // csak 4-essel kezdődő, 4 számjegyű számokat keresünk
                    var p = Regex.Match(sor, @"\b(4\d{3})\b");
                    if (p.Success)
                    {
                        var talalt = p.Groups[1].Value;

                        palyaszam = talalt;
                        break;
                    }
                }

            // 4) Fejlécek keresése (NAPI, OSSZES)
            // 4) Napi / Összes km keresés konkrét mintával
            var napiM = Regex.Match(pre, @"MEGTETT\s*ÚT\s*=\s*(\d+)\s*KM", RegexOptions.IgnoreCase);
            if (napiM.Success)
                napiKm = napiM.Groups[1].Value;

            var osszIdx = Array.FindIndex(lines, l => HasonlotTartalmaz(l, "OSSZES", 2));
            if (osszIdx >= 0)
            {
                for (int i = osszIdx; i < Math.Min(lines.Length, osszIdx + 5); i++)
                {
                    var m = Regex.Match(lines[i], @"MEGTETT\s*ÚT\s*=\s*(\d+)\s*KM", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        osszKm = m.Groups[1].Value;
                        break;
                    }
                }
            }

            // 5) Sorok átvizsgálása: explicit "MEGTETT" és "KM" sorok
            foreach (var sor in lines)
            {
                if (HasonlotTartalmaz(sor, "MEGTETT", 2) || sor.Contains("MEGTETT"))
                {
                    var m = Regex.Match(sor, @"\b(\d{1,7})\b");
                    if (m.Success)
                    {
                        if (string.IsNullOrEmpty(napiKm))
                            napiKm = m.Groups[1].Value;
                        else if (string.IsNullOrEmpty(osszKm))
                            osszKm = m.Groups[1].Value;
                    }
                }
                else if (HasonlotTartalmaz(sor, "KM", 1) || sor.Contains("KM"))
                {
                    var m = Regex.Match(sor, @"\b(\d{1,7})\b");
                    if (m.Success)
                    {
                        if (string.IsNullOrEmpty(napiKm))
                            napiKm = m.Groups[1].Value;
                        else if (string.IsNullOrEmpty(osszKm))
                            osszKm = m.Groups[1].Value;
                    }
                }
            }

            // 6) Végső fallback: ha még hiányzik valamelyik, gyűjtsük össze az összes számot és heuristikusan osszuk szét
            if (string.IsNullOrEmpty(napiKm) || string.IsNullOrEmpty(osszKm))
            {
                var allNums = Regex.Matches(pre, @"\b(\d{1,7})\b")
                                   .Cast<Match>()
                                   .Select(m => m.Groups[1].Value)
                                   .Distinct()
                                   .Where(s => s != palyaszam && !s.StartsWith("202")) // kihagyjuk a pályaszámot és valószínű dátum-éveket
                                   .ToList();

                // gyakorlatban: napi = kisebb érték, összes = nagy (heurisztika)
                if (allNums.Count > 0 && string.IsNullOrEmpty(napiKm))
                {
                    int val = allNums.Select(int.Parse).OrderBy(x => x).First();
                    napiKm = val.ToString();
                    allNums.Remove(val.ToString());
                }
                if (allNums.Count > 0 && string.IsNullOrEmpty(osszKm))
                {
                    int val = allNums.Select(int.Parse).OrderByDescending(x => x).First();
                    osszKm = val.ToString();
                }
            }

            return (datum, palyaszam, napiKm, osszKm);
        }

        // ---------- segédfüggvények ----------
        private static bool HasonlotTartalmaz(string vonal, string cel, int maxTavolsag = 2)
        {
            if (string.IsNullOrWhiteSpace(vonal) || string.IsNullOrWhiteSpace(cel)) return false;
            IEnumerable<string> tokenek = Regex.Split(vonal, @"\W+").Where(t => !string.IsNullOrWhiteSpace(t));
            string normalCel = OsszehasonlitNormalizal(cel);
            foreach (string t in tokenek)
            {
                if (TavolsagSzamitas(OsszehasonlitNormalizal(t), normalCel) <= maxTavolsag)
                    return true;
            }
            return false;
        }

        private static string OsszehasonlitNormalizal(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // eltávolítjuk az ékezeteket és uppercase
            string form = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (var ch in form)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().ToUpperInvariant();
        }

        private static int TavolsagSzamitas(string a, string b)
        {
            //2 karakter között tavolas számítás (Levenshtein-távolság)
            if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[a.Length, b.Length];
        }

    }

}
