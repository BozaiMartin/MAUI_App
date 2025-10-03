using KmKiolvasasMaui.Adatbazis;

namespace KmKiolvasasMaui
{
    public partial class AdatokOldal : ContentPage
    {
        public AdatokOldal()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var db = new Adatbazis_Kezelo();
            await db.InicializalasAsync();
            var lista = await db.LekerdezesAsync();

            AdatokLista.ItemsSource = lista;

            if (lista.Count == 0)
                await DisplayAlert("Info", "Nincs még mentett adat az adatbázisban.", "OK");
            else
                AdatokLista.ItemsSource = lista;

        }
    }
}
