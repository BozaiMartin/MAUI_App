using KmKiolvasasMaui.Adat_Szerkezet;
using KmKiolvasasMaui.Adatbazis;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace KmKiolvasasMaui.Kezelok
{
    public class SQL_Kezelo_Bejelentkezes
    {
        private const string FajlNev = "Bejelentkezes.db";

        private const string TablaNev = "Users";

        public async Task InicializalasAsync()
        {
            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            string sql = $"""
                CREATE TABLE IF NOT EXISTS {TablaNev}
                (
                    UserId INTEGER PRIMARY KEY AUTOINCREMENT,

                    FelhasznaloNev TEXT NOT NULL UNIQUE,

                    DolgozoSzam TEXT NOT NULL UNIQUE,

                    JelszoHash TEXT NOT NULL,

                    JelszoSalt TEXT NOT NULL,

                    JelszoLejarat TEXT NOT NULL,

                    Frissit INTEGER NOT NULL DEFAULT 1,

                    Szervezet TEXT NOT NULL,

                    Admin INTEGER NOT NULL DEFAULT 0
                );
                """;

            using SqliteCommand cmd = new(sql, connection);

            await cmd.ExecuteNonQueryAsync();
        }
        public async Task RogzitesAsync(Adat_User adat)
        {
            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            string sql = $"""
        INSERT INTO {TablaNev}
        (
            FelhasznaloNev,
            DolgozoSzam,
            JelszoHash,
            JelszoSalt,
            JelszoLejarat,
            Frissit,
            Szervezet,
            Admin
        )
        VALUES
        (
            @FelhasznaloNev,
            @DolgozoSzam,
            @JelszoHash,
            @JelszoSalt,
            @JelszoLejarat,
            @Frissit,
            @Szervezet,
            @Admin
        );
        """;

            using SqliteCommand cmd = new(sql, connection);

            cmd.Parameters.AddWithValue("@FelhasznaloNev", adat.FelhasznaloNev);

            cmd.Parameters.AddWithValue("@DolgozoSzam", adat.DolgozoSzam);

            cmd.Parameters.AddWithValue("@JelszoHash", adat.JelszoHash);

            cmd.Parameters.AddWithValue("@JelszoSalt", adat.JelszoSalt);

            cmd.Parameters.AddWithValue("@JelszoLejarat", adat.JelszoLejarat.ToString("O"));

            cmd.Parameters.AddWithValue("@Frissit", adat.Frissit);

            cmd.Parameters.AddWithValue("@Szervezet", adat.Szervezet);

            cmd.Parameters.AddWithValue("@Admin", adat.Admin);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ModositasAsync(Adat_User adat)
        {
            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync(FajlNev);

            string sql = $"""
        UPDATE {TablaNev}
        SET
            FelhasznaloNev = @FelhasznaloNev,
            DolgozoSzam = @DolgozoSzam,
            JelszoHash = @JelszoHash,
            JelszoSalt = @JelszoSalt,
            JelszoLejarat = @JelszoLejarat,
            Frissit = @Frissit,
            Szervezet = @Szervezet,
            Admin = @Admin
        WHERE UserId = @UserId;
        """;

            using SqliteCommand cmd = new(sql, connection);

            cmd.Parameters.AddWithValue("@UserId", adat.UserId);

            cmd.Parameters.AddWithValue("@FelhasznaloNev", adat.FelhasznaloNev);

            cmd.Parameters.AddWithValue("@DolgozoSzam", adat.DolgozoSzam);

            cmd.Parameters.AddWithValue("@JelszoHash", adat.JelszoHash);

            cmd.Parameters.AddWithValue("@JelszoSalt", adat.JelszoSalt);

            cmd.Parameters.AddWithValue("@JelszoLejarat", adat.JelszoLejarat.ToString("O"));

            cmd.Parameters.AddWithValue("@Frissit", adat.Frissit);

            cmd.Parameters.AddWithValue("@Szervezet", adat.Szervezet);

            cmd.Parameters.AddWithValue("@Admin", adat.Admin);

            await cmd.ExecuteNonQueryAsync();
        }
        public static async Task<Adat_User?> FelhasznaloKeresesAsync(string dolgozoSzam)
        {
            if (string.IsNullOrWhiteSpace(dolgozoSzam))
                return null;

            using SqliteConnection connection = await SqliteKapcsolat.MegnyitAsync("Bejelentkezes.db");

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
        SELECT
            UserId,
            FelhasznaloNev,
            DolgozoSzam,
            JelszoHash,
            JelszoSalt,
            JelszoLejarat,
            Frissit,
            Szervezet,
            Admin
        FROM Users
        WHERE DolgozoSzam = $dolgozoSzam
        LIMIT 1;
        """;

            command.Parameters.AddWithValue("$dolgozoSzam", dolgozoSzam.Trim());

            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return UserBetoltese(reader);
        }

        private static Adat_User UserBetoltese(SqliteDataReader reader)
        {
            string lejaratSzoveg = reader.IsDBNull(reader.GetOrdinal("JelszoLejarat")) ? "" : reader.GetString(reader.GetOrdinal("JelszoLejarat"));

            DateTime jelszoLejarat = DateTime.MinValue;

            if (!string.IsNullOrWhiteSpace(lejaratSzoveg))
                DateTime.TryParse(lejaratSzoveg, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out jelszoLejarat);

            return new Adat_User(userId: reader.GetInt32(reader.GetOrdinal("UserId")),

                felhasznaloNev: reader.IsDBNull(reader.GetOrdinal("FelhasznaloNev")) ? "" : reader.GetString(reader.GetOrdinal("FelhasznaloNev")),

                dolgozoSzam: reader.IsDBNull(reader.GetOrdinal("DolgozoSzam")) ? "" : reader.GetString(reader.GetOrdinal("DolgozoSzam")),

                jelszoHash: reader.IsDBNull(reader.GetOrdinal("JelszoHash")) ? "" : reader.GetString(reader.GetOrdinal("JelszoHash")),

                jelszoSalt: reader.IsDBNull(reader.GetOrdinal("JelszoSalt")) ? "" : reader.GetString(reader.GetOrdinal("JelszoSalt")),

                jelszoLejarat: jelszoLejarat,

                frissit: reader.GetInt32(reader.GetOrdinal("Frissit")),

                szervezet: reader.IsDBNull(reader.GetOrdinal("Szervezet")) ? "" : reader.GetString(reader.GetOrdinal("Szervezet")),

                admin: reader.GetInt32(reader.GetOrdinal("Admin"))
            );
        }

    }
}