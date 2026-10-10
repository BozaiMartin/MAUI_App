using SQLite;

namespace KmKiolvasasMaui.Adatbazis
{
    public class IdeiglenesAdat
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

        public string SyncStatus { get; set; } = SyncAllapot.Pending;

        public string? SyncBatchId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public static class SyncAllapot
    {
        public const string Pending = "Pending";

        public const string Syncing = "Syncing";

        public const string Failed = "Failed";

        public const string Sent = "Sent";
    }
}