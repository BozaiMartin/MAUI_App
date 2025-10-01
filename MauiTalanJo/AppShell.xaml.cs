using KmKiolvasMaui;
using KmKiolvasasMaui;
namespace KmKiolvasasMaui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("EmailOldal", typeof(EmailOldal));
            Routing.RegisterRoute("BejelentkezesOldal", typeof(BejelentkezesOldal));

        }
    }

}
