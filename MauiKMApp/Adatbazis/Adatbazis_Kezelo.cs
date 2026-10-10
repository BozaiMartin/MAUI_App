using KmKiolvasasMaui.Kezelok;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace KmKiolvasasMaui.Adatbazis
{
    public class Adatbazis_Kezelo
    {
        private const string FajlNev = "kmadatok.db";

        private static readonly SemaphoreSlim InicializalasiZar = new(1, 1);

        private static bool _inicializalva;

        public async Task InicializalasAsync()
        {
            if (_inicializalva)
                return;

            await InicializalasiZar.WaitAsync();

            try
            {
                if (_inicializalva)
                    return;

                using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

                await SemakLetrehozasaAsync(connection);

                _inicializalva = true;
            }
            finally
            {
                InicializalasiZar.Release();
            }
        }

        public async Task<PalyaszamInfo?> PalyaszamInfoKeresesAsync(int palyaszam)
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                SELECT Palyaszam, Telephely
                FROM PalyaszamInfo
                WHERE Palyaszam = @Palyaszam
                LIMIT 1;
                """;

            cmd.Parameters.AddWithValue("@Palyaszam", palyaszam);

            using SqliteDataReader reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new PalyaszamInfo
            {
                Palyaszam = reader.GetInt32(0),

                Telephely = reader.IsDBNull(1) ? "" : reader.GetString(1)
            };
        }

        public async Task<string> TelephelyKeresesAsync(int palyaszam)
        {
            PalyaszamInfo? adat = await PalyaszamInfoKeresesAsync(palyaszam);

            if (adat != null && !string.IsNullOrWhiteSpace(adat.Telephely))
                return adat.Telephely.Trim();

            return AlkalmazasBeallitasok.Telephely;
        }

        public async Task MentPalyaszamInfoAsync(PalyaszamInfo adat)
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                INSERT INTO PalyaszamInfo
                (
                    Palyaszam,
                    Telephely
                )
                VALUES
                (
                    @Palyaszam,
                    @Telephely
                )
                ON CONFLICT(Palyaszam)
                DO UPDATE SET
                    Telephely =
                    excluded.Telephely;
                """;

            cmd.Parameters.AddWithValue("@Palyaszam", adat.Palyaszam);

            cmd.Parameters.AddWithValue("@Telephely", adat.Telephely ?? "");

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<PalyaszamInfo>> LekerdezesPalyaszamInfoAsync()
        {
            await InicializalasAsync();

            List<PalyaszamInfo> lista = [];

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                SELECT Palyaszam, Telephely
                FROM PalyaszamInfo
                ORDER BY Palyaszam;
                """;

            using SqliteDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new PalyaszamInfo
                {
                    Palyaszam = reader.GetInt32(0),
                    Telephely = reader.IsDBNull(1) ? "" : reader.GetString(1)
                });
            }

            return lista;
        }

        public async Task TorolPalyaszamInfoAsync(int palyaszam)
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                DELETE FROM PalyaszamInfo
                WHERE Palyaszam = @Palyaszam;
                """;

            cmd.Parameters.AddWithValue("@Palyaszam", palyaszam);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MentIdeiglenesAsync(IdeiglenesAdat adat)
        {
            await InicializalasAsync();

            if (string.IsNullOrWhiteSpace(adat.ReadingId))
                adat.ReadingId = Guid.NewGuid().ToString("N");

            if (string.IsNullOrWhiteSpace(adat.SyncStatus))
                adat.SyncStatus = SyncAllapot.Pending;

            if (adat.CreatedAtUtc == default)
                adat.CreatedAtUtc = DateTime.UtcNow;

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                INSERT INTO IdeiglenesAdat
                (
                    ReadingId,
                    Datum,
                    Napi_km,
                    Ossz_km,
                    Palyaszam,
                    DolgozoSzam,
                    DolgozoNev,
                    Szervezet,
                    Telephely,
                    SyncStatus,
                    SyncBatchId,
                    CreatedAtUtc
                )
                VALUES
                (
                    @ReadingId,
                    @Datum,
                    @NapiKm,
                    @OsszKm,
                    @Palyaszam,
                    @DolgozoSzam,
                    @DolgozoNev,
                    @Szervezet,
                    @Telephely,
                    @SyncStatus,
                    @SyncBatchId,
                    @CreatedAtUtc
                );
                """;

            IdeiglenesParameterek(cmd, adat);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<IdeiglenesAdat>> LekerdezesIdeiglenesAsync()
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            return await IdeiglenesListaAsync(connection, null, null);
        }

        public async Task TorolIdeiglenesAsync()
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText = """DELETE FROM IdeiglenesAdat;""";

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> EllenorizDuplikatumAsync(DateTime datum, int palyaszam, int napiKm, int osszKm)
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                SELECT COUNT(1)
                FROM
                (
                    SELECT
                        Datum,
                        Palyaszam,
                        Napi_km,
                        Ossz_km
                    FROM IdeiglenesAdat

                    UNION ALL

                    SELECT
                        Datum,
                        Palyaszam,
                        Napi_km,
                        Ossz_km
                    FROM KiolvasottAdat
                )
                WHERE
                    substr(Datum, 1, 10) = @Nap
                    AND Palyaszam = @Palyaszam
                    AND Napi_km = @NapiKm
                    AND Ossz_km = @OsszKm;
                """;

            cmd.Parameters.AddWithValue("@Nap", datum.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            cmd.Parameters.AddWithValue("@Palyaszam", palyaszam);

            cmd.Parameters.AddWithValue("@NapiKm", napiKm);

            cmd.Parameters.AddWithValue("@OsszKm", osszKm);

            object? eredmeny = await cmd.ExecuteScalarAsync();

            return
                Convert.ToInt32(eredmeny) > 0;
        }

        public async Task<(string? BatchId, List<IdeiglenesAdat> Adatok)> SyncCsomagEloKeszitesAsync()
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteTransaction transaction = connection.BeginTransaction();

            try
            {
                string? batchId = null;

                // 1. Van-e korábban félbemaradt
                // Failed vagy Syncing csomag?
                using (SqliteCommand keres = connection.CreateCommand())
                {
                    keres.Transaction = transaction;

                    keres.CommandText =
                        """
                SELECT SyncBatchId
                FROM IdeiglenesAdat
                WHERE
                    SyncBatchId IS NOT NULL
                    AND SyncBatchId <> ''
                    AND
                    (
                        SyncStatus = @Failed
                        OR SyncStatus = @Syncing
                    )
                ORDER BY Id
                LIMIT 1;
                """;

                    keres.Parameters.AddWithValue("@Failed", SyncAllapot.Failed);

                    keres.Parameters.AddWithValue("@Syncing", SyncAllapot.Syncing);

                    object? eredmeny = await keres.ExecuteScalarAsync();

                    if (eredmeny != null && eredmeny != DBNull.Value)
                        batchId = Convert.ToString(eredmeny);
                }

                // Ha nincs régi csomag,
                // a Pending rekordokból újat készítünk.
                if (string.IsNullOrWhiteSpace(batchId))
                {
                    batchId = Guid.NewGuid().ToString("N");

                    using SqliteCommand ujCsomag = connection.CreateCommand();

                    ujCsomag.Transaction = transaction;

                    ujCsomag.CommandText =
                        """
                UPDATE IdeiglenesAdat
                SET
                    SyncBatchId = @BatchId,
                    SyncStatus = @Syncing
                WHERE
                    SyncStatus = @Pending
                    AND
                    (
                        SyncBatchId IS NULL
                        OR SyncBatchId = ''
                    );
                """;

                    ujCsomag.Parameters.AddWithValue("@BatchId", batchId);

                    ujCsomag.Parameters.AddWithValue("@Syncing", SyncAllapot.Syncing);

                    ujCsomag.Parameters.AddWithValue("@Pending", SyncAllapot.Pending);

                    int modosult = await ujCsomag.ExecuteNonQueryAsync();

                    if (modosult == 0)
                    {
                        transaction.Commit();

                        return (null, new List<IdeiglenesAdat>());
                    }
                }
                else
                {
                    // 3. A régi Failed csomagot
                    // újra Syncing állapotba tesszük.
                    using SqliteCommand ujraprobal = connection.CreateCommand();

                    ujraprobal.Transaction = transaction;

                    ujraprobal.CommandText =
                        """
                UPDATE IdeiglenesAdat
                SET SyncStatus = @Syncing
                WHERE SyncBatchId = @BatchId;
                """;

                    ujraprobal.Parameters.AddWithValue("@Syncing", SyncAllapot.Syncing);

                    ujraprobal.Parameters.AddWithValue("@BatchId", batchId);

                    await ujraprobal.ExecuteNonQueryAsync();
                }

                // 4. Az aktuális csomag adatainak lekérése.
                List<IdeiglenesAdat> adatok = await IdeiglenesListaAsync(connection, transaction, batchId);

                transaction.Commit();

                return (batchId, adatok);
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task SyncCsomagSikertelenAsync(string batchId)
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                UPDATE IdeiglenesAdat
                SET SyncStatus = @Failed
                WHERE SyncBatchId = @BatchId;
                """;

            cmd.Parameters.AddWithValue("@Failed", SyncAllapot.Failed);

            cmd.Parameters.AddWithValue("@BatchId", batchId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SyncCsomagSikeresAsync(string batchId)
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteTransaction transaction = connection.BeginTransaction();

            List<IdeiglenesAdat> lista = await IdeiglenesListaAsync(connection, transaction, batchId);

            foreach (IdeiglenesAdat adat in lista)
            {
                using SqliteCommand cmd = connection.CreateCommand();

                cmd.Transaction = transaction;

                cmd.CommandText =
                    """
                    INSERT OR IGNORE
                    INTO KiolvasottAdat
                    (
                        ReadingId,
                        Datum,
                        Napi_km,
                        Ossz_km,
                        Palyaszam,
                        DolgozoSzam,
                        DolgozoNev,
                        Szervezet,
                        Telephely,
                        Email_kuldve,
                        SyncStatus,
                        CreatedAtUtc,
                        SentAtUtc
                    )
                    VALUES
                    (
                        @ReadingId,
                        @Datum,
                        @NapiKm,
                        @OsszKm,
                        @Palyaszam,
                        @DolgozoSzam,
                        @DolgozoNev,
                        @Szervezet,
                        @Telephely,
                        1,
                        @SyncStatus,
                        @CreatedAtUtc,
                        @SentAtUtc
                    );
                    """;

                cmd.Parameters.AddWithValue("@ReadingId", adat.ReadingId);

                cmd.Parameters.AddWithValue("@Datum", DatumSzoveg(adat.Datum));

                cmd.Parameters.AddWithValue("@NapiKm", adat.Napi_km);

                cmd.Parameters.AddWithValue("@OsszKm", adat.Ossz_km);

                cmd.Parameters.AddWithValue("@Palyaszam", adat.Palyaszam);

                cmd.Parameters.AddWithValue("@DolgozoSzam", adat.DolgozoSzam);

                cmd.Parameters.AddWithValue("@DolgozoNev", adat.DolgozoNev);

                cmd.Parameters.AddWithValue("@Szervezet", adat.Szervezet);

                cmd.Parameters.AddWithValue("@Telephely", adat.Telephely);

                cmd.Parameters.AddWithValue("@SyncStatus", SyncAllapot.Sent);

                cmd.Parameters.AddWithValue("@CreatedAtUtc", DatumSzoveg(adat.CreatedAtUtc));

                cmd.Parameters.AddWithValue("@SentAtUtc", DatumSzoveg(DateTime.UtcNow));

                await cmd.ExecuteNonQueryAsync();
            }

            using (SqliteCommand torles = connection.CreateCommand())
            {
                torles.Transaction = transaction;

                torles.CommandText = """DELETE FROM IdeiglenesAdat WHERE SyncBatchId = @BatchId; """;

                torles.Parameters.AddWithValue("@BatchId", batchId);

                await torles.ExecuteNonQueryAsync();
            }

            transaction.Commit();
        }

        public async Task TorolKiolvasottAdatokatAsync()
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText = """DELETE FROM KiolvasottAdat;""";

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task MentesAsync(KiolvasottAdat adat)
        {
            await InicializalasAsync();

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                INSERT OR IGNORE
                INTO KiolvasottAdat
                (
                    ReadingId,
                    Datum,
                    Napi_km,
                    Ossz_km,
                    Palyaszam,
                    DolgozoSzam,
                    DolgozoNev,
                    Szervezet,
                    Telephely,
                    Email_kuldve,
                    SyncStatus,
                    CreatedAtUtc,
                    SentAtUtc
                )
                VALUES
                (
                    @ReadingId,
                    @Datum,
                    @NapiKm,
                    @OsszKm,
                    @Palyaszam,
                    @DolgozoSzam,
                    @DolgozoNev,
                    @Szervezet,
                    @Telephely,
                    @EmailKuldve,
                    @SyncStatus,
                    @CreatedAtUtc,
                    @SentAtUtc
                );
                """;

            cmd.Parameters.AddWithValue("@ReadingId", adat.ReadingId);

            cmd.Parameters.AddWithValue("@Datum", DatumSzoveg(adat.Datum));

            cmd.Parameters.AddWithValue("@NapiKm", adat.Napi_km);

            cmd.Parameters.AddWithValue("@OsszKm", adat.Ossz_km);

            cmd.Parameters.AddWithValue("@Palyaszam", adat.Palyaszam);

            cmd.Parameters.AddWithValue("@DolgozoSzam", adat.DolgozoSzam);

            cmd.Parameters.AddWithValue("@DolgozoNev", adat.DolgozoNev);

            cmd.Parameters.AddWithValue("@Szervezet", adat.Szervezet);

            cmd.Parameters.AddWithValue("@Telephely", adat.Telephely);

            cmd.Parameters.AddWithValue("@EmailKuldve", adat.Email_kuldve ? 1 : 0);

            cmd.Parameters.AddWithValue("@SyncStatus", adat.SyncStatus);

            cmd.Parameters.AddWithValue("@CreatedAtUtc", DatumSzoveg(adat.CreatedAtUtc));

            cmd.Parameters.AddWithValue("@SentAtUtc", adat.SentAtUtc.HasValue ? DatumSzoveg(adat.SentAtUtc.Value) : DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<KiolvasottAdat>> LekerdezesAsync()
        {
            await InicializalasAsync();

            List<KiolvasottAdat> lista = [];

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText =
                """
                SELECT
                    Id,
                    ReadingId,
                    Datum,
                    Napi_km,
                    Ossz_km,
                    Palyaszam,
                    DolgozoSzam,
                    DolgozoNev,
                    Szervezet,
                    Telephely,
                    Email_kuldve,
                    SyncStatus,
                    CreatedAtUtc,
                    SentAtUtc
                FROM KiolvasottAdat
                ORDER BY
                    Datum DESC,
                    Id DESC;
                """;

            using SqliteDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new KiolvasottAdat
                {
                    Id = reader.GetInt32(0),

                    ReadingId = reader.GetString(1),

                    Datum = DatumOlvas(reader.GetString(2)),

                    Napi_km = reader.GetInt32(3),

                    Ossz_km = reader.GetInt32(4),

                    Palyaszam = reader.GetInt32(5),

                    DolgozoSzam = reader.GetString(6),

                    DolgozoNev = reader.GetString(7),

                    Szervezet = reader.GetString(8),

                    Telephely = reader.GetString(9),

                    Email_kuldve = reader.GetInt32(10) == 1,

                    SyncStatus = reader.GetString(11),

                    CreatedAtUtc = DatumOlvas(reader.GetString(12)),

                    SentAtUtc = reader.IsDBNull(13) ? null : DatumOlvas(reader.GetString(13))
                });
            }

            return lista;
        }

        private static async Task SemakLetrehozasaAsync(SqliteConnection connection)
        {
            string[] parancsok =
            {
                """
                CREATE TABLE IF NOT EXISTS
                PalyaszamInfo
                (
                    Palyaszam INTEGER
                        PRIMARY KEY,

                    Telephely TEXT
                        NOT NULL
                        DEFAULT ''
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS IdeiglenesAdat
                (
                    Id INTEGER
                        PRIMARY KEY
                        AUTOINCREMENT,

                    ReadingId TEXT
                        NOT NULL
                        UNIQUE,

                    Datum TEXT
                        NOT NULL,

                    Napi_km INTEGER
                        NOT NULL,

                    Ossz_km INTEGER
                        NOT NULL,

                    Palyaszam INTEGER
                        NOT NULL,

                    DolgozoSzam TEXT
                        NOT NULL,

                    DolgozoNev TEXT
                        NOT NULL,

                    Szervezet TEXT
                        NOT NULL,

                    Telephely TEXT
                        NOT NULL,

                    SyncStatus TEXT
                        NOT NULL,

                    SyncBatchId TEXT,

                    CreatedAtUtc TEXT
                        NOT NULL
              );
              """,

                """
                CREATE TABLE IF NOT EXISTS
                KiolvasottAdat
                (
                    Id INTEGER
                        PRIMARY KEY
                        AUTOINCREMENT,

                    ReadingId TEXT
                        NOT NULL
                        UNIQUE,

                    Datum TEXT
                        NOT NULL,

                    Napi_km INTEGER
                        NOT NULL,

                    Ossz_km INTEGER
                        NOT NULL,

                    Palyaszam INTEGER
                        NOT NULL,

                    DolgozoSzam TEXT
                        NOT NULL,

                    DolgozoNev TEXT
                        NOT NULL,

                    Szervezet TEXT
                        NOT NULL,

                    Telephely TEXT
                        NOT NULL,

                    Email_kuldve INTEGER
                        NOT NULL
                        DEFAULT 0,

                    SyncStatus TEXT
                        NOT NULL,

                    CreatedAtUtc TEXT
                        NOT NULL,

                    SentAtUtc TEXT
                        NULL
                );
                """,

                """
                CREATE INDEX IF NOT EXISTS
                IX_IdeiglenesAdat_SyncBatchId
                ON IdeiglenesAdat(SyncBatchId);
                """,

                """
                CREATE INDEX IF NOT EXISTS
                IX_IdeiglenesAdat_Palyaszam
                ON IdeiglenesAdat(Palyaszam);
                """,

                """
                CREATE INDEX IF NOT EXISTS
                IX_KiolvasottAdat_Palyaszam
                ON KiolvasottAdat(Palyaszam);
                """
            };

            foreach (string sql in parancsok)
            {
                using SqliteCommand cmd = connection.CreateCommand();

                cmd.CommandText = sql;

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task<List<IdeiglenesAdat>> IdeiglenesListaAsync(SqliteConnection connection, SqliteTransaction? transaction, string? batchId)
        {
            List<IdeiglenesAdat> lista = [];

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.Transaction = transaction;

            cmd.CommandText = batchId == null ?

                """
                SELECT
                    Id,
                    ReadingId,
                    Datum,
                    Napi_km,
                    Ossz_km,
                    Palyaszam,
                    DolgozoSzam,
                    DolgozoNev,
                    Szervezet,
                    Telephely,
                    SyncStatus,
                    SyncBatchId,
                    CreatedAtUtc
                FROM IdeiglenesAdat
                ORDER BY
                    CreatedAtUtc,
                    Id;
                """
                :
                """
                SELECT
                    Id,
                    ReadingId,
                    Datum,
                    Napi_km,
                    Ossz_km,
                    Palyaszam,
                    DolgozoSzam,
                    DolgozoNev,
                    Szervezet,
                    Telephely,
                    SyncStatus,
                    SyncBatchId,
                    CreatedAtUtc
                FROM IdeiglenesAdat
                WHERE SyncBatchId = @BatchId
                ORDER BY
                    CreatedAtUtc,
                    Id;
                """;

            if (batchId != null)
                cmd.Parameters.AddWithValue("@BatchId", batchId);

            using SqliteDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new IdeiglenesAdat
                {
                    Id = reader.GetInt32(0),

                    ReadingId = reader.GetString(1),

                    Datum = DatumOlvas(reader.GetString(2)),

                    Napi_km = reader.GetInt32(3),

                    Ossz_km = reader.GetInt32(4),

                    Palyaszam = reader.GetInt32(5),

                    DolgozoSzam = reader.GetString(6),

                    DolgozoNev = reader.GetString(7),

                    Szervezet = reader.GetString(8),

                    Telephely = reader.GetString(9),

                    SyncStatus = reader.GetString(10),

                    SyncBatchId = reader.IsDBNull(11) ? null : reader.GetString(11),

                    CreatedAtUtc = DatumOlvas(reader.GetString(12))
                });
            }
            return lista;
        }

        private static void IdeiglenesParameterek(SqliteCommand cmd, IdeiglenesAdat adat)
        {
            cmd.Parameters.AddWithValue("@ReadingId", adat.ReadingId);

            cmd.Parameters.AddWithValue("@Datum", DatumSzoveg(adat.Datum));

            cmd.Parameters.AddWithValue("@NapiKm", adat.Napi_km);

            cmd.Parameters.AddWithValue("@OsszKm", adat.Ossz_km);

            cmd.Parameters.AddWithValue("@Palyaszam", adat.Palyaszam);

            cmd.Parameters.AddWithValue("@DolgozoSzam", adat.DolgozoSzam);

            cmd.Parameters.AddWithValue("@DolgozoNev", adat.DolgozoNev);

            cmd.Parameters.AddWithValue("@Szervezet", adat.Szervezet);

            cmd.Parameters.AddWithValue("@Telephely", adat.Telephely);

            cmd.Parameters.AddWithValue("@SyncStatus", adat.SyncStatus);

            cmd.Parameters.AddWithValue("@SyncBatchId", (object?)adat.SyncBatchId ?? DBNull.Value);

            cmd.Parameters.AddWithValue("@CreatedAtUtc", DatumSzoveg(adat.CreatedAtUtc));
        }

        private static string DatumSzoveg(DateTime datum)
        {
            return datum.ToString("O", CultureInfo.InvariantCulture);
        }

        private static DateTime DatumOlvas(string szoveg)
        {
            if (DateTime.TryParse(szoveg, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime datum))
                return datum;

            return DateTime.MinValue;
        }
    }
}