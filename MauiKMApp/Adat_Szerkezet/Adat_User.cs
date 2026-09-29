namespace KmKiolvasasMaui.Adat_Szerkezet
{
    public class Adat_User(
        int userId,
        string felhasznaloNev,
        string dolgozoSzam,
        string jelszoHash,
        string jelszoSalt,
        DateTime jelszoLejarat,
        int frissit,
        string szervezet,
        int admin)
    {
        public int UserId { get; private set; } = userId;

        public string FelhasznaloNev { get; private set; } = felhasznaloNev;

        public string DolgozoSzam { get; private set; } = dolgozoSzam;

        public string JelszoHash { get; private set; } = jelszoHash;

        public string JelszoSalt { get; private set; } = jelszoSalt;

        public DateTime JelszoLejarat { get; private set; } = jelszoLejarat;

        public int Frissit { get; private set; } = frissit;

        public string Szervezet { get; private set; } = szervezet;

        public int Admin { get; private set; } = admin;

        public void JelszoModositas(string ujHash, string ujSalt, DateTime ujLejarat)
        {
            JelszoHash = ujHash;
            JelszoSalt = ujSalt;
            JelszoLejarat = ujLejarat;
            Frissit = 0;
        }

        public void FrissitesKotelezo()
        {
            Frissit = 1;
        }
    }
}