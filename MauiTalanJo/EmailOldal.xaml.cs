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
            var lista = await _db.LekerdezesIdeiglenesAsync();
            if (lista.Count == 0) { await DisplayAlert("Info", "Nincs elküldhető adat.", "OK"); return; }
            // CSV készítés
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Datum;Palyaszam;Napi_km;Ossz_km");
            foreach (var a in lista)
                csv.AppendLine($"{a.Datum:yyyy.MM.dd};{a.Palyaszam};{a.Napi_km};{a.Ossz_km}");
            // Email küldés (egyszerűsítve, csatolmány helyett szövegben)
            await EmailKuld.KuldesAsync("bozaim@bkv.hu", "Napi kiolvasott adatok", csv.ToString());
            // Ha sikeres → áthelyezés a fő adatbázisba
            foreach (var a in lista)
            {
                KiolvasottAdat vegleges = new KiolvasottAdat { Datum = a.Datum, Palyaszam = a.Palyaszam, Napi_km = a.Napi_km, Ossz_km = a.Ossz_km, Email_kuldve = true };
                await _db.MentesAsync(vegleges);
            }
            // Ideiglenes törlése
            await _db.TorolIdeiglenesAsync();
            await DisplayAlert("Siker", "Az adatok elküldve és mentve a fő adatbázisba.", "OK");
        }

        //Legkozelebb megnezem itt az a baj hogy nincs elvileg telepitve gmail vagy outlook

        //private async void Kuldes_Clicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        // 1) Ideiglenes adatok lekérdezése
        //        List<IdeiglenesAdat> lista = await _db.LekerdezesIdeiglenesAsync();
        //        if (lista.Count == 0)
        //        {
        //            await DisplayAlert("Info", "Nincs elküldhető adat.", "OK");
        //            return;
        //        }

        //        // 2) CSV készítés
        //        StringBuilder csv = new();
        //        csv.AppendLine("Datum;Palyaszam;Napi_km;Ossz_km");
        //        foreach (var a in lista)
        //            csv.AppendLine($"{a.Datum:yyyy.MM.dd};{a.Palyaszam};{a.Napi_km};{a.Ossz_km}");

        //        // 3) Ideiglenes fájl létrehozása az AppDataDirectory-ban
        //        string fileName = $"adatok_{DateTime.Now:yyyyMMdd_HHmm}.csv";
        //        string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
        //        File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);

        //        // 4) Email üzenet összeállítása
        //        var message = new EmailMessage
        //        {
        //            Subject = "Napi kiolvasott adatok",
        //            Body = "Csatolva találod a napi kiolvasott adatokat.",
        //            To = new List<string> { "bozaim@bkv.hu", "bozaimartin@gmail.com" }
        //        };

        //        var file = new EmailAttachment(filePath, "text/csv");
        //        message.Attachments.Add(file);

        //        // 5) Email kliens megnyitása
        //        await Email.ComposeAsync(message);

        //        // 6) Ha sikeres → áthelyezés a fő adatbázisba
        //        foreach (var a in lista)
        //        {
        //            KiolvasottAdat vegleges = new()
        //            {
        //                Datum = a.Datum,
        //                Palyaszam = a.Palyaszam,
        //                Napi_km = a.Napi_km,
        //                Ossz_km = a.Ossz_km,
        //                Email_kuldve = true
        //            };
        //            await _db.MentesAsync(vegleges);
        //        }

        //        // 7) Ideiglenes adatok törlése
        //        await _db.TorolIdeiglenesAsync();

        //        await DisplayAlert("Siker", "Az adatok elküldve és mentve a fő adatbázisba.", "OK");
        //    }
        //    catch (FeatureNotSupportedException)
        //    {
        //        // Ha nincs email kliens telepítve
        //        await DisplayAlert("Hiba", "Az email küldés ezen az eszközön nem támogatott. Telepíts Gmail vagy Outlook alkalmazást.", "OK");
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Hiba", $"Email küldés sikertelen: {ex.Message}", "OK");
        //    }
        //}

    }
}
