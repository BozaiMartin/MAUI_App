using KmKiolvasasMaui.Adat_Szerkezet;

namespace KmKiolvasasMaui
{
    public partial class MenuOldal : ContentPage
    {
        private readonly Adat_User _felhasznalo;

        public MenuOldal(Adat_User felhasznalo)
        {
            InitializeComponent();

            _felhasznalo = felhasznalo;
        }

        private async void NavigalMainPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage(_felhasznalo));
        }

        private async void NavigalEmailOldal(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EmailOldal(_felhasznalo));
        }

        private async void NavigalAdatokOldal(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AdatokOldal());
        }

        private async void NavigalJelszoCsereOldal(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new JelszoCsereOldal(_felhasznalo, false));
        }

        private void Kijelentkezes_Clicked(object sender, EventArgs e)
        {
            Application.Current!.MainPage = new NavigationPage(new BejelentkezesOldal());
        }
    }
}