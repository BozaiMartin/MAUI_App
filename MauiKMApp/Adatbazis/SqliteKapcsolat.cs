using Microsoft.Data.Sqlite;

namespace KmKiolvasasMaui.Adatbazis
{
    public static class SqliteKapcsolat
    {
        public static async Task<SqliteConnection> MegnyitAsync(string fajlNev)
        {
            string jelszo = await AdatbazisKulcsKezelo.KulcsAsync(fajlNev);

            string path = Path.Combine(FileSystem.AppDataDirectory, fajlNev);

            SqliteConnectionStringBuilder builder = new()
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = jelszo
            };

            SqliteConnection connection = new(builder.ToString());

            await connection.OpenAsync();

            using SqliteCommand pragma = connection.CreateCommand();

            pragma.CommandText = "PRAGMA foreign_keys = ON;";

            await pragma.ExecuteNonQueryAsync();

            return connection;
        }

        public static async Task<string> CipherVerzioAsync(string fajlNev)
        {
            using SqliteConnection connection = await MegnyitAsync(fajlNev);

            using SqliteCommand cmd = connection.CreateCommand();

            cmd.CommandText = "PRAGMA cipher_version;";

            object? eredmeny = await cmd.ExecuteScalarAsync();

            return eredmeny?.ToString() ?? "";
        }
    }
}