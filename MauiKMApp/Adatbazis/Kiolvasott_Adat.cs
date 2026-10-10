using SQLite;

namespace KmKiolvasasMaui.Adatbazis
{
    public class KiolvasottAdat
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Unique = true)]
        public string ReadingId { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime Datum { get; set; }

        public int Napi_km { get; set; }

        public int Ossz_km { get; set; }

        public int Palyaszam { get; set; }

        public string DolgozoSzam { get; set; } = "";

        public string DolgozoNev { get; set; } = "";

        public string Szervezet { get; set; } = "";

        public string Telephely { get; set; } = "";

        public bool Email_kuldve { get; set; }

        public string SyncStatus { get; set; } = SyncAllapot.Sent;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? SentAtUtc { get; set; }

        [Ignore]
        public PalyaszamInfo? PalyaszamInfo { get; set; }
    }
}