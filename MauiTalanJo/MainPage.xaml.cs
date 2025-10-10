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
            try
            {
                await Adatbazis.InicializalasAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"Adatbázis inicializálás sikertelen: {ex.Message}", "OK");
            }
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await OcrPlugin.Default.InitAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"OCR inicializálás sikertelen: {ex.Message}", "OK");
            }
        }

        private async void SelectBtn_Clicked(object sender, EventArgs e)
        {
            await KepFeldolgoz(async () => await MediaPicker.Default.PickPhotoAsync(), isCamera: false);
        }

        private async void PictureBtn_Clicked(object sender, EventArgs e)
        {
            // Régi MediaPicker verzió:
            // await KepFeldolgoz(async () => await MediaPicker.Default.CapturePhotoAsync());

            // Új CameraView-os megoldás
            await Navigation.PushAsync(new CameraPage());
        }
        public async Task InvokeKepFeldolgozAsync(string kepPath)
        {
            try
            {
                FileResult? fakeFile = new(kepPath);
                await KepFeldolgoz(() => Task.FromResult<FileResult?>(fakeFile), isCamera: true);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"A kép feldolgozása nem sikerült: {ex.Message}", "OK");
            }
        }

        private async Task KepFeldolgoz(Func<Task<FileResult?>> kepValasztVagyKeszit, bool isCamera)
        {
            try
            {
                FileResult? kepEredmeny = await kepValasztVagyKeszit();
                if (kepEredmeny == null)
                    return;

                using Stream imageAsStream = await kepEredmeny.OpenReadAsync();
                byte[] imageAsBytes = new byte[imageAsStream.Length];
                await imageAsStream.ReadAsync(imageAsBytes);
                OcrResult? ocrResult = await OcrPlugin.Default.RecognizeTextAsync(imageAsBytes, true);

                if (!ocrResult.Success)
                {
                    await DisplayAlert("Hiba", "Nem sikerült szöveget felismerni a képen.", "OK");
                    await UjraMegnyitasAsync(isCamera);
                    return;
                }

                var adatok = OcrAdatokKinyeres(ocrResult.AllText);

                // Ellenőrzés – minden mező megvan-e
                List<string> hianyok = [];
                if (string.IsNullOrWhiteSpace(adatok.datum)) hianyok.Add("Dátum");
                if (string.IsNullOrWhiteSpace(adatok.palyaszam)) hianyok.Add("Pályaszám");
                if (string.IsNullOrWhiteSpace(adatok.napiKm)) hianyok.Add("Napi km");
                if (string.IsNullOrWhiteSpace(adatok.osszKm)) hianyok.Add("Összes km");

                if (hianyok.Count != 0)
                {
                    string msg = "A következő adatok hiányoznak: " +
                                 string.Join(", ", hianyok) +
                                 "\nKérlek, próbáld újra!";
                    await DisplayAlert("Hiányzó adatok", msg, "OK");
                    await UjraMegnyitasAsync(isCamera);
                    return;
                }

                string osszegzes =
                    $"Dátum: {adatok.datum}\n" +
                    $"Pályaszám: {adatok.palyaszam}\n" +
                    $"Napi km: {adatok.napiKm}\n" +
                    $"Összes km: {adatok.osszKm}\n\n" +
                    "Szeretnéd ezeket az adatokat elmenteni az email küldéshez?";

                bool menteni = await DisplayAlert("Felismert adatok", osszegzes, "Mentés", "Elvetés");

                if (menteni)
                {
                    IdeiglenesAdat adat = new()
                    {
                        Datum = DateTime.Parse(adatok.datum),
                        Palyaszam = int.Parse(adatok.palyaszam),
                        Napi_km = int.Parse(adatok.napiKm),
                        Ossz_km = int.Parse(adatok.osszKm)
                    };

                    await Adatbazis.MentIdeiglenesAsync(adat);
                    await DisplayAlert("Információ", "Az adatok elmentve az email küldéshez.", "OK");
                }
                else
                {
                    await DisplayAlert("Információ", "Az adatok nem lettek elmentve.", "OK");
                    await UjraMegnyitasAsync(isCamera);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"Hiba történt a feldolgozás során: {ex.Message}", "OK");
                await UjraMegnyitasAsync(isCamera);
            }
        }

        private async Task UjraMegnyitasAsync(bool isCamera)
        {
            if (isCamera)
            {
                try
                {
                    if (Navigation.NavigationStack.Count > 1)
                        await Navigation.PopAsync();

                    await Task.Delay(10);
                    await Navigation.PushAsync(new CameraPage());
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hiba", $"A kamera újraindítása sikertelen: {ex.Message}", "OK");
                }
            }
            else
            {
                await Task.Delay(200);
                await KepFeldolgoz(async () => await MediaPicker.Default.PickPhotoAsync(), isCamera: false);
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

            Dictionary<string, string> fixes = new()
            {
                // km variánsok
                {"KLM", "KM"}, {"K1M", "KM"}, {"K I M", "KM"}, {"K|M", "KM"}, {"KIR", "KM"}, {"K1R","KM"},
                // összes / rosszul olvasott O hiány
                {"SSZES", "OSSZES"}, {"OSZES", "OSSZES"},
                // egyéb gyakori rövidítések/javítások
                {"MEGTET", "MEGTETT"}, {"MEGTETTIT", "MEGTETT"}, {"MEGTETT IT", "MEGTETT UT"}, {"MEGTETTUT", "MEGTETT UT"}
            };

            foreach (KeyValuePair<string, string> kv in fixes)
                pre = Regex.Replace(pre, @"\b" + Regex.Escape(kv.Key) + @"\b", kv.Value, RegexOptions.IgnoreCase);

            // sorokra bontás
            string[] lines = pre.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToArray();

            // 2) Dátum keresés (szigorú mintával: YYYY.MM.DD.)
            Match dm = Regex.Match(pre, @"\b\d{4}\.\d{2}\.\d{2}\.");
            if (dm.Success)
                datum = dm.Value.Trim();
            else datum = DateTime.Today.ToString("yyyy.MM.dd.");

            // 3) Pályaszám: első "tiszta" 3-5 jegyű szám a sorok között
            foreach (string sor in lines)
                {
                    // csak 4-essel kezdődő, 4 számjegyű számokat keresünk
                    Match p = Regex.Match(sor, @"\b(4\d{3})\b");
                    if (p.Success)
                    {
                        string talalt = p.Groups[1].Value;
                        palyaszam = talalt;
                        break;
                    }
                }

            // 4) Fejlécek keresése (NAPI, OSSZES)
            // 4) Napi / Összes km keresés konkrét mintával
            Match napiM = Regex.Match(pre, @"MEGTETT\s*ÚT\s*=\s*(\d+)\s*KM", RegexOptions.IgnoreCase);
            if (napiM.Success)
                napiKm = napiM.Groups[1].Value;

            int osszIdx = Array.FindIndex(lines, l => HasonlotTartalmaz(l, "OSSZES", 2));
            if (osszIdx >= 0)
            {
                for (int i = osszIdx; i < Math.Min(lines.Length, osszIdx + 5); i++)
                {
                    Match m = Regex.Match(lines[i], @"MEGTETT\s*ÚT\s*=\s*(\d+)\s*KM", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        osszKm = m.Groups[1].Value;
                        break;
                    }
                }
            }

            // 5) Sorok átvizsgálása: explicit "MEGTETT" és "KM" sorok
            foreach (string sor in lines)
            {
                if (HasonlotTartalmaz(sor, "MEGTETT", 2) || sor.Contains("MEGTETT"))
                {
                    Match m = Regex.Match(sor, @"\b(\d{1,7})\b");
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
                    Match m = Regex.Match(sor, @"\b(\d{1,7})\b");
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
                List<string> allNums = Regex.Matches(pre, @"\b(\d{1,7})\b")
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
            StringBuilder sb = new();
            foreach (char ch in form)
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
