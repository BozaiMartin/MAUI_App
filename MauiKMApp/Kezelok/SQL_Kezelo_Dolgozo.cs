using Microsoft.Data.Sqlite;
using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Adatbazis;

namespace KmKiolvasasMaui.Kezelok
{
    public class SQL_Kezelo_Dolgozo
    {
        private const string FajlNev = "Alapadatok.db";
        private const string TablaNev = "Dolgozo";

        public async Task InicializalasAsync()
        {
            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            string sql = $"""
                CREATE TABLE IF NOT EXISTS {TablaNev}
                (
                    DolgozoSzam TEXT PRIMARY KEY,

                    DolgozoNev TEXT NOT NULL,

                    Szervezet TEXT NOT NULL,

                    Status INTEGER NOT NULL DEFAULT 0
                );
                """;

            using SqliteCommand cmd = new(sql, connection);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task RogzitesAsync(Adat_Dolgozo adat)
        {
            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            string sql = $"""
                INSERT INTO {TablaNev}
                (
                    DolgozoSzam,
                    DolgozoNev,
                    Szervezet,
                    Status
                )
                VALUES
                (
                    @DolgozoSzam,
                    @DolgozoNev,
                    @Szervezet,
                    @Status
                );
                """;

            using SqliteCommand cmd = new(sql, connection);

            cmd.Parameters.AddWithValue("@DolgozoSzam", adat.DolgozoSzam);

            cmd.Parameters.AddWithValue("@DolgozoNev", adat.DolgozoNev);

            cmd.Parameters.AddWithValue("@Szervezet", adat.Szervezet);

            cmd.Parameters.AddWithValue("@Status", adat.Status);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ModositasAsync(Adat_Dolgozo adat)
        {
            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            string sql = $"""
                UPDATE {TablaNev}
                SET
                    DolgozoNev = @DolgozoNev,
                    Szervezet = @Szervezet,
                    Status = @Status
                WHERE DolgozoSzam = @DolgozoSzam;
                """;

            using SqliteCommand cmd = new(sql, connection);

            cmd.Parameters.AddWithValue("@DolgozoSzam", adat.DolgozoSzam);

            cmd.Parameters.AddWithValue("@DolgozoNev", adat.DolgozoNev);

            cmd.Parameters.AddWithValue("@Szervezet", adat.Szervezet);

            cmd.Parameters.AddWithValue("@Status", adat.Status);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<bool> DolgozoAktivAsync(string dolgozoSzam)
        {
            if (string.IsNullOrWhiteSpace(dolgozoSzam))
                return false;

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = $"""
                SELECT COUNT(1)
                FROM {TablaNev}
                WHERE DolgozoSzam = $dolgozoSzam
                  AND Status = 0;
                """;

            command.Parameters.AddWithValue("$dolgozoSzam", dolgozoSzam.Trim());

            object? eredmeny = await command.ExecuteScalarAsync();

            return Convert.ToInt32(eredmeny) > 0;
        }


        public async Task<bool> DolgozoLetezikAsync(string dolgozoSzam)
        {
            if (string.IsNullOrWhiteSpace(dolgozoSzam))
                return false;

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = $"""
                SELECT COUNT(1)
                FROM {TablaNev}
                WHERE DolgozoSzam = $dolgozoSzam;
                """;

            command.Parameters.AddWithValue("$dolgozoSzam", dolgozoSzam.Trim());

            object? eredmeny = await command.ExecuteScalarAsync();

            return Convert.ToInt32(eredmeny) > 0;
        }
    }
}