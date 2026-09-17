using SQLite;

namespace KmKiolvasasMaui.Adatbazis
{
    public class PalyaszamInfo
    {
        [PrimaryKey]
        public int Palyaszam { get; set; }
        public string Telephely { get; set; }
    }
}
