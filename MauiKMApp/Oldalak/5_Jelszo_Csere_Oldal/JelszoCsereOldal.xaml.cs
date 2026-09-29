using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Adatbazis;
using KmKiolvasasMaui.Kezelok;

namespace KmKiolvasasMaui
{
    public partial class JelszoCsereOldal : ContentPage
    {
        private readonly Adat_User _user;
        private readonly SQL_Kezelo_Bejelentkezes _bejelentkezesDb;
        private bool _mentesFolyamatban;

        public JelszoCsereOldal(Adat_User user)
        {
            InitializeComponent();

            _user = user
                ?? throw new ArgumentNullException(nameof(user));

            _bejelentkezesDb =
                new SQL_Kezelo_Bejelentkezes();

            // A dolgozószám csak megjelenik,
            // nem szerkeszthetõ.
            DolgozoSzamLabel.Text =
                _user.DolgozoSzam;
        }

        private async void MentesButton_Clicked(object sender, EventArgs e)
        {
            await JelszoMentesAsync();
        }

        private async void UjJelszoMegerositesEntry_Completed(object sender, EventArgs e)
        {
            await JelszoMentesAsync();
        }

        private async Task JelszoMentesAsync()
        {
            if (_mentesFolyamatban)
                return;

            string regiJelszo = RegiJelszoEntry.Text ?? "";

            string ujJelszo = UjJelszoEntry.Text ?? "";

            string ujJelszoMegerosites = UjJelszoMegerositesEntry.Text ?? "";

            // 1. Régi jelszó megadása
            
            if (string.IsNullOrWhiteSpace(regiJelszo))
            {
                await DisplayAlert( "Jelszó módosítása", "Add meg a jelenlegi jelszavadat.", "OK");

                RegiJelszoEntry.Focus();
                return;
            }

            // 2. Régi jelszó ellenõrzése
            
            bool regiJelszoHelyes = JelszoKezelo.Ellenorzes( regiJelszo, _user.JelszoHash, _user.JelszoSalt);

            if (!regiJelszoHelyes)
            {
                await DisplayAlert("Jelszó módosítása", "A régi jelszó hibás.", "OK");

                RegiJelszoEntry.Text = "";
                RegiJelszoEntry.Focus();
                return;
            }

            // 3. Új jelszó
            
            if (string.IsNullOrWhiteSpace(ujJelszo))
            {
                await DisplayAlert("Jelszó módosítása", "Add meg az új jelszót.", "OK");

                UjJelszoEntry.Focus();
                return;
            }

            // 4. Új jelszó megerõsítése
            
            if (string.IsNullOrWhiteSpace(ujJelszoMegerosites))
            {
                await DisplayAlert("Jelszó módosítása", "Add meg újra az új jelszót.", "OK");

                UjJelszoMegerositesEntry.Focus();
                return;
            }

            // 5. A két új jelszó egyezzen
            
            if (ujJelszo != ujJelszoMegerosites)
            {
                await DisplayAlert("Jelszó módosítása", "A két új jelszó nem egyezik.", "OK");

                UjJelszoMegerositesEntry.Text = "";
                UjJelszoMegerositesEntry.Focus();
                return;
            }

            // 6. Jelszószabályok
            
            if (!JelszoKezelo.JelszoMegfelelo(ujJelszo, out string hiba))
            {
                await DisplayAlert("Jelszó módosítása", hiba, "OK");

                return;
            }

            // 7. Ne lehessen az új jelszó ugyanaz, mint a régi

            bool ugyanazMintARegi = JelszoKezelo.Ellenorzes( ujJelszo, _user.JelszoHash, _user.JelszoSalt);

            if (ugyanazMintARegi)
            {
                await DisplayAlert("Jelszó módosítása", "Az új jelszó nem lehet azonos a régi jelszóval.", "OK");

                UjJelszoEntry.Text = "";
                UjJelszoMegerositesEntry.Text = "";
                UjJelszoEntry.Focus();
                return;
            }

            try
            {
                _mentesFolyamatban = true;
                MentesButton.IsEnabled = false;
                BetoltesIndicator.IsVisible = true;
                BetoltesIndicator.IsRunning = true;

                // 8. Új hash és új salt

                var ujJelszoAdat = await Task.Run(() => JelszoKezelo.HashKeszites(ujJelszo));

                // 9. User objektum módosítása
                // Frissit = 0 lesz
                // új lejárat = +90 nap

                _user.JelszoModositas( ujJelszoAdat.Hash, ujJelszoAdat.Salt, JelszoKezelo.UjLejarat(90));

                // 10. Mentés az adatbázisba
                
                await _bejelentkezesDb.ModositasAsync(_user);

                // 11. Mezõk törlése
                
                RegiJelszoEntry.Text = "";
                UjJelszoEntry.Text = "";
                UjJelszoMegerositesEntry.Text = "";

                await DisplayAlert("Jelszó módosítása", "A jelszó sikeresen módosítva. Jelentkezz be az új jelszóval.", "OK");

                // 12. VISSZA A BEJELENTKEZÉSHEZ
                
                Application.Current!.MainPage = new NavigationPage( new BejelentkezesOldal( _user.DolgozoSzam));
            }
            catch (Exception ex)
            {
#if DEBUG
                await DisplayAlert("Jelszó módosítási hiba", ex.ToString(), "OK");
#else
                await DisplayAlert("Jelszó módosítási hiba",  "A jelszó módosítása nem sikerült.", "OK");
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
        private void JelszoMutatasaCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            bool rejtett = !e.Value;
            UjJelszoEntry.IsPassword = rejtett;
            UjJelszoMegerositesEntry.IsPassword = rejtett;
        }
    }
}