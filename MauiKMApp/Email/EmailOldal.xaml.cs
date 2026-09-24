using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Email;
using System.Security.Cryptography;
using System.Text;

namespace KmKiolvasasMaui
{
    public partial class EmailOldal : ContentPage
    {
        private readonly Adatbazis_Kezelo _db = new();

        public EmailOldal()
        {
            InitializeComponent();
        }
        private async void Urites_Clicked(object sender, EventArgs e)
        {
            bool megerosites = await DisplayAlert(
                "Megerősítés",
                "Biztosan üríteni szeretnéd a kiolvasott adatok táblát? Az adatok végleg törlődnek, de az adatbázis fájl megmarad.",
                "Igen, töröld",
                "Mégsem");

            if (!megerosites)
                return;

            try
            {
                await _db.TorolKiolvasottAdatokatAsync();
                await DisplayAlert("Információ", "Az adatábzis sikeresen ürítve lett.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"Az adatbázis ürítése nem sikerült: {ex.Message}", "OK");
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

            string tartalom = string.Join("\n\n", lista.Select(a =>
                $"Dátum: {a.Datum:yyyy.MM.dd}\nPályaszám: {a.Palyaszam}\nNapi km: {a.Napi_km}\nÖsszes km: {a.Ossz_km}"));

            await DisplayAlert("Ideiglenes adatok", tartalom, "OK");
        }
        private async void Kuldes_Clicked(object sender, EventArgs e)
        {
            Button? kuldesGomb = sender as Button;

            try
            {
                // Ne lehessen küldés közben újra megnyomni.
                if (kuldesGomb != null)
                    kuldesGomb.IsEnabled = false;


                // Internet ellenőrzés
                NetworkAccess current =
                    Connectivity.Current.NetworkAccess;

                if (current != NetworkAccess.Internet)
                {
                    await DisplayAlert(
                        "Hiba",
                        "Nincs internetkapcsolat! Az adatok küldéséhez csatlakozz hálózathoz.",
                        "OK");

                    return;
                }


                // Ideiglenes adatok lekérdezése
                List<IdeiglenesAdat> lista =
                    await _db.LekerdezesIdeiglenesAsync();

                if (lista.Count == 0)
                {
                    await DisplayAlert(
                        "Információ",
                        "Nincs elküldhető adat.",
                        "OK");

                    return;
                }
                
                // CSV összeállítása
                
                StringBuilder csv = new();

                csv.AppendLine(
                    "Datum;Palyaszam;Napi_km;Ossz_km");

                foreach (IdeiglenesAdat a in lista)
                {
                    csv.AppendLine(
                        $"{a.Datum:yyyy.MM.dd};" +
                        $"{a.Palyaszam};" +
                        $"{a.Napi_km};" +
                        $"{a.Ossz_km}");
                }

                string csvTartalom =
                    csv.ToString();

                // STABIL REQUEST ID
                

                byte[] hashBytes =
                    SHA256.HashData(
                        Encoding.UTF8.GetBytes(csvTartalom));

                string requestId =
                    Convert.ToHexString(hashBytes)
                        .ToLowerInvariant();

                // FELHASZNÁLÓ
                
                string userName =
                    "Teszt felhasználó";
                
                // EMAIL KÜLDÉS

                await EmailKuld.KuldesCsvAsync(
                    requestId: requestId,
                    userName: userName,
                    csvTartalom: csvTartalom
                );
                
                // SIKERES KÜLDÉS UTÁN
                
                foreach (IdeiglenesAdat a in lista)
                {
                    KiolvasottAdat vegleges = new()
                    {
                        Datum = a.Datum,
                        Palyaszam = a.Palyaszam,
                        Napi_km = a.Napi_km,
                        Ossz_km = a.Ossz_km,
                        Email_kuldve = true
                    };

                    await _db.MentesAsync(vegleges);
                }

                // Csak akkor töröljük az ideiglenes adatokat,
                // ha az e-mail küldése sikerült.
                await _db.TorolIdeiglenesAsync();


                await DisplayAlert(
                    "Siker",
                    "Az adatok sikeresen elküldve és mentve.",
                    "OK");
            }
            catch (HttpRequestException ex)
            {
                await DisplayAlert(
                    "Hálózati hiba",
                    "Az e-mail küldése nem sikerült.\n\n" +
                    "Az adatok nem vesztek el, továbbra is az ideiglenes adatbázisban vannak.\n\n" +
                    $"Hiba: {ex.Message}",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Hiba",
                    "Az e-mail küldése nem sikerült.\n\n" +
                    "Az adatok nem vesztek el.\n\n" +
                    $"Hiba: {ex.Message}",
                    "OK");
            }
            finally
            {
                if (kuldesGomb != null)
                    kuldesGomb.IsEnabled = true;
            }

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
                await DisplayAlert(
                    "Adatbázis hiba",
                    ex.Message,
                    "OK");
            }
        }
        private async void TesztAdat_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Biztosítjuk, hogy a táblák létezzenek.
                await _db.InicializalasAsync();

                IdeiglenesAdat teszt = new()
                {
                    Datum = DateTime.Now,
                    Palyaszam = 1143,
                    Napi_km = 125,
                    Ossz_km = 123456
                };

                await _db.MentIdeiglenesAsync(teszt);

                await DisplayAlert(
                    "Siker",
                    "Tesztadat bekerült az ideiglenes adatbázisba.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert(
                    "Hiba",
                    ex.Message,
                    "OK");
            }
        }
    }
}
