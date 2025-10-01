using Microsoft.Maui.Controls;

namespace KmKiolvasasMaui
{
    public partial class BejelentkezesOldal : ContentPage
    {
        public BejelentkezesOldal()
        {
            InitializeComponent();
        }

        private async void NavigalMainPage(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("MainPage");
        }

        private async void NavigalEmailOldal(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("EmailOldal");
        }
    }
}
