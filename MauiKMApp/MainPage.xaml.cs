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
            _ = MainPage.Inicializal();
        }

        private static async Task Inicializal()
        {
            try
            {
                await Adatbazis.InicializalasAsync();
            }
            catch
            {
                // csendes hiba
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await OcrPlugin.Default.InitAsync();
            }
            catch
            {
                // nincs üzenet, ha az OCR init nem sikerül
            }
        }

        private async void SelectBtn_Clicked(object sender, EventArgs e)
        {
            await MainPage.KepFeldolgoz(async () => await MediaPicker.Default.PickPhotoAsync(), isCamera: false);
        }

        private async void PictureBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }

        public static async Task InvokeKepFeldolgozAsync(string kepPath)
        {
            try
            {
                FileResult? fakeFile = new(kepPath);
                await MainPage.KepFeldolgoz(() => Task.FromResult<FileResult?>(fakeFile), isCamera: true);
            }
            catch
            {
                // nem jelez semmit
            }
        }

        private static async Task KepFeldolgoz(Func<Task<FileResult?>> kepValasztVagyKeszit, bool isCamera)
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
                if (ocrResult == null || !ocrResult.Success || string.IsNullOrWhiteSpace(ocrResult.AllText))
                    return; // nincs adat -> csendben kilép

                var (datum, palyaszam, napiKm, osszKm) = MainPage.OcrAdatokKinyeres(ocrResult.AllText);

                // ha nincs minden adat, akkor nem mentünk, csak kilépünk
                if (string.IsNullOrWhiteSpace(datum) ||
                    string.IsNullOrWhiteSpace(palyaszam) ||
                    string.IsNullOrWhiteSpace(napiKm) ||
                    string.IsNullOrWhiteSpace(osszKm))
                    return;

                IdeiglenesAdat adat = new()
                {
                    Datum = DateTime.Parse(datum),
                    Palyaszam = int.Parse(palyaszam),
                    Napi_km = int.Parse(napiKm),
                    Ossz_km = int.Parse(osszKm)
                };

                bool duplikatum = await Adatbazis.EllenorizDuplikatumAsync(
                    adat.Datum,
                    adat.Palyaszam,
                    adat.Napi_km,
                    adat.Ossz_km);

                if (duplikatum)
                    return; // már létezett -> nem mentjük újra

                await Adatbazis.MentIdeiglenesAsync(adat);
            }
            catch
            {
                // bármilyen hiba esetén csendben visszalép
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
                catch
                {
                    // csendes
                }
            }
            else
            {
                await Task.Delay(200);
                await MainPage.KepFeldolgoz(async () => await MediaPicker.Default.PickPhotoAsync(), isCamera: false);
            }
        }

        // ----------- OCR adatkinyerés -----------
        private static (string datum, string palyaszam, string napiKm, string osszKm) OcrAdatokKinyeres(string ocrText)
        {
            string datum = "";
            string palyaszam = "";
            string napiKm = "";
            string osszKm = "";

            if (string.IsNullOrWhiteSpace(ocrText))
                return (datum, palyaszam, napiKm, osszKm);

            string pre = ocrText.ToUpperInvariant();
            Dictionary<string, string> fixes = new()
            {
                {"KLM", "KM"}, {"K1M", "KM"}, {"K I M", "KM"}, {"K|M", "KM"}, {"KIR", "KM"}, {"K1R","KM"},
                {"SSZES", "OSSZES"}, {"OSZES", "OSSZES"},
                {"MEGTET", "MEGTETT"}, {"MEGTETTIT", "MEGTETT"}, {"MEGTETT IT", "MEGTETT UT"}, {"MEGTETTUT", "MEGTETT UT"}
            };
            foreach (var kv in fixes)
                pre = Regex.Replace(pre, @"\b" + Regex.Escape(kv.Key) + @"\b", kv.Value, RegexOptions.IgnoreCase);

            string[] lines = [.. pre.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l))];

            Match dm = Regex.Match(pre, @"\b\d{4}\.\d{2}\.\d{2}\.");
            if (dm.Success)
                datum = dm.Value.Trim();
            else datum = DateTime.Today.ToString("yyyy.MM.dd.");

            foreach (string sor in lines)
            {
                Match p = Regex.Match(sor, @"\b(4\d{3})\b");
                if (p.Success)
                {
                    palyaszam = p.Groups[1].Value;
                    break;
                }
            }

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

            if (string.IsNullOrEmpty(napiKm) || string.IsNullOrEmpty(osszKm))
            {
                List<string> allNums = [.. Regex.Matches(pre, @"\b(\d{1,7})\b")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .Distinct()
                    .Where(s => s != palyaszam && !s.StartsWith("202"))];

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

        private static bool HasonlotTartalmaz(string vonal, string cel, int maxTavolsag = 2)
        {
            if (string.IsNullOrWhiteSpace(vonal) || string.IsNullOrWhiteSpace(cel)) return false;
            var tokenek = Regex.Split(vonal, @"\W+").Where(t => !string.IsNullOrWhiteSpace(t));
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
        public async Task<bool> InvokeKepFeldolgozAutomatikusAsync(string kepPath)
        {
            try
            {
                FileResult fakeFile = new(kepPath);
                return await KepFeldolgozAutomatikus(() => Task.FromResult<FileResult?>(fakeFile));
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> KepFeldolgozAutomatikus(Func<Task<FileResult?>> kepValasztVagyKeszit)
        {
            try
            {
                FileResult? kepEredmeny = await kepValasztVagyKeszit();
                if (kepEredmeny == null)
                    return false;

                using Stream imageAsStream = await kepEredmeny.OpenReadAsync();
                byte[] imageAsBytes = new byte[imageAsStream.Length];
                await imageAsStream.ReadAsync(imageAsBytes);

                OcrResult? ocrResult = await OcrPlugin.Default.RecognizeTextAsync(imageAsBytes, true);
                if (ocrResult == null || !ocrResult.Success || string.IsNullOrWhiteSpace(ocrResult.AllText))
                    return false;

                var (datum, palyaszam, napiKm, osszKm) = MainPage.OcrAdatokKinyeres(ocrResult.AllText);

                // ha bármelyik adat hiányzik, újrafotózás
                if (string.IsNullOrWhiteSpace(datum) ||
                    string.IsNullOrWhiteSpace(palyaszam) ||
                    string.IsNullOrWhiteSpace(napiKm) ||
                    string.IsNullOrWhiteSpace(osszKm))
                    return false;

                // Adatok konvertálása
                IdeiglenesAdat adat = new()
                {
                    Datum = DateTime.Parse(datum),
                    Palyaszam = int.Parse(palyaszam),
                    Napi_km = int.Parse(napiKm),
                    Ossz_km = int.Parse(osszKm)
                };

                // Duplikátum ellenőrzés
                bool duplikatum = await Adatbazis.EllenorizDuplikatumAsync(
                    adat.Datum, adat.Palyaszam, adat.Napi_km, adat.Ossz_km);

                if (duplikatum)
                {
                    await DisplayAlert(
                        "Figyelmeztetés",
                        $"A(z) {adat.Palyaszam} pályaszámhoz már létezik adat {adat.Datum:yyyy.MM.dd}-én.",
                        "OK");
                    return true; // sikeres OCR, de már létező adat -> kilép
                }

                // Összegzés megjelenítése
                string osszegzes =
                    $"Dátum: {datum}\n" +
                    $"Pályaszám: {palyaszam}\n" +
                    $"Napi km: {napiKm}\n" +
                    $"Összes km: {osszKm}\n\n" +
                    "Szeretnéd ezeket az adatokat elmenteni az email küldéshez?";

                bool menteni = await DisplayAlert("Felismert adatok", osszegzes, "Mentés", "Elvetés");

                if (menteni)
                {
                    await Adatbazis.MentIdeiglenesAsync(adat);
                    return true; // sikeres feldolgozás
                }

                return false; // elvetette -> újrafotózás
            }
            catch
            {
                return false; // hiba -> újrafotózás
            }
        }


    }
}
