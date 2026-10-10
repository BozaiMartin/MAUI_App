using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Email;
using System.Diagnostics;
using System.Text;

namespace KmKiolvasasMaui
{
    public partial class EmailOldal : ContentPage
    {
        private readonly Adatbazis_Kezelo _db = new();

        private readonly Adat_User _felhasznalo;

        public EmailOldal(Adat_User felhasznalo)
        {
            InitializeComponent();

            _felhasznalo = felhasznalo;
        }

        private async void Urites_Clicked(object sender, EventArgs e)
        {
            bool megerosites = await DisplayAlert("Megerősítés", "Biztosan üríteni szeretnéd a kiolvasott adatok táblát?", "Igen, töröld", "Mégsem");

            if (!megerosites)
                return;

            try
            {
                await _db.TorolKiolvasottAdatokatAsync();

                await DisplayAlert("Információ", "Az adatbázis sikeresen ürítve lett.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Adatbázis ürítési hiba: {ex}");

                await DisplayAlert("Hiba", "Az adatbázis ürítése nem sikerült.", "OK");
            }
        }

        private async void MutatIdeiglenes_Clicked(object sender, EventArgs e)
        {
            List<IdeiglenesAdat> lista = await _db.LekerdezesIdeiglenesAsync();

            if (lista.Count == 0)
            {
                await DisplayAlert("Info", "Nincs ideiglenes adat.", "OK");
                return;
            }

            string tartalom = string.Join("\n\n",
                lista.Select(
                    adat =>
                    $"Dátum: {adat.Datum:yyyy.MM.dd}" +
                    $"\nPályaszám: {adat.Palyaszam}" +
                    $"\nTelephely: {adat.Telephely}" +
                    $"\nDolgozó: {adat.DolgozoNev}" +
                    $"\nDolgozószám: {adat.DolgozoSzam}" +
                    $"\nNapi km: {adat.Napi_km}" +
                    $"\nÖsszes km: {adat.Ossz_km}" +
                    $"\nÁllapot: {adat.SyncStatus}" +
                    $"\nReadingId: {adat.ReadingId}"
                    ));

            await DisplayAlert("Ideiglenes adatok", tartalom, "OK");
        }

        private async void Kuldes_Clicked(object sender, EventArgs e)
        {
            Button? kuldesGomb = sender as Button;

            try
            {
                if (kuldesGomb != null)
                    kuldesGomb.IsEnabled = false;

                NetworkAccess current = Connectivity.Current.NetworkAccess;

                if (current != NetworkAccess.Internet)
                {
                    await DisplayAlert("Nincs internetkapcsolat", "Az adatok helyben megmaradnak. Később újra elküldhetők.", "OK");
                    return;
                }

                int osszesElkuldott = 0;
                int feldolgozottCsomag = 0;

                while (true)
                {
                    (string? batchId,List<IdeiglenesAdat> lista) = await _db.SyncCsomagEloKeszitesAsync();

                    if (string.IsNullOrWhiteSpace(batchId) || lista.Count == 0)
                        break;

                    feldolgozottCsomag++;

                    StringBuilder csv = new();

                    csv.AppendLine("Datum;Telephely;DolgozoSzam;DolgozoNev;Szervezet;Palyaszam;Napi_km;Ossz_km");

                    foreach (IdeiglenesAdat adat in lista)
                    {
                        csv.AppendLine(
                            $"{adat.Datum:yyyy.MM.dd};" +
                            $"{CsvMezo(adat.Telephely)};" +
                            $"{CsvMezo(adat.DolgozoSzam)};" +
                            $"{CsvMezo(adat.DolgozoNev)};" +
                            $"{CsvMezo(adat.Szervezet)};" +
                            $"{adat.Palyaszam};" +
                            $"{adat.Napi_km};" +
                            $"{adat.Ossz_km}");
                    }

                    string userName = $"{_felhasznalo.FelhasznaloNev} ({_felhasznalo.DolgozoSzam})";

                    try
                    {
                        bool marElkuldve = await EmailKuld.KuldesCsvAsync(requestId: batchId, userName: userName, csvTartalom: csv.ToString());

                        await _db.SyncCsomagSikeresAsync(batchId);

                        osszesElkuldott += lista.Count;

                        Debug.WriteLine(marElkuldve ? $"A(z) {batchId} csomag már korábban elküldésre került.": $"A(z) {batchId} csomag sikeresen elküldve.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Csomagküldési hiba. BatchId: {batchId}, Hiba: {ex}");

                        try
                        {
                            await _db.SyncCsomagSikertelenAsync(batchId);
                        }
                        catch (Exception statusEx)
                        {
                            Debug.WriteLine($"Failed státusz beállítási hiba: {statusEx}");
                        }

                        await DisplayAlert(
                            "Küldési hiba",
                            $"Az adatok küldése nem sikerült.\n\n" +
                            $"Hiba: {ex.Message}\n\n" +
                            "A rekordok helyben megmaradtak.",
                            "OK");

                        return;
                    }

                    if (feldolgozottCsomag >= 20)
                    {
                        Debug.WriteLine("A szinkronizáció elérte a 20 csomagos biztonsági korlátot.");

                        break;
                    }
                }

                if (osszesElkuldott == 0)
                {
                    await DisplayAlert("Információ", "Nincs elküldhető adat.", "OK");
                    return;
                }

                await DisplayAlert("Siker", $"{osszesElkuldott} mérési rekord sikeresen feldolgozásra került.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Általános küldési hiba: {ex}");

                await DisplayAlert("Hiba", $"Az adatok küldése nem sikerült.\n\n Hiba: {ex.Message}", "OK");
            }
            finally
            {
                if (kuldesGomb != null)
                    kuldesGomb.IsEnabled = true;
            }
        }

        private async Task BiztonsagosanSikertelenreAllitAsync(string batchId)
        {
            try
            {
                await _db.SyncCsomagSikertelenAsync(batchId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sync státusz hiba: {ex}");
            }
        }

        private static string CsvMezo(string? value)
        {
            string szoveg = value ?? "";

            if (szoveg.Contains(';') || szoveg.Contains('"') || szoveg.Contains('\n') || szoveg.Contains('\r'))
            {
                return $"\"{szoveg.Replace("\"", "\"\"")}\"";
            }

            return szoveg;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _db.InicializalasAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Adatbázis inicializálási hiba: {ex}");

                await DisplayAlert("Adatbázis hiba", "A helyi adatbázis inicializálása nem sikerült.", "OK");
            }
        }
    }
}