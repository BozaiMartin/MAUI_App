using SQLite;

namespace KmKiolvasasMaui.Adatbazis
{
    public class IdeiglenesAdat
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Datum { get; set; }
        public int Napi_km { get; set; }
        public int Ossz_km { get; set; }
        public int Palyaszam { get; set; }
    }
}
