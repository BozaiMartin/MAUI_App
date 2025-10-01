namespace KmKiolvasasMaui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            //MainPage = new AppShell();
            MainPage = new ContentPage { Content = new Label { Text = "Teszt oldal" } };

        }
    }
}
