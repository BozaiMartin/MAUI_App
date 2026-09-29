namespace KmKiolvasasMaui.Adat_Szerkezet
{
    public class Adat_Dolgozo(
        string dolgozoSzam,
        string dolgozoNev,
        string szervezet,
        int status)
    {
        public string DolgozoSzam { get; private set; } = dolgozoSzam;

        public string DolgozoNev { get; private set; } = dolgozoNev;

        public string Szervezet { get; private set; } = szervezet;

        public int Status { get; private set; } = status;
    }
}