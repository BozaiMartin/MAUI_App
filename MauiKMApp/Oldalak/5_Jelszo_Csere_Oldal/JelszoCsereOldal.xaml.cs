using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Kezelok;
using Microsoft.Maui;

namespace KmKiolvasasMaui
{
    public partial class JelszoCsereOldal : ContentPage
    {
        private readonly Adat_User _felhasznalo;
        private readonly SQL_Kezelo_Bejelentkezes _db;
        private readonly bool _kotelezo;

        private bool _mentesFolyamatban;

        public JelszoCsereOldal(Adat_User felhasznalo, bool kotelezo)
        {
            InitializeComponent();

            _felhasznalo = felhasznalo;

            _kotelezo = kotelezo;

            _db = new SQL_Kezelo_Bejelentkezes();

            RegiJelszoEntry.IsVisible = !_kotelezo;

            if (_kotelezo)
            {
                CimLabel.Text = "Új jelszó megadása";

                NavigationPage.SetHasBackButton(this, false);
            }
        }

        private async void MentesButton_Clicked(object sender, EventArgs e)
        {
            await JelszoModositasAsync();
        }

        private async void UjJelszoUjraEntry_Completed(object sender, EventArgs e)
        {
            await JelszoModositasAsync();
        }

        private async Task JelszoModositasAsync()
        {
            if (_mentesFolyamatban)
                return;

            string regiJelszo = RegiJelszoEntry.Text ?? "";

            string ujJelszo = UjJelszoEntry.Text ?? "";

            string ujJelszoUjra = UjJelszoUjraEntry.Text ?? "";

            if (!_kotelezo)
            {
                if (string.IsNullOrWhiteSpace(regiJelszo))
                {
                    await DisplayAlert("Jelszó módosítása", "Add meg a jelenlegi jelszót.", "OK");

                    RegiJelszoEntry.Focus();
                    return;
                }

                bool regiJelszoJo = await Task.Run(() =>
                    JelszoKezelo.Ellenorzes(
                        regiJelszo,
                        _felhasznalo.JelszoHash,
                        _felhasznalo.JelszoSalt));

                if (!regiJelszoJo)
                {
                    await DisplayAlert("Jelszó módosítása", "A jelenlegi jelszó hibás.", "OK");

                    RegiJelszoEntry.Text = "";
                    RegiJelszoEntry.Focus();
                    return;
                }
            }

            if (!JelszoKezelo.JelszoMegfelelo(ujJelszo, out string hiba))
            {
                await DisplayAlert("Jelszó módosítása", hiba, "OK");

                UjJelszoEntry.Focus();
                return;
            }

            if (ujJelszo != ujJelszoUjra)
            {
                await DisplayAlert("Jelszó módosítása", "A két új jelszó nem egyezik.", "OK");

                UjJelszoUjraEntry.Text = "";
                UjJelszoUjraEntry.Focus();
                return;
            }

            bool ugyanazAJelszo = await Task.Run(() => JelszoKezelo.Ellenorzes(ujJelszo, _felhasznalo.JelszoHash, _felhasznalo.JelszoSalt));

            if (ugyanazAJelszo)
            {
                await DisplayAlert("Jelszó módosítása", "Az új jelszó nem lehet ugyanaz, mint a jelenlegi jelszó.", "OK");
                return;
            }

            try
            {
                _mentesFolyamatban = true;

                MentesButton.IsEnabled = false;

                BetoltesIndicator.IsVisible = true;
                BetoltesIndicator.IsRunning = true;

                var (Hash, Salt) = await Task.Run(() => JelszoKezelo.HashKeszites(ujJelszo));

                _felhasznalo.JelszoModositas(Hash, Salt, JelszoKezelo.UjLejarat(90));

                await _db.ModositasAsync(_felhasznalo);

                await DisplayAlert("Jelszó módosítása", "A jelszó sikeresen megváltozott.", "OK");

                if (_kotelezo)
                {
                    Application.Current!.MainPage = new NavigationPage(new MenuOldal(_felhasznalo));

                    return;
                }

                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert("Jelszó módosítási hiba", ex.ToString(), "OK");
#else
                await DisplayAlert("Jelszó módosítási hiba", "A jelszó módosítása során hiba történt.", "OK");
#endif
            }
            finally
            {
                _mentesFolyamatban = false;
                MentesButton.IsEnabled = true;
                BetoltesIndicator.IsRunning = false;
                BetoltesIndicator.IsVisible = false;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (_kotelezo)
                return true;

            return base.OnBackButtonPressed();
        }

        private void JelszoMegjelenitesCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            RegiJelszoEntry.IsPassword = !e.Value;

            UjJelszoEntry.IsPassword = !e.Value;

            UjJelszoUjraEntry.IsPassword = !e.Value;
        }
    }
}