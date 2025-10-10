using KmKiolvasasMaui.Adatbazis;
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
            try
            {
                //  Ideiglenes adatok lekérdezése
                List<IdeiglenesAdat> lista = await _db.LekerdezesIdeiglenesAsync();
                if (lista.Count == 0)
                {
                    await DisplayAlert("Info", "Nincs elküldhető adat.", "OK");
                    return;
                }

                // CSV tartalom összeállítása
                StringBuilder csv = new();
                csv.AppendLine("Datum;Palyaszam;Napi_km;Ossz_km");
                foreach (IdeiglenesAdat a in lista)
                    csv.AppendLine($"{a.Datum:yyyy.MM.dd};{a.Palyaszam};{a.Napi_km};{a.Ossz_km}");

                // AUTOMATIKUS EMAIL KÜLDÉS
                await EmailKuld.KuldesCsvAsync(
                    //"pozsgaii@bkv.hu",
                    "bozaim@bkv.hu",
                    "Napi kiolvasott adatok",
                    csv.ToString()
                );

                // Siker esetén mentés a fő adatbázisba
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

                await _db.TorolIdeiglenesAsync();
                await DisplayAlert("Információ", "Az adatok automatikusan elküldve és mentve a fő adatbázisba.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", $"Email küldés sikertelen: {ex.Message}", "OK");
            }
        }
    }
}
