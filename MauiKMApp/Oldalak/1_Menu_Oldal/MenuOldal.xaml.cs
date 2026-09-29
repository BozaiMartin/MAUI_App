using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Kezelok;
namespace KmKiolvasasMaui
{
    public partial class MenuOldal : ContentPage
    {
        public MenuOldal()
        {
            InitializeComponent();
        }

        private async void NavigalMainPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage());
        }

        private async void NavigalEmailOldal(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EmailOldal());
        }

        private async void NavigalAdatokOldal(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdatokOldal());
        }

        private async void AdatbazisTeszt_Clicked(object sender, EventArgs e)
        {
#if DEBUG
            try
            {
                SQL_Kezelo_Bejelentkezes belepes = new();

                SQL_Kezelo_Dolgozo dolgozo = new();

                // Adatbázisok + táblák létrehozása
                await belepes.InicializalasAsync();
                await dolgozo.InicializalasAsync();

                // Adatbázis elérési utak
                string belepesPath = Path.Combine(FileSystem.AppDataDirectory, "Bejelentkezes.db");

                string alapPath = Path.Combine(FileSystem.AppDataDirectory, "Alapadatok.db");

                // SQLCipher verzió ellenõrzése
                string cipher = await SqliteKapcsolat.CipherVerzioAsync("Bejelentkezes.db");

                // Fejlesztõi adatbázis-kulcs lekérése
                string belepesKulcs = await AdatbazisKulcsKezelo.FejlesztoiKulcsAsync("Bejelentkezes.db");

                await DisplayAlert(
                    "Adatbázis teszt",

                    $"Bejelentkezés DB: " +
                    $"{File.Exists(belepesPath)}\n" +

                    $"Alapadatok DB: " +
                    $"{File.Exists(alapPath)}\n\n" +

                    $"SQLCipher: {cipher}\n\n" +

                    $"Bejelentkezés DB kulcs:\n" +
                    $"{belepesKulcs}",

                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", ex.ToString(), "OK");
            }

#else
            await DisplayAlert("Információ", "Az adatbázis teszt csak fejlesztõi módban érhetõ el.", "OK");

#endif
        }

        private async void BelepesTeszt_Clicked(object sender, EventArgs e)
        {
#if DEBUG
            try
            {
                const string dolgozoSzam = "123456";
                const string tesztJelszo = "Teszt1234";

                // 1. Adatbázis-kezelõk
                
                SQL_Kezelo_Bejelentkezes bejelentkezesDb = new();

                SQL_Kezelo_Dolgozo dolgozoDb = new();

                await bejelentkezesDb.InicializalasAsync();
                await dolgozoDb.InicializalasAsync();

                // 2. Dolgozó létrehozása az Alapadatok.db-ben
                
                bool dolgozoLetezik = await dolgozoDb.DolgozoLetezikAsync(dolgozoSzam);

                if (!dolgozoLetezik)
                {
                    Adat_Dolgozo dolgozo = new(dolgozoSzam: dolgozoSzam, dolgozoNev: "Teszt Felhasználó", szervezet: "Teszt", status: 0);

                    await dolgozoDb.RogzitesAsync(dolgozo);
                }

                // 3. User ellenõrzése
                
                Adat_User? meglevoUser = await SQL_Kezelo_Bejelentkezes.FelhasznaloKeresesAsync(dolgozoSzam);

                // 4. Ha nincs user, létrehozzuk
                
                if (meglevoUser == null)
                {
                    var (Hash, Salt) = JelszoKezelo.HashKeszites(tesztJelszo);

                    Adat_User user = new(userId: 0,
                        felhasznaloNev: "Teszt Felhasználó",
                        dolgozoSzam: dolgozoSzam,
                        jelszoHash: Hash,
                        jelszoSalt: Salt,
                        jelszoLejarat:JelszoKezelo.UjLejarat(90),

                            // Most 0, hogy rögtön be tudjunk lépni.
                            frissit: 0,

                            szervezet: "Teszt",

                            // Teszt admin
                            admin: 1
                        );

                    await bejelentkezesDb.RogzitesAsync(user);
                }

                // 5. Teljes belépési folyamat tesztelése
                
                BejelentkezesSzolgaltatas szolgaltatas = new();

                BejelentkezesEredmeny eredmeny = await szolgaltatas.BejelentkezesAsync(dolgozoSzam, tesztJelszo);

                // 6. Eredmény
                
                switch (eredmeny.Tipus)
                {
                    case BejelentkezesEredmenyTipus.Sikeres:

                        await DisplayAlert(
                            "TESZT SIKERES",
                            $"Belépett:\n" +
                            $"{eredmeny.Felhasznalo?.FelhasznaloNev}\n\n" +
                            $"Dolgozószám: {eredmeny.Felhasznalo?.DolgozoSzam}\n" +
                            $"Szervezet: {eredmeny.Felhasznalo?.Szervezet}\n" +
                            $"Admin: {eredmeny.Felhasznalo?.Admin}",
                            "OK");

                        break;

                    case BejelentkezesEredmenyTipus.JelszoCsereSzukseges:

                        await DisplayAlert("TESZT", "A jelszócsere kötelezõ.", "OK");
                        break;


                    case BejelentkezesEredmenyTipus.InaktivDolgozo:

                        await DisplayAlert("TESZT", "A dolgozó inaktív.", "OK");
                        break;


                    case BejelentkezesEredmenyTipus.HibasAdatok:

                        await DisplayAlert("TESZT HIBA", "Hibás dolgozószám vagy jelszó.", "OK");
                        break;


                    default:

                        await DisplayAlert("TESZT HIBA", $"Ismeretlen eredmény: {eredmeny.Tipus}", "OK");
                        break;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("TESZT HIBA", ex.ToString(), "OK");
            }
#else
    await DisplayAlert("Információ", "Ez a funkció csak Debug módban érhetõ el.", "OK");
#endif
        }

        private async void JelszoCsereTeszt_Clicked(object sender, EventArgs e)
        {
#if DEBUG
            try
            {
                const string dolgozoSzam = "123456";

                SQL_Kezelo_Bejelentkezes db = new();

                await db.InicializalasAsync();

                Adat_User? user = await SQL_Kezelo_Bejelentkezes.FelhasznaloKeresesAsync(dolgozoSzam);

                if (user == null)
                {
                    await DisplayAlert("Teszt", "A tesztfelhasználó nem található.", "OK");
                    return;
                }

                // Következõ belépéskor kötelezõ jelszócsere
                user.FrissitesKotelezo();

                await db.ModositasAsync(user);

                await DisplayAlert("Teszt", "Frissit = 1 beállítva. Most újra be kell jelentkezni.", "OK");

                Application.Current!.MainPage = new NavigationPage(new BejelentkezesOldal());
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hiba", ex.ToString(), "OK");
            }
#else
    await DisplayAlert("Információ", "Ez csak Debug módban használható.", "OK");
        "OK");
#endif
        }
    }
}