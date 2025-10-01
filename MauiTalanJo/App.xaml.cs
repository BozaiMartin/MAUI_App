using KmKiolvasMaui;

namespace KmKiolvasasMaui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();

            MainPage.Dispatcher.Dispatch(async () =>
            {
                await Shell.Current.GoToAsync("BejelentkezesOldal");
            });

            // MainPage = new BejelentkezesOldal{};

        }
    }
}
