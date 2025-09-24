using KmKiolvasasMaui.Data;
using Microsoft.Extensions.Logging;
using Plugin.Maui.OCR;

namespace KmKiolvasasMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseOcr()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            using (var context = new PalyaszamDbContext())
            {
                string csvPath = Path.Combine(FileSystem.AppDataDirectory, "AllomanyTabla.csv");

                if (!File.Exists(csvPath))
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync("AllomanyTabla.csv").Result;
                    using var fileStream = File.Create(csvPath);
                    stream.CopyTo(fileStream);
                }

                PalyaszamSeeder.Seed(context, csvPath);
            }
            return builder.Build();
        }
    }
}
