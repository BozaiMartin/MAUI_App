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
    }
}
