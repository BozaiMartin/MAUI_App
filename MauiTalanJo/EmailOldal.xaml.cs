namespace KmKiolvasasMaui
{
    public partial class EmailOldal : ContentPage
    {
        public EmailOldal()
        {
            InitializeComponent();
        }

        private async void Vissza_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
