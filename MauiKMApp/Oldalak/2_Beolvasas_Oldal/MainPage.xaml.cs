using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Kezelok;
using Plugin.Maui.OCR;
using System.Diagnostics;

namespace KmKiolvasasMaui
{
    public partial class MainPage : ContentPage
    {
        private readonly Adatbazis_Kezelo _adatbazis = new();

        private readonly Adat_User _felhasznalo;

        private bool _ocrInicializalva;

        private bool _inicializalasiHibaMarJelezve;

        public MainPage(Adat_User felhasznalo)
        {
            InitializeComponent();

            _felhasznalo = felhasznalo;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _adatbazis.InicializalasAsync();

                if (!_ocrInicializalva)
                {
                    await OcrPlugin.Default.InitAsync();

                    _ocrInicializalva = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Inicializálási hiba: {ex}");

                if (!_inicializalasiHibaMarJelezve)
                {
                    _inicializalasiHibaMarJelezve = true;

                    await DisplayAlert("Inicializálási hiba", "Az OCR vagy a helyi adatbázis inicializálása nem sikerült.", "OK");
                }
            }
        }

        private async void SelectBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                FileResult? file = await MediaPicker.Default.PickPhotoAsync();

                if (file == null)
                    return;

                await KepFeldolgozAsync(file, automatikusKamera: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Kép kiválasztási hiba: {ex}");

                await DisplayAlert("Hiba", "A kép kiválasztása vagy feldolgozása nem sikerült.", "OK");
            }
        }

        private async void PictureBtn_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CameraPage());
        }

        public async Task<bool> InvokeKepFeldolgozAutomatikusAsync(string kepPath)
        {
            try
            {
                FileResult file = new(kepPath);

                return await KepFeldolgozAsync(file, automatikusKamera: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Automatikus OCR hiba: {ex}");

                return false;
            }
        }

        private async Task<bool> KepFeldolgozAsync(FileResult kepEredmeny, bool automatikusKamera)
        {
            try
            {
                using Stream stream = await kepEredmeny.OpenReadAsync();

                using MemoryStream memory = new();

                await stream.CopyToAsync(memory);

                byte[] imageBytes = memory.ToArray();

                OcrKinyertAdat? eredmeny = await OcrFeldolgozo.FelismeresAsync(imageBytes);

                if (eredmeny == null)
                {
                    if (!automatikusKamera)
                        await DisplayAlert("OCR", "A képből nem sikerült szöveget felismerni.", "OK");

                    return false;
                }

                Debug.WriteLine($"OCR forrás: {eredmeny.FelismeresiForras}");

                Debug.WriteLine($"OCR idő: {eredmeny.FeldolgozasiIdoMs} ms");

                Debug.WriteLine($"OCR pont: {eredmeny.MegbizhatosagiPont}");

                Debug.WriteLine($"Bizonytalan: {eredmeny.Bizonytalan}");

                // Nem találunk ki hiányzó adatot.
                if (!eredmeny.Teljes)
                {
                    Debug.WriteLine($"Hiányzó mezők: {eredmeny.HianyzoMezokSzovege()}");

                    if (!automatikusKamera)
                        await DisplayAlert("Hiányos felismerés", "Nem sikerült minden szükséges adatot felismerni.\n\nHiányzó adatok: {eredmeny.HianyzoMezokSzovege()}", "OK");

                    return false;
                }

                int palyaszam = eredmeny.Palyaszam!.Value;

                int napiKm = eredmeny.NapiKm!.Value;

                int osszKm = eredmeny.OsszKm!.Value;

                DateTime datum = eredmeny.Datum!.Value;

                // Mindkét OCR út ugyanazt a
                // telephely-meghatározást használja.
                string telephely = await _adatbazis.TelephelyKeresesAsync(palyaszam);

                bool duplikatum = await _adatbazis.EllenorizDuplikatumAsync(datum, palyaszam, napiKm, osszKm);

                if (duplikatum)
                {
                    await DisplayAlert("Már rögzített adat", $"A(z) {palyaszam} pályaszámhoz ugyanez az adat már szerepel {datum:yyyy.MM.dd}. napra.", "OK");
                    return true;
                }

                string figyelmeztetes = "";

                if (eredmeny.Figyelmeztetesek.Count > 0)
                    figyelmeztetes = $"\n\nFigyelmeztetés:\n-{string.Join("\n- ", eredmeny.Figyelmeztetesek)}";

                string osszegzes =
                    $"Dátum: {datum:yyyy.MM.dd}.\n" +
                    $"Pályaszám: {palyaszam}\n" +
                    $"Napi km: {napiKm}\n" +
                    $"Összes km: {osszKm}\n" +
                    $"OCR: {eredmeny.FelismeresiForras}\n" +
                    $"Feldolgozási idő: {eredmeny.FeldolgozasiIdoMs} ms" +
                    figyelmeztetes +
                    "\n\nEllenőrizted az adatokat?";

                string cim = eredmeny.Bizonytalan ? "Bizonytalan OCR-eredmény" : "Felismert adatok";

                bool menteni = await DisplayAlert(cim, osszegzes, "Mentés", "Elvetés");

                if (!menteni)
                    return false;

                IdeiglenesAdat adat = new()
                {
                    ReadingId = Guid.NewGuid().ToString("N"),
                    Datum = datum,
                    Palyaszam = palyaszam,
                    Napi_km = napiKm,
                    Ossz_km = osszKm,
                    DolgozoSzam = _felhasznalo.DolgozoSzam,
                    DolgozoNev = _felhasznalo.FelhasznaloNev,
                    Szervezet = _felhasznalo.Szervezet,
                    Telephely = telephely,
                    SyncStatus = SyncAllapot.Pending,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _adatbazis.MentIdeiglenesAsync(adat);

                if (!automatikusKamera)
                    await DisplayAlert("Mentés", "Az adat helyben elmentésre került.", "OK");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine( $"Képfeldolgozási hiba: {ex}");

                if (!automatikusKamera)
                    await DisplayAlert("Hiba", "A kép feldolgozása nem sikerült.", "OK");

                return false;
            }
        }
    }
}