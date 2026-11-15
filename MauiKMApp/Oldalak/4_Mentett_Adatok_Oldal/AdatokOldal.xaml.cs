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

            Adatbazis_Kezelo db = new();
            await Adatbazis_Kezelo.InicializalasAsync();
            List<KiolvasottAdat> lista = await Adatbazis_Kezelo.LekerdezesAsync();

            AdatokLista.ItemsSource = lista;

            if (lista.Count == 0)
                await DisplayAlert("Info", "Nincs még mentett adat az adatbázisban.", "OK");
            else
                AdatokLista.ItemsSource = lista;

        }
    }
}
