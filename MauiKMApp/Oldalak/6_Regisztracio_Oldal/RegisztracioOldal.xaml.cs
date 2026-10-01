using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Kezelok;

namespace KmKiolvasasMaui
{
    public partial class RegisztracioOldal : ContentPage
    {
        private RegisztraciosAdat? _regisztraciosAdat;

        private bool _regisztracioFolyamatban;

        public RegisztracioOldal()
        {
            InitializeComponent();
        }

        private async void QrBeolvasasButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                PermissionStatus status =
                    await Permissions.CheckStatusAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert(
                        "Kameraengedély",
                        "A QR-kód beolvasásához engedélyezni kell a kamerát.",
                        "OK");

                    return;
                }

                await Navigation.PushAsync(new QrBeolvasoOldal(qrTartalom => 
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        QrFeldolgozas(qrTartalom);
                    });
                },
                true));
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert("QR hiba", ex.ToString(), "OK");
#else
                await DisplayAlert(
                    "QR hiba",
                    "A QR-kód beolvasása nem indítható el.",
                    "OK");
#endif
            }
        }

        private void QrFeldolgozas(string qrTartalom)
        {
            bool ervenyes =
                RegisztraciosQrKezelo.Ellenorzes(
                    qrTartalom,
                    out RegisztraciosAdat? adat,
                    out string hiba);

            if (!ervenyes || adat == null)
            {
                _regisztraciosAdat = null;

                AdatokLayout.IsVisible = false;

                RegisztracioButton.IsEnabled = false;

                _ = DisplayAlert(
                    "Regisztráció",
                    hiba,
                    "OK");

                return;
            }

            _regisztraciosAdat = adat;

            DolgozoNevLabel.Text =
                adat.DolgozoNev;

            DolgozoSzamLabel.Text =
                $"Dolgozószám: {adat.DolgozoSzam}";

            SzervezetLabel.Text =
                $"Szervezet: {adat.Szervezet}";

            TelephelyLabel.Text =
                $"Telephely: {adat.Telephely}";

            AdatokLayout.IsVisible = true;

            RegisztracioButton.IsEnabled = true;
        }

        private void JelszoMegjelenitesCheckBox_CheckedChanged(
            object sender,
            CheckedChangedEventArgs e)
        {
            UjJelszoEntry.IsPassword = !e.Value;

            UjJelszoUjraEntry.IsPassword = !e.Value;
        }

        private async void RegisztracioButton_Clicked(
            object sender,
            EventArgs e)
        {
            if (_regisztracioFolyamatban)
                return;

            if (_regisztraciosAdat == null)
            {
                await DisplayAlert(
                    "Regisztráció",
                    "Elõször olvasd be a regisztrációs QR-kódot.",
                    "OK");

                return;
            }

            if (DateTime.UtcNow >
                _regisztraciosAdat.LejaratUtc.ToUniversalTime())
            {
                await DisplayAlert(
                    "Regisztráció",
                    "A regisztrációs QR-kód lejárt.",
                    "OK");

                return;
            }

            string jelszo =
                UjJelszoEntry.Text ?? "";

            string jelszoUjra =
                UjJelszoUjraEntry.Text ?? "";

            if (!JelszoKezelo.JelszoMegfelelo(
                jelszo,
                out string hiba))
            {
                await DisplayAlert(
                    "Regisztráció",
                    hiba,
                    "OK");

                return;
            }

            if (jelszo != jelszoUjra)
            {
                await DisplayAlert(
                    "Regisztráció",
                    "A két jelszó nem egyezik.",
                    "OK");

                UjJelszoUjraEntry.Text = "";

                UjJelszoUjraEntry.Focus();

                return;
            }

            try
            {
                _regisztracioFolyamatban = true;

                RegisztracioButton.IsEnabled = false;

                BetoltesIndicator.IsVisible = true;
                BetoltesIndicator.IsRunning = true;

                SQL_Kezelo_Bejelentkezes bejelentkezesDb =
                    new();

                SQL_Kezelo_Dolgozo dolgozoDb =
                    new();

                await bejelentkezesDb.InicializalasAsync();

                await dolgozoDb.InicializalasAsync();

                Adat_User? meglevoUser =
                    await SQL_Kezelo_Bejelentkezes
                        .FelhasznaloKeresesAsync(
                            _regisztraciosAdat.DolgozoSzam);

                if (meglevoUser != null)
                {
                    await DisplayAlert(
                        "Regisztráció",
                        "Ez a felhasználó ezen az eszközön már regisztrálva van.",
                        "OK");

                    return;
                }

                Adat_Dolgozo dolgozo =
                    new(
                        dolgozoSzam:
                            _regisztraciosAdat.DolgozoSzam,

                        dolgozoNev:
                            _regisztraciosAdat.DolgozoNev,

                        szervezet:
                            _regisztraciosAdat.Szervezet,

                        status: 0
                    );

                bool dolgozoLetezik =
                    await dolgozoDb.DolgozoLetezikAsync(
                        _regisztraciosAdat.DolgozoSzam);

                if (dolgozoLetezik)
                    await dolgozoDb.ModositasAsync(dolgozo);
                else
                    await dolgozoDb.RogzitesAsync(dolgozo);

                var (Hash, Salt) =
                    await Task.Run(() =>
                        JelszoKezelo.HashKeszites(jelszo));

                Adat_User user =
                    new(
                        userId: 0,

                        felhasznaloNev:
                            _regisztraciosAdat.DolgozoNev,

                        dolgozoSzam:
                            _regisztraciosAdat.DolgozoSzam,

                        jelszoHash: Hash,

                        jelszoSalt: Salt,

                        jelszoLejarat:
                            JelszoKezelo.UjLejarat(90),

                        frissit: 0,

                        szervezet:
                            _regisztraciosAdat.Szervezet,

                        admin:
                            _regisztraciosAdat.Admin
                    );

                await bejelentkezesDb.RogzitesAsync(user);

                AlkalmazasBeallitasok.TelephelyBeallitas(
                    _regisztraciosAdat.Telephely);

                string dolgozoSzam =
                    _regisztraciosAdat.DolgozoSzam;

                await DisplayAlert(
                    "Regisztráció",
                    "A regisztráció sikeres.",
                    "OK");

                Application.Current!.MainPage =
                    new NavigationPage(
                        new BejelentkezesOldal(dolgozoSzam));
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert(
                    "Regisztrációs hiba",
                    ex.ToString(),
                    "OK");
#else
                await DisplayAlert(
                    "Regisztrációs hiba",
                    "A regisztráció nem sikerült.",
                    "OK");
#endif
            }
            finally
            {
                _regisztracioFolyamatban = false;

                BetoltesIndicator.IsRunning = false;

                BetoltesIndicator.IsVisible = false;

                if (_regisztraciosAdat != null)
                    RegisztracioButton.IsEnabled = true;
            }


        }
    }
}