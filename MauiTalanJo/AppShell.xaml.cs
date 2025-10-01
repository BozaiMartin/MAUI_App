using KmKiolvasMaui;
using KmKiolvasasMaui;
namespace KmKiolvasasMaui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("EmailOldal", typeof(BejelentkezesOldal));
            Routing.RegisterRoute("EmailOldal", typeof(EmailOldal));
            Routing.RegisterRoute("FenykepOldal", typeof(MainPage));
        }
    }

}
