using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Adat_Szerkezet;

namespace KmKiolvasasMaui
{
    public partial class BejelentkezesOldal : ContentPage
    {
        private readonly BejelentkezesSzolgaltatas _bejelentkezesSzolgaltatas;
        private bool _belepesFolyamatban;

        public BejelentkezesOldal() : this(null)
        {
        }

        public BejelentkezesOldal(string? dolgozoSzam)
        {
            InitializeComponent();

            _bejelentkezesSzolgaltatas = new BejelentkezesSzolgaltatas();

            if (!string.IsNullOrWhiteSpace(dolgozoSzam))
                DolgozoSzamEntry.Text = dolgozoSzam.Trim();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await AdatbazisInicializalo.InicializalasAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert("Adatbázis hiba", ex.ToString(), "OK");
#else
        await DisplayAlert("Adatbázis hiba", "Az adatbázis nem inicializálható.", "OK");
#endif
            }
        }

        private async void BelepesButton_Clicked(object sender, EventArgs e)
        {
            await BelepesAsync();
        }

        private async void JelszoEntry_Completed(object sender, EventArgs e)
        {
            await BelepesAsync();
        }

        private async Task BelepesAsync()
        {
            if (_belepesFolyamatban)
                return;

            string dolgozoSzam = DolgozoSzamEntry.Text?.Trim() ?? "";

            string jelszo = JelszoEntry.Text ?? "";

            if (string.IsNullOrWhiteSpace(dolgozoSzam))
            {
                await DisplayAlert("Bejelentkezés", "Add meg a dolgozószámot.", "OK");

                DolgozoSzamEntry.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(jelszo))
            {
                await DisplayAlert("Bejelentkezés", "Add meg a jelszót.", "OK");

                JelszoEntry.Focus();
                return;
            }

            try
            {
                _belepesFolyamatban = true;
                BelepesButton.IsEnabled = false;
                BetoltesIndicator.IsVisible = true;
                BetoltesIndicator.IsRunning = true;

                BejelentkezesEredmeny eredmeny = await _bejelentkezesSzolgaltatas.BejelentkezesAsync(dolgozoSzam, jelszo);

                switch (eredmeny.Tipus)
                {
                    case BejelentkezesEredmenyTipus.Sikeres:

                        if (eredmeny.Felhasznalo == null)
                        {
                            await DisplayAlert("Hiba", "A felhasználó nem tölthetõ be.", "OK");
                            break;
                        }

                        SikeresBelepes(eredmeny.Felhasznalo);
                        break;

                    case BejelentkezesEredmenyTipus.JelszoCsereSzukseges:

                        if (eredmeny.Felhasznalo == null)
                        {
                            await DisplayAlert("Hiba", "A felhasználó nem tölthetõ be.", "OK");
                            break;
                        }

                        JelszoEntry.Text = "";

                        await Navigation.PushAsync(new JelszoCsereOldal(eredmeny.Felhasznalo, true));
                        break;

                    case BejelentkezesEredmenyTipus.InaktivDolgozo:

                        await DisplayAlert("Bejelentkezés sikertelen", "A dolgozó nem aktív.", "OK");

                        JelszoEntry.Text = "";
                        break;

                    case BejelentkezesEredmenyTipus.HibasAdatok:

                    default:

                        await DisplayAlert("Bejelentkezés sikertelen", "Hibás dolgozószám vagy jelszó.", "OK");

                        JelszoEntry.Text = "";
                        JelszoEntry.Focus();
                        break;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert("Bejelentkezési hiba", ex.ToString(), "OK");
#else
                await DisplayAlert("Bejelentkezési hiba", "A bejelentkezés során hiba történt.", "OK");
#endif
            }
            finally
            {
                _belepesFolyamatban = false;
                BelepesButton.IsEnabled = true;
                BetoltesIndicator.IsRunning = false;
                BetoltesIndicator.IsVisible = false;
            }
        }
        private async void QrBeolvasasButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Kameraengedély", "A QR-kód beolvasásához engedélyezni kell a kamerát.", "OK");
                    return;
                }

                await Navigation.PushAsync(new QrBeolvasoOldal(dolgozoSzam =>
                {
                    DolgozoSzamEntry.Text = dolgozoSzam;
                    JelszoEntry.Text = "";
                    JelszoEntry.Focus();
                }));
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert("QR hiba", ex.ToString(), "OK");
#else
        await DisplayAlert("QR hiba", "A QR-kód beolvasása nem indítható el.", "OK");
#endif
            }
        }

        private void SikeresBelepes(Adat_User felhasznalo)
        {
            Application.Current!.MainPage = new NavigationPage(new MenuOldal(felhasznalo));
        }
    }
}