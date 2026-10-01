namespace KmKiolvasasMaui.Adat_Szerkezet
{
    public class RegisztraciosAdat
    {
        public int Verzio { get; set; } = 1;

        public string RegisztracioId { get; set; } = "";

        public string DolgozoSzam { get; set; } = "";

        public string DolgozoNev { get; set; } = "";

        public string Szervezet { get; set; } = "";

        public string Telephely { get; set; } = "";

        public int Admin { get; set; }

        public DateTime LejaratUtc { get; set; }
    }
}