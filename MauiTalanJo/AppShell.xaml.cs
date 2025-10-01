using KmKiolvasMaui;
namespace KmKiolvasasMaui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("EmailOldal", typeof(EmailOldal));
            Routing.RegisterRoute("FenykepOldal", typeof(FenykepOldal));
        }
    }

}
