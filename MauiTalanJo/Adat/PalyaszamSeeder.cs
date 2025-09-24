using CsvHelper;
using KmKiolvasasMaui.Models;
using System.Formats.Asn1;
using System.Globalization;

namespace KmKiolvasasMaui.Data
{
    public static class PalyaszamSeeder
    {
        public static void Seed(PalyaszamDbContext context, string csvPath)
        {
            context.Database.EnsureCreated();

            if (!context.Palyaszamok.Any())
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                var records = csv.GetRecords<Palyaszam>().ToList();

                context.Palyaszamok.AddRange(records);
                context.SaveChanges();
            }
        }
    }
}
